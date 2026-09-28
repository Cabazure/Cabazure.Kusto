using Azure.Core;

namespace Cabazure.Kusto.Tests;

public class CabazureKustoOptionsTests
{
    [Theory, AutoNSubstituteData]
    public void WithHostAddress_Sets_HostAddress(
        Uri hostAddress,
        CabazureKustoOptions sut)
    {
        CabazureKustoOptions result = sut.WithHostAddress(hostAddress);

        result.Should().BeSameAs(sut);
        sut.HostAddress.Should().Be(hostAddress);
    }

    [Theory, AutoNSubstituteData]
    public void WithDatabaseName_Sets_DatabaseName(
        string databaseName,
        CabazureKustoOptions sut)
    {
        CabazureKustoOptions result = sut.WithDatabaseName(databaseName);

        result.Should().BeSameAs(sut);
        sut.DatabaseName.Should().Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    public void WithCredential_Sets_Credential(
        TokenCredential credential,
        CabazureKustoOptions sut)
    {
        CabazureKustoOptions result = sut.WithCredential(credential);

        result.Should().BeSameAs(sut);
        sut.Credential.Should().BeSameAs(credential);
    }

    [Theory, AutoNSubstituteData]
    public void WithConnectionString_Sets_ConnectionString(
        string connectionString,
        CabazureKustoOptions sut)
    {
        CabazureKustoOptions result = sut.WithConnectionString(connectionString);

        result.Should().BeSameAs(sut);
        sut.ConnectionString.Should().Be(connectionString);
    }
}
