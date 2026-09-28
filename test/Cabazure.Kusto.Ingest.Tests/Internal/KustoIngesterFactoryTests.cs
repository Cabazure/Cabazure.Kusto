using Cabazure.Kusto.Ingest.Internal;
using Kusto.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Ingest.Tests.Internal;

public class KustoIngesterFactoryTests
{
    public record Item(string Value);

    [Theory, AutoNSubstituteData]
    public void Create_Uses_Registration_Defaults(
        [Frozen] IKustoConnectionStringProvider connectionStringProvider,
        string connectionName,
        string databaseName,
        string tableName,
        string mappingName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        connectionStringProvider
            .GetConnectionString(connectionName, null)
            .Returns(new KustoConnectionStringBuilder(
                "https://example.kusto.windows.net")
            {
                InitialCatalog = databaseName,
            });
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton(new KustoIngestion<Item>(
                tableName,
                mappingName,
                KustoIngestionMode.Queued,
                connectionName))
            .BuildServiceProvider();
        var sut = new KustoIngesterFactory(
            serviceProvider,
            clientProvider,
            connectionStringProvider,
            Options.Create(new CabazureKustoIngestOptions()));

        IKustoIngester<Item> result = sut.Create<Item>();

        var ingester = result.Should()
            .BeOfType<KustoIngester<Item>>()
            .Subject;
        ingester.ConnectionName.Should().Be(connectionName);
        ingester.DatabaseName.Should().Be(databaseName);
        ingester.Mode.Should().Be(KustoIngestionMode.Queued);
    }

    [Theory, AutoNSubstituteData]
    public void Create_Applies_Overrides(
        [Frozen] IKustoConnectionStringProvider connectionStringProvider,
        string configuredConnectionName,
        string requestedConnectionName,
        string requestedDatabaseName,
        string tableName,
        string mappingName)
    {
        IKustoIngestClientProvider clientProvider
            = Substitute.For<IKustoIngestClientProvider>();
        connectionStringProvider
            .GetConnectionString(
                requestedConnectionName,
                requestedDatabaseName)
            .Returns(new KustoConnectionStringBuilder(
                "https://example.kusto.windows.net")
            {
                InitialCatalog = requestedDatabaseName,
            });
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton(new KustoIngestion<Item>(
                tableName,
                mappingName,
                KustoIngestionMode.ManagedStreaming,
                configuredConnectionName))
            .BuildServiceProvider();
        var sut = new KustoIngesterFactory(
            serviceProvider,
            clientProvider,
            connectionStringProvider,
            Options.Create(new CabazureKustoIngestOptions()));

        IKustoIngester<Item> result = sut.Create<Item>(
            requestedConnectionName,
            requestedDatabaseName,
            KustoIngestionMode.Streaming);

        var ingester = result.Should()
            .BeOfType<KustoIngester<Item>>()
            .Subject;
        ingester.ConnectionName.Should().Be(requestedConnectionName);
        ingester.DatabaseName.Should().Be(requestedDatabaseName);
        ingester.Mode.Should().Be(KustoIngestionMode.Streaming);
    }
}
