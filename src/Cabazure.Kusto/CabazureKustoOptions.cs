using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;

namespace Cabazure.Kusto;

public class CabazureKustoOptions
{
    public Uri? HostAddress { get; set; }

    public string? DatabaseName { get; set; }

    public TokenCredential? Credential { get; set; }

    public string? ConnectionString { get; set; }

    public JsonSerializerOptions SerializerOptions { get; }
        = CreateDefaultJsonOptions();

    /// <summary>
    /// Converts empty scalar query values mapped to nullable string properties to null.
    /// Dynamic values and non-nullable strings are unchanged. Disabled by default.
    /// </summary>
    public bool ConvertEmptyNullableStringsToNull
    {
        get => NullableStringResultMapping.IsEnabled(SerializerOptions);
        set => NullableStringResultMapping.Configure(SerializerOptions, value);
    }

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

    public CabazureKustoOptions ConfigureSerializerOptions(
        Action<JsonSerializerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(SerializerOptions);
        return this;
    }

    internal static JsonSerializerOptions CreateDefaultJsonOptions()
        => new()
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new DataReaderExtensions.DateOnlyJsonConverter(),
            },
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        };
}