using System.Text.Json;

namespace Cabazure.Kusto.Ingest;

public class CabazureKustoIngestOptions
{
    public JsonSerializerOptions SerializerOptions { get; set; }
        = new(JsonSerializerDefaults.Web);
}
