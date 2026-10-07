using Azure.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    [Theory, AutoNSubstituteData]
    public void ConfigureSerializerOptions_Configures_SerializerOptions(
        CabazureKustoOptions sut)
    {
        JsonSerializerOptions serializerOptions = sut.SerializerOptions;

        CabazureKustoOptions result = sut.ConfigureSerializerOptions(
            options => options.WriteIndented = true);

        result.Should().BeSameAs(sut);
        sut.SerializerOptions.Should().BeSameAs(serializerOptions);
        sut.SerializerOptions.WriteIndented.Should().BeTrue();
        sut.SerializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
        sut.SerializerOptions.Converters
            .Should()
            .ContainSingle(c => c is JsonStringEnumConverter)
            .And
            .ContainSingle(c => c is DataReaderExtensions.DateOnlyJsonConverter);
    }

    [Fact]
    public void ConfigureSerializerOptions_Rejects_Null_Configure()
    {
        var sut = new CabazureKustoOptions();

        Action act = () => sut.ConfigureSerializerOptions(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SerializerOptions_Uses_Query_Mapping_Defaults()
    {
        var sut = new CabazureKustoOptions();

        sut.SerializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
        sut.SerializerOptions.PropertyNamingPolicy
            .Should()
            .BeSameAs(JsonNamingPolicy.CamelCase);
        sut.SerializerOptions.NumberHandling
            .Should()
            .Be(JsonNumberHandling.AllowReadingFromString);
        sut.SerializerOptions.UnmappedMemberHandling
            .Should()
            .Be(JsonUnmappedMemberHandling.Skip);
        sut.SerializerOptions.Converters
            .Should()
            .ContainSingle(c => c is JsonStringEnumConverter)
            .And
            .ContainSingle(c => c is DataReaderExtensions.DateOnlyJsonConverter);
    }

    [Fact]
    public void SerializerOptions_Default_Is_Not_Shared()
    {
        var first = new CabazureKustoOptions();
        var second = new CabazureKustoOptions();

        first.SerializerOptions
            .Should()
            .NotBeSameAs(second.SerializerOptions);
    }
}
