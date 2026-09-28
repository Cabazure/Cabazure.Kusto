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

    public CabazureKustoOptions WithHostAddress(string hostAddress)
        => WithHostAddress(new Uri(hostAddress));

    public CabazureKustoOptions WithHostAddress(Uri hostAddress)
    {
        HostAddress = hostAddress;
        return this;
    }

    public CabazureKustoOptions WithDatabaseName(string databaseName)
    {
        DatabaseName = databaseName;
        return this;
    }

    public CabazureKustoOptions WithCredential(TokenCredential credential)
    {
        Credential = credential;
        return this;
    }

    public CabazureKustoOptions WithConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }
}
