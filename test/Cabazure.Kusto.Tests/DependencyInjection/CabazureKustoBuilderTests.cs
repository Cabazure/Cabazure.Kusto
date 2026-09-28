using Cabazure.Kusto.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Tests.DependencyInjection;

public class CabazureKustoBuilderTests
{
    public class ConfigureKustoOptions
        : IConfigureOptions<CabazureKustoOptions>
    {
        public void Configure(CabazureKustoOptions options)
            => options.DatabaseName = "configured";
    }

    [Theory, AutoNSubstituteData]
    public void Exposes_Services_And_ConnectionName(
        ServiceCollection services,
        string connectionName)
    {
        var sut = new CabazureKustoBuilder(
            services,
            connectionName);

        sut.Services.Should().BeSameAs(services);
        sut.ConnectionName.Should().Be(connectionName);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Registers_Named_Options(
        ServiceCollection services,
        string connectionName,
        string databaseName)
    {
        var sut = new CabazureKustoBuilder(
            services,
            connectionName);

        sut.Configure(o => o.DatabaseName = databaseName);

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>()
            .Get(connectionName)
            .DatabaseName
            .Should()
            .Be(databaseName);
    }

    [Theory, AutoNSubstituteData]
    public void Configure_Registers_ConfigureOptions(
        ServiceCollection services)
    {
        var sut = new CabazureKustoBuilder(
            services,
            connectionName: null);

        sut.Configure<ConfigureKustoOptions>();

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<CabazureKustoOptions>>()
            .Value
            .DatabaseName
            .Should()
            .Be("configured");
    }
}
