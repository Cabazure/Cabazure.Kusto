namespace Cabazure.Kusto.Tests;

public class KustoProcessorFactoryTests
{
    [Theory, AutoNSubstituteData]
    public void Can_Create_Processor(
        KustoProcessorFactory sut)
    {
        var processor = sut.Create();

        processor
            .Should()
            .BeOfType<KustoProcessor>();
        ((KustoProcessor)processor)
            .ConnectionName
            .Should()
            .BeNull();
        ((KustoProcessor)processor)
            .DatabaseName
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void Can_Create_Processor_With_Connection_And_Database(
        KustoProcessorFactory sut,
        string connectionName,
        string databaseName)
    {
        var processor = sut.Create(
            connectionName,
            databaseName);

        processor
            .Should()
            .BeOfType<KustoProcessor>();
        ((KustoProcessor)processor)
            .ConnectionName
            .Should()
            .Be(connectionName);
        ((KustoProcessor)processor)
            .DatabaseName
            .Should()
            .Be(databaseName);
    }
}
