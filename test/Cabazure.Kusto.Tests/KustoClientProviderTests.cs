using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Tests;

public class KustoClientProviderTests
{
    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Returns_Client(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        KustoClientProvider sut)
    {
        monitor.Get(null).Returns(options);

        var client = sut.GetQueryClient();

        client.DefaultDatabaseName = options.DatabaseName;
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Returns_Client_With_Connection(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        string connectionName,
        KustoClientProvider sut)
    {
        monitor.Get(connectionName).Returns(options);

        var client = sut.GetQueryClient(connectionName);

        client.DefaultDatabaseName = options.DatabaseName;
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Returns_Client_With_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        string databaseName,
        KustoClientProvider sut)
    {
        monitor.Get(null).Returns(options);

        var client = sut.GetQueryClient(databaseName: databaseName);

        client.DefaultDatabaseName = databaseName;
    }

    [Theory, AutoNSubstituteData]
    public void GetQueryClient_Returns_Client_With_Connection_And_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        string connectionName,
        string databaseName,
        KustoClientProvider sut)
    {
        monitor.Get(connectionName).Returns(options);

        var client = sut.GetQueryClient(connectionName, databaseName);

        client.DefaultDatabaseName = databaseName;
    }

    [Theory, AutoNSubstituteData]
    public void GetAdminClient_Returns_Client(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        KustoClientProvider sut)
    {
        monitor.Get(null).Returns(options);

        var client = sut.GetAdminClient();

        client.DefaultDatabaseName = options.DatabaseName;
    }

    [Theory, AutoNSubstituteData]
    public void GetAdminClient_Returns_Client_With_Connection(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        string connectionName,
        KustoClientProvider sut)
    {
        monitor.Get(connectionName).Returns(options);

        var client = sut.GetAdminClient(connectionName);

        client.DefaultDatabaseName = options.DatabaseName;
    }

    [Theory, AutoNSubstituteData]
    public void GetAdminClient_Returns_Client_With_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        string databaseName,
        KustoClientProvider sut)
    {
        monitor.Get(null).Returns(options);

        var client = sut.GetAdminClient(databaseName: databaseName);

        client.DefaultDatabaseName = databaseName;
    }

    [Theory, AutoNSubstituteData]
    public void GetAdminClient_Returns_Client_With_Connection_And_Database(
        [Frozen] IOptionsMonitor<CabazureKustoOptions> monitor,
        CabazureKustoOptions options,
        string connectionName,
        string databaseName,
        KustoClientProvider sut)
    {
        monitor.Get(connectionName).Returns(options);

        var client = sut.GetAdminClient(connectionName, databaseName);

        client.DefaultDatabaseName = databaseName;
    }
}
