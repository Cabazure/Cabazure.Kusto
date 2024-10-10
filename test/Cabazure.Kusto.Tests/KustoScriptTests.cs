namespace Cabazure.Kusto.Tests;

public record KustoScriptTests : KustoScript
{
    public string Property1 { get; set; }

    public int Property2 { get; set; }

    public KustoScriptTests()
    {
        var fixure = FixtureFactory.Create();
        Property1 = fixure.Create<string>();
        Property2 = fixure.Create<int>();
    }

    [Fact]
    public void GetQuery_Returns_Embedded_Resource_With_Same_Name()
    {
        var resourcePath = $"{GetType().FullName}.kusto";
        using var stream = GetType().Assembly.GetManifestResourceStream(resourcePath)!;
        using var reader = new StreamReader(stream);
        var expectedResult = reader.ReadToEnd();

        GetQueryText()
            .Should()
            .Be(expectedResult);
    }

    [Fact]
    public void GetParameters_Returns_Properties_As_Dictionary()
        => GetParameters()
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, object>()
                {
                    ["property1"] = Property1,
                    ["property2"] = Property2,
                });
}
