using Cabazure.Kusto.Processing;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Tests.Processing;

public class ScriptHandlerFactoryTests
{
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
}
