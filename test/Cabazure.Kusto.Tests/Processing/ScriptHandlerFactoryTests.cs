using System.Data;
using System.Text.Json;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Tests.Processing;

public class ScriptHandlerFactoryTests
{
    private sealed record KeysetRow(string SessionId);

    private sealed record KeysetQuery : KustoQuery<KeysetRow>
    {
        public JsonSerializerOptions? SeenOptions;

        public override string GetQueryText() => "Events";

        public override KeysetRow[]? ReadResult(IDataReader reader, JsonSerializerOptions serializerOptions)
        {
            SeenOptions = serializerOptions;
            return base.ReadResult(reader, serializerOptions);
        }
    }

    [Theory, AutoNSubstituteData]
    public void CanCreate_SimpleQueryHandler(
        ScriptHandlerFactory sut,
        IKustoQuery<string> query)
        => sut
            .Create(query)
            .Should()
            .BeAssignableTo<SimpleQueryHandler<string>>();

    [Theory, AutoNSubstituteData]
    public void CanCreate_StreamQueryHandler(
        ScriptHandlerFactory sut,
        IKustoStreamQuery<string> query)
        => sut
            .CreateStream(query)
            .Should()
            .BeAssignableTo<StreamQueryHandler<string>>();

    [Theory, AutoNSubstituteData]
    public void CanCreate_NewStoredQueryHandler(
        ScriptHandlerFactory sut,
        IKustoQuery<IReadOnlyList<string>> query,
        int maxItemCount,
        string sessionId)
        => sut
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken: null)
            .Should()
            .BeAssignableTo<NewStoredQueryHandler<string>>();

    [Theory, AutoNSubstituteData]
    public void CanCreate_NewStoredQueryHandler_With_TotalCount(
        ScriptHandlerFactory sut,
        IKustoQuery<IReadOnlyList<string>> query,
        int maxItemCount,
        string sessionId)
        => sut
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken: null,
                includeTotalCount: true)
            .Should()
            .BeAssignableTo<NewStoredQueryHandler<string>>();

    [Theory, AutoNSubstituteData]
    public void CanCreate_ExistingStoredQueryHandler(
        ScriptHandlerFactory sut,
        IKustoQuery<IReadOnlyList<string>> query,
        int maxItemCount,
        string sessionId,
        string continuationToken)
        => sut
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken)
            .Should()
            .BeAssignableTo<ExistingStoredQueryHandler<string>>();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(24 * 60 * 60 + 1)]
    public void Create_PagedHandler_Throws_For_Invalid_PagedResultExpiration(
        int expirationSeconds)
    {
        var monitor = Substitute.For<IOptionsMonitor<CabazureKustoOptions>>();
        monitor.Get(Arg.Any<string?>()).Returns(new CabazureKustoOptions
        {
            PagedResultExpiration = TimeSpan.FromSeconds(expirationSeconds),
        });
        var sut = new ScriptHandlerFactory(
            Substitute.For<IQueryIdProvider>(),
            Substitute.For<IKustoClientProvider>(),
            monitor);

        var act = () => sut.Create(
            Substitute.For<IKustoQuery<IReadOnlyList<string>>>(),
            sessionId: null,
            maxItemCount: 10,
            continuationToken: null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_PagedHandler_Reads_PagedResultExpiration_For_Connection()
    {
        var monitor = Substitute.For<IOptionsMonitor<CabazureKustoOptions>>();
        monitor.Get(Arg.Any<string?>()).Returns(new CabazureKustoOptions());
        var sut = new ScriptHandlerFactory(
            Substitute.For<IQueryIdProvider>(),
            Substitute.For<IKustoClientProvider>(),
            monitor);

        sut.Create(
            Substitute.For<IKustoQuery<IReadOnlyList<string>>>(),
            sessionId: null,
            maxItemCount: 10,
            continuationToken: null,
            connectionName: "named");

        monitor.Received(1).Get("named");
    }

    [Fact]
    public async Task CreateKeyset_Uses_Named_Options_And_Only_A_Query_Client()
    {
        var options = new CabazureKustoOptions
        {
            HostAddress = new Uri("https://cluster.kusto.windows.net/"),
            DatabaseName = "tenant",
            PagedResultExpiration = TimeSpan.Zero,
        };
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        var monitor = Substitute.For<IOptionsMonitor<CabazureKustoOptions>>();
        monitor.Get("named").Returns(options);
        var table = new DataTable();
        table.Columns.Add("session_id", typeof(string));
        table.Rows.Add("a");
        table.Rows.Add("b");
        var queryProvider = Substitute.For<ICslQueryProvider>();
        queryProvider.DefaultDatabaseName.Returns("tenant");
        queryProvider.ExecuteQueryAsync(default, default, default, default)
            .ReturnsForAnyArgs(_ => Task.FromResult<IDataReader>(table.CreateDataReader()));
        var clientProvider = Substitute.For<IKustoClientProvider>();
        clientProvider.GetQueryClient("named", null).Returns(queryProvider);
        var sut = new ScriptHandlerFactory(new QueryIdProvider(), clientProvider, monitor);
        var query = new KeysetQuery();
        var request = new KeysetPagination(KustoOrder<KeysetRow>.Ascending(r => r.SessionId), 1);

        var result = await sut.CreateKeyset(query, request, "named").ExecuteAsync(CancellationToken.None);

        result!.Items.Single().SessionId.Should().Be("a");
        result.ContinuationToken.Should().NotBeNull();
        query.SeenOptions.Should().BeSameAs(options.SerializerOptions);
        monitor.Received(1).Get("named");
        clientProvider.Received(1).GetQueryClient("named", null);
        clientProvider.DidNotReceiveWithAnyArgs().GetAdminClient(default, default);

        options.HostAddress = new Uri("https://other.kusto.windows.net/");
        var otherHost = await sut.CreateKeyset(query, request with { ContinuationToken = result.ContinuationToken }, "named")
            .ExecuteAsync(CancellationToken.None);
        otherHost.Should().BeNull();
        _ = queryProvider.ReceivedWithAnyArgs(1).ExecuteQueryAsync(default, default, default, default);
    }

    [Theory, AutoNSubstituteData]
    public async Task Create_Uses_SerializerOptions_For_Connection(
        string connectionName,
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        var options = new CabazureKustoOptions();
        JsonSerializerOptions serializerOptions = options.SerializerOptions;
        var monitor = Substitute.For<IOptionsMonitor<CabazureKustoOptions>>();
        monitor.Get(connectionName).Returns(options);
        var queryProvider = Substitute.For<ICslQueryProvider>();
        queryProvider
            .ExecuteQueryAsync(default, default, default, default)
            .ReturnsForAnyArgs(reader);
        var clientProvider = Substitute.For<IKustoClientProvider>();
        clientProvider
            .GetQueryClient(connectionName, null)
            .Returns(queryProvider);
        var query = Substitute.For<IKustoQuery<string>>();
        var sut = new ScriptHandlerFactory(
            Substitute.For<IQueryIdProvider>(),
            clientProvider,
            monitor);

        await sut
            .Create(
                query,
                connectionName)
            .ExecuteAsync(cancellationToken);

        query
            .Received(1)
            .ReadResult(
                reader,
                serializerOptions);
    }
}