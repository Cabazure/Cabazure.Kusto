using Cabazure.Kusto.Ingest.Internal;
using Kusto.Data;
using Kusto.Ingest;

namespace Cabazure.Kusto.Ingest.Tests.Internal;

public class KustoIngestClientProviderTests
{
    [Theory, AutoNSubstituteData]
    public void GetClient_Caches_Client_Per_Connection_And_Mode(
        [Frozen] IKustoConnectionStringProvider connectionStringProvider,
        IKustoIngestClient client,
        string connectionName,
        KustoIngestionMode mode)
    {
        IKustoIngestClientFactory clientFactory
            = Substitute.For<IKustoIngestClientFactory>();
        var connectionString = new KustoConnectionStringBuilder(
            "https://example.kusto.windows.net");
        connectionStringProvider
            .GetConnectionString(connectionName)
            .Returns(connectionString);
        clientFactory
            .Create(mode, connectionString)
            .Returns(client);
        using var sut = new KustoIngestClientProvider(
            connectionStringProvider,
            clientFactory);

        IKustoIngestClient first = sut.GetClient(
            connectionName,
            mode);
        IKustoIngestClient second = sut.GetClient(
            connectionName,
            mode);

        first.Should().BeSameAs(client);
        second.Should().BeSameAs(client);
        clientFactory.Received(1).Create(
            mode,
            connectionString);
    }

    [Theory, AutoNSubstituteData]
    public void GetClient_Creates_Separate_Clients_Per_Mode(
        [Frozen] IKustoConnectionStringProvider connectionStringProvider,
        IKustoIngestClient managedClient,
        IKustoIngestClient queuedClient,
        string connectionName)
    {
        IKustoIngestClientFactory clientFactory
            = Substitute.For<IKustoIngestClientFactory>();
        var connectionString = new KustoConnectionStringBuilder(
            "https://example.kusto.windows.net");
        connectionStringProvider
            .GetConnectionString(connectionName)
            .Returns(connectionString);
        clientFactory
            .Create(KustoIngestionMode.ManagedStreaming, connectionString)
            .Returns(managedClient);
        clientFactory
            .Create(KustoIngestionMode.Queued, connectionString)
            .Returns(queuedClient);
        using var sut = new KustoIngestClientProvider(
            connectionStringProvider,
            clientFactory);

        IKustoIngestClient first = sut.GetClient(
            connectionName,
            KustoIngestionMode.ManagedStreaming);
        IKustoIngestClient second = sut.GetClient(
            connectionName,
            KustoIngestionMode.Queued);

        first.Should().BeSameAs(managedClient);
        second.Should().BeSameAs(queuedClient);
    }

    [Theory, AutoNSubstituteData]
    public void Dispose_Disposes_Cached_Clients(
        [Frozen] IKustoConnectionStringProvider connectionStringProvider,
        IKustoIngestClient client,
        string connectionName,
        KustoIngestionMode mode)
    {
        IKustoIngestClientFactory clientFactory
            = Substitute.For<IKustoIngestClientFactory>();
        var connectionString = new KustoConnectionStringBuilder(
            "https://example.kusto.windows.net");
        connectionStringProvider
            .GetConnectionString(connectionName)
            .Returns(connectionString);
        clientFactory
            .Create(mode, connectionString)
            .Returns(client);
        var sut = new KustoIngestClientProvider(
            connectionStringProvider,
            clientFactory);
        sut.GetClient(
            connectionName,
            mode);

        sut.Dispose();

        client.Received(1).Dispose();
    }
}
