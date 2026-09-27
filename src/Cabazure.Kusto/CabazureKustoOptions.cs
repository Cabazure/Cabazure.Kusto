using Azure.Core;

namespace Cabazure.Kusto;

public class CabazureKustoOptions
{
    public Uri? HostAddress { get; set; }

    public string? DatabaseName { get; set; }

    public TokenCredential? Credential { get; set; }

    public string? ConnectionString { get; set; }

    public TimeSpan PagedResultExpiration { get; set; } = DefaultPagedResultExpiration;

    public static TimeSpan DefaultPagedResultExpiration { get; } = TimeSpan.FromMinutes(30);

    public static TimeSpan MaxPagedResultExpiration { get; } = TimeSpan.FromHours(24);
}
