using Cabazure.Kusto.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cabazure.Kusto.Tests.DependencyInjection;

public class CabazureKustoBuilderTests
{
    public sealed record ConfiguredDatabase(string Name);

    public class ConfigureKustoOptions
        : IConfigureOptions<CabazureKustoOptions>
    {
        public void Configure(CabazureKustoOptions options)
            => options.DatabaseName = "configured";
    }

    public class ConfigureNamedKustoOptions
        : IConfigureNamedOptions<CabazureKustoOptions>
    {
        public void Configure(CabazureKustoOptions options)
            => Configure(Options.DefaultName, options);

        public void Configure(
            string? name,
            CabazureKustoOptions options)
            => options.DatabaseName = name;
    }

    public class ConfigureInjectedKustoOptions(
        ConfiguredDatabase database)
        : IConfigureOptions<CabazureKustoOptions>
    {
        public void Configure(CabazureKustoOptions options)
            => options.DatabaseName = database.Name;
    }

    [Theory, AutoNSubstituteData]
    public void Exposes_Services(
        ServiceCollection services)
    {
        var sut = new CabazureKustoBuilder(services);

        sut.Services.Should().BeSameAs(services);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Registers_Named_Options(
        ServiceCollection services,
        string connectionName,
        string databaseName)
    {
        var sut = new CabazureKustoBuilder(services);

        sut.Configure(
            connectionName,
            o => o.DatabaseName = databaseName);

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>()
            .Get(connectionName)
            .DatabaseName
            .Should()
            .Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Registers_Multiple_Named_Options(
        ServiceCollection services,
        string firstConnectionName,
        string secondConnectionName,
        string firstDatabaseName,
        string secondDatabaseName)
    {
        var sut = new CabazureKustoBuilder(services);

        sut.Configure(
            firstConnectionName,
            options => options.DatabaseName = firstDatabaseName);
        sut.Configure(
            secondConnectionName,
            options => options.DatabaseName = secondDatabaseName);

        IOptionsMonitor<CabazureKustoOptions> monitor = services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>();
        monitor.Get(firstConnectionName).DatabaseName
            .Should()
            .Be(firstDatabaseName);
        monitor.Get(secondConnectionName).DatabaseName
            .Should()
            .Be(secondDatabaseName);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Customizes_Seeded_SerializerOptions(
        ServiceCollection services,
        string connectionName)
    {
        var sut = new CabazureKustoBuilder(services);

        sut.Configure(
            connectionName,
            o => o.ConfigureSerializerOptions(
                json => json.PropertyNamingPolicy = null));

        JsonSerializerOptions serializerOptions = services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>()
            .Get(connectionName)
            .SerializerOptions;

        serializerOptions.PropertyNamingPolicy.Should().BeNull();
        serializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
        serializerOptions.NumberHandling
            .Should()
            .Be(JsonNumberHandling.AllowReadingFromString);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Registers_ConfigureOptions(
        ServiceCollection services)
    {
        var sut = new CabazureKustoBuilder(services);

        sut.Configure<ConfigureKustoOptions>();

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<CabazureKustoOptions>>()
            .Value
            .DatabaseName
            .Should()
            .Be("configured");
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Registers_Named_ConfigureOptions(
        ServiceCollection services,
        string connectionName)
    {
        var sut = new CabazureKustoBuilder(services);

        sut.Configure<ConfigureNamedKustoOptions>();

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>()
            .Get(connectionName)
            .DatabaseName
            .Should()
            .Be(connectionName);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Resolves_ConfigureOptions_Dependencies(
        ServiceCollection services,
        ConfiguredDatabase database)
    {
        services.AddSingleton(database);
        var sut = new CabazureKustoBuilder(services);

        sut.Configure<ConfigureInjectedKustoOptions>();

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<CabazureKustoOptions>>()
            .Value
            .DatabaseName
            .Should()
            .Be(database.Name);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Named_Rejects_Empty_ConnectionName(
        ServiceCollection services)
    {
        var sut = new CabazureKustoBuilder(services);

        Action act = () => sut.Configure(
            string.Empty,
            _ => { });

        act.Should().Throw<ArgumentException>();
    }
}
