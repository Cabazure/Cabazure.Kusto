using Cabazure.Kusto;
using Cabazure.Kusto.DependencyInjection;
using Cabazure.Kusto.Processing;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCabazureKusto(
        this IServiceCollection services)
        => AddCabazureKustoCore(
            services,
            connectionName: null,
            builder: null);

    public static IServiceCollection AddCabazureKusto(
        this IServiceCollection services,
        Action<CabazureKustoBuilder> builder)
        => AddCabazureKustoCore(
            services,
            connectionName: null,
            builder);

    public static IServiceCollection AddCabazureKusto(
        this IServiceCollection services,
        string? connectionName)
        => AddCabazureKustoCore(
            services,
            connectionName,
            builder: null);

    public static IServiceCollection AddCabazureKusto(
        this IServiceCollection services,
        string? connectionName,
        Action<CabazureKustoBuilder> builder)
        => AddCabazureKustoCore(
            services,
            connectionName,
            builder);

    private static IServiceCollection AddCabazureKustoCore(
        IServiceCollection services,
        string? connectionName,
        Action<CabazureKustoBuilder>? builder)
    {
        services.AddOptions<CabazureKustoOptions>(connectionName);

        if (builder is not null)
        {
            var kustoBuilder = new CabazureKustoBuilder(
                services,
                connectionName);
            builder.Invoke(kustoBuilder);
        }

        services
            .TryAddSingleton<IKustoConnectionStringProvider, KustoConnectionStringProvider>();
        services
            .TryAddSingleton<IKustoClientProvider, KustoClientProvider>();
        services
            .TryAddSingleton<IQueryIdProvider, QueryIdProvider>();
        services
            .TryAddSingleton<IScriptHandlerFactory, ScriptHandlerFactory>();
        services
            .TryAddSingleton<IKustoProcessorFactory, KustoProcessorFactory>();

        if (connectionName == null)
        {
            services
                .TryAddSingleton(s => s
                    .GetRequiredService<IKustoProcessorFactory>()
                    .Create());
        }

        return services;
    }
}
