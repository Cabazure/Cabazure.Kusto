using Azure.Core;

namespace Cabazure.Kusto;

public class CabazureKustoOptions
{
    public string? HostAddress { get; set; }

    public string? DatabaseName { get; set; }

    public TokenCredential? Credential { get; set; }
}
