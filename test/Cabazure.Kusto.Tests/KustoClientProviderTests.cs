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
        TokenCredential credential,
        KustoClientProvider sut)
    {
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
        TokenCredential credential,
        KustoClientProvider sut)
    {
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
        string requestedDatabase,
        KustoClientProvider sut)
    {
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
        TokenCredential credential,
        KustoClientProvider sut)
    {
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
        TokenCredential credential,
        KustoClientProvider sut)
    {
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
        TokenCredential credential,
        KustoClientProvider sut)
    {
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
        string requestedDatabase,
        KustoClientProvider sut)
    {
        monitor.Get(null).Returns(
            CreateOptions(
                connectionString: HostAddress,
                databaseName: configuredDatabase));

        var client = sut.GetAdminClient(databaseName: requestedDatabase);

        client.DefaultDatabaseName.Should().Be(requestedDatabase);
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Throws_When_Connection_Is_Not_Configured(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        KustoClientProvider sut)
    {
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
}
