using Cabazure.Kusto;
using Cabazure.Kusto.Processing;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCabazureKusto(
        this IServiceCollection services,
        Action<CabazureKustoOptions>? options = null)
    {
        if (options != null)
        {
            services
                .AddOptions<CabazureKustoOptions>()
                .Configure(options);
        }

        return services
            .AddSingleton<IKustoClientProvider, KustoClientProvider>()
            .AddSingleton<IQueryIdProvider, QueryIdProvider>()
            .AddSingleton<IScriptHandlerFactory, ScriptHandlerFactory>()
            .AddSingleton<IKustoProcessorFactory, KustoProcessorFactory>()
            .AddSingleton(s => s.GetRequiredService<IKustoProcessorFactory>().Create());
    }
}
