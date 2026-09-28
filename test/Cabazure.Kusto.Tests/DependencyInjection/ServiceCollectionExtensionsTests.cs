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
    public void AddCabazureKusto_Named_Without_Builder_Registers_Options(
        ServiceCollection services,
        string connectionName)
    {
        services.AddCabazureKusto(connectionName);

        ServiceProvider provider = services.BuildServiceProvider();
        provider
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>()
            .Get(connectionName)
            .Should()
            .NotBeNull();
        provider
            .GetService<IKustoProcessor>()
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void AddCabazureKusto_Invokes_Default_Builder(
        ServiceCollection services,
        [Substitute] Action<CabazureKustoBuilder> builder)
    {
        services.AddCabazureKusto(builder);

        builder.Received(1).Invoke(
            Arg.Is<CabazureKustoBuilder>(b
                => b.Services == services
                && b.ConnectionName == null));
    }

    [Theory, AutoNSubstituteData]
    public void AddCabazureKusto_Invokes_Named_Builder(
        ServiceCollection services,
        string connectionName,
        [Substitute] Action<CabazureKustoBuilder> builder)
    {
        services.AddCabazureKusto(connectionName, builder);

        builder.Received(1).Invoke(
            Arg.Is<CabazureKustoBuilder>(b
                => b.Services == services
                && b.ConnectionName == connectionName));
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
    public void AddCabazureKusto_Named_Does_Not_Register_Default_Processor(
        ServiceCollection services,
        string connectionName)
    {
        services.AddCabazureKusto(connectionName, _ => { });

        services
            .BuildServiceProvider()
            .GetService<IKustoProcessor>()
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public void AddCabazureKusto_Registers_Named_Options(
        ServiceCollection services,
        string connectionName)
    {
        services.AddCabazureKusto(connectionName, _ => { });

        services
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<CabazureKustoOptions>>()
            .Get(connectionName)
            .Should()
            .NotBeNull();
    }
}
