using System.Data;
using System.Data.SqlTypes;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto.Tests;

public class StreamKustoQueryTests
{
    private sealed record TestStreamQuery : StreamKustoQuery<StreamQueryTestObject>;

    public record StreamQueryTestObject(
        bool BoolValue,
        decimal DecimalValue,
        string? NullableValue,
        NestedStreamQueryTestObject NestedValue,
        DateOnly DateOnlyValue);

    public record NestedStreamQueryTestObject(string Name, int Count);

    [Fact]
    public async Task ReadResults_Will_Return_Same_Objects_As_ReadObjects()
    {
        var values = new[]
        {
            new object[]
            {
                true,
                SqlDecimal.Parse("123.45"),
                DBNull.Value,
                JObject.Parse("""{"name":"first","count":1}"""),
                "2026-09-25T00:00:00+00:00",
            },
            new object[]
            {
                false,
                SqlDecimal.Parse("987.65"),
                "available",
                JObject.Parse("""{"name":"second","count":2}"""),
                "2026-09-26",
            },
        };
        var fieldNames = new[]
        {
            nameof(StreamQueryTestObject.BoolValue),
            nameof(StreamQueryTestObject.DecimalValue),
            nameof(StreamQueryTestObject.NullableValue),
            nameof(StreamQueryTestObject.NestedValue),
            nameof(StreamQueryTestObject.DateOnlyValue),
        };
        var typeNames = new[]
        {
            nameof(SByte),
            nameof(SqlDecimal),
            typeof(string).Name,
            nameof(JToken),
            typeof(string).Name,
        };

        var eagerReader = CreateReader(values, fieldNames, typeNames);
        var streamReader = CreateReader(values, fieldNames, typeNames);
        var expected = eagerReader.ReadObjects<StreamQueryTestObject>();
        var query = new TestStreamQuery();

        var actual = new List<StreamQueryTestObject>();
        await foreach (var item in query.ReadResults(streamReader, CancellationToken.None))
        {
            actual.Add(item);
        }

        actual.Should().BeEquivalentTo(expected);
    }

    private static IDataReader CreateReader(
        IReadOnlyList<object[]> rows,
        IReadOnlyList<string> fieldNames,
        IReadOnlyList<string> typeNames)
    {
        var dataReader = Substitute.For<IDataReader>();
        var index = -1;

        dataReader.FieldCount.Returns(fieldNames.Count);
        dataReader.Read().Returns(_ => ++index < rows.Count);
        dataReader.GetName(default).ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);
        dataReader.GetValue(default).ReturnsForAnyArgs(c => rows[index][c.Arg<int>()]);
        dataReader.GetBoolean(default).ReturnsForAnyArgs(c => (bool)rows[index][c.Arg<int>()]);
        dataReader.GetDataTypeName(default).ReturnsForAnyArgs(c => typeNames[c.Arg<int>()]);

        return dataReader;
    }
}
