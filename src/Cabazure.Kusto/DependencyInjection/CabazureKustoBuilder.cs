using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cabazure.Kusto.DependencyInjection;

public class CabazureKustoBuilder(
    IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

    public CabazureKustoBuilder Configure(
        Action<CabazureKustoOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }

    public CabazureKustoBuilder Configure(
        string connectionName,
        Action<CabazureKustoOptions> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        Services.Configure(connectionName, configure);
        return this;
    }

    public CabazureKustoBuilder Configure<TConfigureOptions>()
        where TConfigureOptions : class, IConfigureOptions<CabazureKustoOptions>
    {
        Services.ConfigureOptions<TConfigureOptions>();
        return this;
    }
}
