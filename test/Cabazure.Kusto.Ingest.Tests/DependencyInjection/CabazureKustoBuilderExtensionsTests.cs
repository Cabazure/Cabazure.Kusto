using Cabazure.Kusto.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cabazure.Kusto.Ingest.Tests.DependencyInjection;

public class CabazureKustoBuilderExtensionsTests
{
    public record Item(string Value);

    [Theory, AutoNSubstituteData]
    public void AddIngestion_Registers_Typed_Ingestion(
        ServiceCollection services,
        string connectionName,
        string tableName,
        string mappingName)
    {
        var builder = new CabazureKustoBuilder(
            services,
            connectionName);

        CabazureKustoBuilder result = builder.AddIngestion<Item>(
            tableName,
            mappingName);

        result.Should().BeSameAs(builder);
        services
            .BuildServiceProvider()
            .GetRequiredService<KustoIngestion<Item>>()
            .Should()
            .Be(new KustoIngestion<Item>(
                tableName,
                mappingName,
                KustoIngestionMode.ManagedStreaming,
                connectionName));
    }

    [Theory, AutoNSubstituteData]
    public void AddIngestion_Uses_Requested_Mode(
        ServiceCollection services,
        string tableName,
        string mappingName,
        KustoIngestionMode mode)
    {
        var builder = new CabazureKustoBuilder(
            services,
            connectionName: null);

        builder.AddIngestion<Item>(
            tableName,
            mappingName,
            mode);

        services
            .BuildServiceProvider()
            .GetRequiredService<KustoIngestion<Item>>()
            .Mode
            .Should()
            .Be(mode);
    }

    [Theory, AutoNSubstituteData]
    public void AddIngestion_Throws_For_Duplicate_Type(
        ServiceCollection services,
        string tableName,
        string mappingName)
    {
        var builder = new CabazureKustoBuilder(
            services,
            connectionName: null);
        builder.AddIngestion<Item>(
            tableName,
            mappingName);

        Action act = () => builder.AddIngestion<Item>(
            tableName,
            mappingName);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Theory, AutoNSubstituteData]
    public void AddIngestion_Throws_For_Empty_Table(
        ServiceCollection services,
        string mappingName)
    {
        var builder = new CabazureKustoBuilder(
            services,
            connectionName: null);

        Action act = () => builder.AddIngestion<Item>(
            string.Empty,
            mappingName);

        act.Should().Throw<ArgumentException>();
    }

    [Theory, AutoNSubstituteData]
    public void AddIngestion_Throws_For_Empty_Mapping(
        ServiceCollection services,
        string tableName)
    {
        var builder = new CabazureKustoBuilder(
            services,
            connectionName: null);

        Action act = () => builder.AddIngestion<Item>(
            tableName,
            string.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
