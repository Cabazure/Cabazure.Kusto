using Azure.Core;

namespace Cabazure.Kusto;

public class CabazureKustoOptions
{
    public Uri? HostAddress { get; set; }

    public string? DatabaseName { get; set; }

    public TokenCredential? Credential { get; set; }

    public string? ConnectionString { get; set; }
}
