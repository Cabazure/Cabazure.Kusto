namespace Cabazure.Kusto.Ingest.Tests;

public class AssemblyTests
{
    [Fact]
    public void Assembly_Can_Be_Loaded()
        => typeof(AssemblyTests).Assembly.Should().NotBeNull();
}
