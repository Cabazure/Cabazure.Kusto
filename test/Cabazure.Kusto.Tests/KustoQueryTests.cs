namespace Cabazure.Kusto.Tests;

public record KustoQueryTests
{
    [Fact]
    public void Implements()
        => typeof(KustoQuery<>)
        .Should()
        .BeAssignableTo<KustoScript>();
}
