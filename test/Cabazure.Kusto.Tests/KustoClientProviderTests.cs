using Azure.Core;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Tests;

public class KustoClientProviderTests
{
    private const string HostAddress = "http://localhost:8080/";

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Uses_Configured_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string databaseName,
        TokenCredential credential)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(null).Returns(
            CreateOptions(
                databaseName: databaseName,
                credential: credential));

        var client = sut.GetQueryClient();

        client.DefaultDatabaseName.Should().Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Uses_Requested_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string configuredDatabase,
        string requestedDatabase,
        TokenCredential credential)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(null).Returns(
            CreateOptions(
                databaseName: configuredDatabase,
                credential: credential));

        var client = sut.GetQueryClient(databaseName: requestedDatabase);

        client.DefaultDatabaseName.Should().Be(requestedDatabase);
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Uses_Requested_Database_For_ConnectionString(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string configuredDatabase,
        string requestedDatabase)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(null).Returns(
            CreateOptions(
                connectionString: HostAddress,
                databaseName: configuredDatabase));

        var client = sut.GetQueryClient(databaseName: requestedDatabase);

        client.DefaultDatabaseName.Should().Be(requestedDatabase);
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Uses_Connection_Specific_Options(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string connectionName,
        string databaseName,
        TokenCredential credential)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(connectionName).Returns(
            CreateOptions(
                databaseName: databaseName,
                credential: credential));

        var client = sut.GetQueryClient(connectionName);

        client.DefaultDatabaseName.Should().Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    public void GetAdminClient_Uses_Configured_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string databaseName,
        TokenCredential credential)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(null).Returns(
            CreateOptions(
                databaseName: databaseName,
                credential: credential));

        var client = sut.GetAdminClient();

        client.DefaultDatabaseName.Should().Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    public void GetAdminClient_Uses_Requested_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string configuredDatabase,
        string requestedDatabase,
        TokenCredential credential)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(null).Returns(
            CreateOptions(
                databaseName: configuredDatabase,
                credential: credential));

        var client = sut.GetAdminClient(databaseName: requestedDatabase);

        client.DefaultDatabaseName.Should().Be(requestedDatabase);
    }

    [Theory, AutoNSubstituteData]
    public void GetAdminClient_Uses_Requested_Database_For_ConnectionString(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string configuredDatabase,
        string requestedDatabase)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(null).Returns(
            CreateOptions(
                connectionString: HostAddress,
                databaseName: configuredDatabase));

        var client = sut.GetAdminClient(databaseName: requestedDatabase);

        client.DefaultDatabaseName.Should().Be(requestedDatabase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetQueryClientContext_Preserves_The_Cached_Destination_After_Options_Change(
        bool useConnectionString)
    {
        var monitor = Substitute.For<IOptionsMonitor<CabazureKustoOptions>>();
        var options = CreateOptions(
            connectionString: useConnectionString ? HostAddress : null,
            databaseName: "original");
        monitor.Get("named").Returns(options);
        using var sut = CreateSut(monitor);

        var first = sut.GetQueryClientContext("named");
        options.HostAddress = useConnectionString ? null : new Uri("http://localhost:9090/");
        options.ConnectionString = useConnectionString ? "http://localhost:9090/" : null;
        options.DatabaseName = "changed";
        var cached = sut.GetQueryClientContext("named");
        using var freshProvider = CreateSut(monitor);
        var fresh = freshProvider.GetQueryClientContext("named");

        cached.Should().BeSameAs(first);
        sut.GetQueryClient("named").Should().BeSameAs(first.Client);
        new Uri(cached.DataSource).Port.Should().Be(8080);
        cached.DatabaseName.Should().Be("original");
        new Uri(fresh.DataSource).Port.Should().Be(9090);
        fresh.DatabaseName.Should().Be("changed");
        fresh.Client.Should().NotBeSameAs(cached.Client);
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClientContext_Binds_The_Requested_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        string requestedDatabase)
    {
        monitor.Get(null).Returns(CreateOptions(databaseName: "configured"));
        using var sut = CreateSut(monitor);

        var context = sut.GetQueryClientContext(databaseName: requestedDatabase);

        context.DatabaseName.Should().Be(requestedDatabase);
        context.Client.DefaultDatabaseName.Should().Be(requestedDatabase);
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Throws_When_Connection_Is_Not_Configured(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor)
    {
        using var sut = CreateSut(monitor);
        monitor.Get(null).Returns(new CabazureKustoOptions());

        var act = () => sut.GetQueryClient();

        act.Should().Throw<InvalidOperationException>();
    }

    private static CabazureKustoOptions CreateOptions(
        string? connectionString = null,
        string? databaseName = null,
        TokenCredential? credential = null)
        => new()
        {
            HostAddress = connectionString == null
                ? new Uri(HostAddress)
                : null,
            ConnectionString = connectionString,
            DatabaseName = databaseName,
            Credential = credential,
        };

    private static KustoClientProvider CreateSut(
        IOptionsMonitor<CabazureKustoOptions> monitor)
        => new(new KustoConnectionStringProvider(monitor));
}