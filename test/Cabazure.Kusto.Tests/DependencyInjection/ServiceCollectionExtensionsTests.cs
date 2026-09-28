using Cabazure.Kusto.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public void AddCabazureKusto_Without_Builder_Registers_Default_Processor(
        ServiceCollection services)
    {
        services.AddCabazureKusto();

        ServiceProvider provider = services.BuildServiceProvider();
        provider
            .GetRequiredService<IKustoProcessor>()
            .Should()
            .NotBeNull();
        provider
            .GetRequiredService<IOptions<CabazureKustoOptions>>()
            .Value
            .Should()
            .NotBeNull();
    }

    [Theory, AutoNSubstituteData]
    public void AddCabazureKusto_Invokes_Builder(
        ServiceCollection services,
        [Substitute] Action<CabazureKustoBuilder> builder)
    {
        services.AddCabazureKusto(builder);

        builder.Received(1).Invoke(
            Arg.Is<CabazureKustoBuilder>(b
                => b.Services == services));
    }

    [Theory, AutoNSubstituteData]
    public void AddCabazureKusto_Registers_Default_Processor(
        ServiceCollection services)
    {
        services.AddCabazureKusto(_ => { });

        services
            .BuildServiceProvider()
            .GetRequiredService<IKustoProcessor>()
            .Should()
            .NotBeNull();
    }

    [Theory, AutoNSubstituteData]
    public void AddCabazureKusto_Registers_Named_Options(
        ServiceCollection services,
        string connectionName,
        string databaseName)
    {
        services.AddCabazureKusto(builder => builder.Configure(
            connectionName,
            options => options.DatabaseName = databaseName));

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>()
            .Get(connectionName)
            .DatabaseName
            .Should()
            .Be(databaseName);
    }
}
