namespace Cabazure.Kusto.Tests;

public class KustoScriptExtensionsTests
{
    public record T();

    public static TheoryData<object, string> DataValues => new()
    {
        { true, "true" },
        { false, "false" },
        { 123, "123" },
        { 123L, "123" },
        { 123.12M, "decimal(123.12)" },
        { 123.12D, "123.12" },
        { new TimeSpan(1, 2, 3), "time(01:02:03)" },
        { new DateTime(1, 2, 3, 4, 5, 6, DateTimeKind.Utc), "datetime(0001-02-03T04:05:06.0000000Z)" },
        { new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero), "datetime(0001-02-03T04:05:06.0000000Z)" },
        { "string", "\"string\"" },
        { new Dictionary<string, string> { { "key", "value" } }, "dynamic({\"key\":\"value\"})" },
    };

    [Theory]
    [MemberData(nameof(DataValues))]
    public void GetClsValue_Returns_Cls_Formatted_String(
        object input,
        string output)
        => KustoScriptExtensions
            .GetCslValue(input)
            .Should()
            .Be(output);

    [Theory]
    [MemberAutoNSubstituteData(nameof(DataValues))]
    public void GetCslParameters_Returns_Cls_Converted_Query_Parameters(
        object data,
        string cslData,
        IKustoQuery<T> query,
        string[] parameterNames)
    {
        query
            .GetParameters()
            .Returns(parameterNames
                .ToDictionary(
                    n => n,
                    _ => data));

        KustoScriptExtensions
            .GetCslParameters(query)
            .Should()
            .BeEquivalentTo(parameterNames
                .ToDictionary(
                    n => n,
                    _ => cslData));
    }

    [Theory]
    [MemberAutoNSubstituteData(nameof(DataValues))]
    public void GetRequestProperties_Returns_RequestProperties(
        object data,
        string cslData,
        IKustoQuery<T> query,
        string[] parameterNames)
    {
        query
            .GetParameters()
            .Returns(parameterNames
                .ToDictionary(
                    n => n,
                    _ => data));

        var result = KustoScriptExtensions
            .GetRequestProperties(query);

        result.ClientRequestId
            .Should()
            .NotBeEmpty();
        result.Parameters
            .Should()
            .BeEquivalentTo(parameterNames
                .ToDictionary(
                    n => n,
                    _ => cslData));
    }
}
