using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto.Tests;

public class NewtonsoftObjectConverterTests
{
    [Theory]
    [InlineAutoNSubstituteData(typeof(object), true)]
    [InlineAutoNSubstituteData(typeof(string), false)]
    [InlineAutoNSubstituteData(typeof(NewtonsoftObjectConverterTests), false)]
    public void CanConvert_Returns_True_For_Object(
        Type type,
        bool canConvert,
        NewtonsoftObjectConverter sut)
        => sut.CanConvert(type)
            .Should()
            .Be(canConvert);

    [Theory, AutoNSubstituteData]
    public void ReadJson_Returns_JsonElement(
        Dictionary<string, string> data,
        NewtonsoftObjectConverter sut)
    {
        var json = JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions()
            {
                WriteIndented = true,
            });
        var token = JToken.Parse(json);
        using var reader = new JTokenReader(token);
        reader.Read();

        var result = sut.ReadJson(
            reader: reader,
            objectType: typeof(object),
            existingValue: null,
            serializer: null);

        result
            .Should()
            .BeAssignableTo<JsonElement>();

        ((JsonElement)result!)
            .GetRawText()
            .Should()
            .BeEquivalentTo(json);
    }
}
