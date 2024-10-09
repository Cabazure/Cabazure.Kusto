using Cabazure.Kusto;
using Cabazure.Kusto.Processing;
using Kusto.Data;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

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
               .AddSingleton(s => KustoClientFactory.CreateCslQueryProvider(s.GetKustoConnection()))
               .AddSingleton(s => KustoClientFactory.CreateCslAdminProvider(s.GetKustoConnection()))
               .AddSingleton<IQueryIdProvider, QueryIdProvider>()
               .AddSingleton<IScriptHandlerFactory, ScriptHandlerFactory>()
               .AddSingleton<IKustoProcessor, KustoProcessor>();
    }

    private static KustoConnectionStringBuilder GetKustoConnection(
        this IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<CabazureKustoOptions>>().Value;

        var connectionString = new KustoConnectionStringBuilder(
            options.HostAddress,
            options.DatabaseName);

        return connectionString
            .WithAadAzureTokenCredentialsAuthentication(
                options.Credential);
    }
}
