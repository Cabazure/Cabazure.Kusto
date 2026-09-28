using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.DependencyInjection;

public class CabazureKustoBuilder(
    IServiceCollection services,
    string? connectionName)
{
    public IServiceCollection Services { get; } = services;

    public string? ConnectionName { get; } = connectionName;

    public CabazureKustoBuilder Configure(
        Action<CabazureKustoOptions> configure)
    {
        Services.Configure(ConnectionName, configure);
        return this;
    }

    public CabazureKustoBuilder Configure<TConfigureOptions>()
        where TConfigureOptions : class, IConfigureOptions<CabazureKustoOptions>
    {
        Services.ConfigureOptions<TConfigureOptions>();
        return this;
    }
}
