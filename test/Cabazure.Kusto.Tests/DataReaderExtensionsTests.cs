using System.Data;
using System.Data.SqlTypes;
using Kusto.Cloud.Platform.Utils;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto.Tests;

public class DataReaderExtensionsTests
{
    public record TestObject(string Property1, string Property2, string Property3);

    public record RichTestObject(
        bool BoolValue,
        decimal DecimalValue,
        string? NullableValue,
        NestedTestObject NestedValue,
        DateOnly DateOnlyValue);

    public record NestedTestObject(string Name, int Count);

    [Theory, AutoNSubstituteData]
    public void ReadObjects_Will_Return_Objects_Read_From_DataReader(
        List<TestObject> data,
        IDataReader dataReader)
    {
        var properties = typeof(TestObject).GetProperties();
        var fieldNames = properties
            .Select(p => p.Name)
            .ToArray();
        var values = data
            .Select(d => properties.Select(p => p.GetValue(d)!).ToArray())
            .ToArray();
        var index = -1;
        dataReader.FieldCount.Returns(fieldNames.Length);
        dataReader.GetName(default).ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);
        dataReader.Read().Returns(c => ++index < data.Count);
        dataReader.GetValues(default).ReturnsForAnyArgs(c => c.Arg<object[]>().CopyFrom(values[index], 0));
        dataReader.GetValue(default).ReturnsForAnyArgs(c => values[index][c.Arg<int>()]);
        dataReader.GetDataTypeName(default).ReturnsForAnyArgs(c => values[index][c.Arg<int>()].GetType().Name);

        DataReaderExtensions
            .ReadObjects<TestObject>(dataReader)
            .Should()
            .BeEquivalentTo(data);
    }

    [Theory, AutoNSubstituteData]
    public void CanReadObjects(
        List<TestObject> data,
        IDataReader dataReader)
    {
        var properties = typeof(TestObject).GetProperties();
        var fieldNames = properties
            .Select(p => p.Name)
            .ToArray();
        var values = data
            .Select(d => properties.Select(p => p.GetValue(d)!).ToArray())
            .ToArray();
        var index = -1;
        dataReader.FieldCount.Returns(fieldNames.Length);
        dataReader.GetName(default).ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);
        dataReader.Read().Returns(c => ++index < data.Count);
        dataReader.GetValues(default).ReturnsForAnyArgs(c => c.Arg<object[]>().CopyFrom(values[index], 0));
        dataReader.GetValue(default).ReturnsForAnyArgs(c => values[index][c.Arg<int>()]);
        dataReader.GetDataTypeName(default).ReturnsForAnyArgs(c => values[index][c.Arg<int>()].GetType().Name);
        dataReader.NextResult().Returns(true);

        DataReaderExtensions
            .ReadObjectsFromNextResult<TestObject>(dataReader)
            .Should()
            .BeEquivalentTo(data);

        dataReader
            .Received(1)
            .NextResult();
    }

    [Fact]
    public void ReadObject_Will_Reuse_Row_Conversion_Rules()
    {
        var dataReader = Substitute.For<IDataReader>();
        var nested = new JObject
        {
            ["name"] = "nested",
            ["count"] = 3,
        };
        var values = new object[]
        {
            true,
            SqlDecimal.Parse("123.45"),
            DBNull.Value,
            nested,
            "2026-09-25T00:00:00+00:00",
        };
        var typeNames = new[]
        {
            nameof(SByte),
            nameof(SqlDecimal),
            nameof(DBNull),
            nameof(JToken),
            typeof(string).Name,
        };
        var fieldNames = new[]
        {
            nameof(RichTestObject.BoolValue),
            nameof(RichTestObject.DecimalValue),
            nameof(RichTestObject.NullableValue),
            nameof(RichTestObject.NestedValue),
            nameof(RichTestObject.DateOnlyValue),
        };

        dataReader.FieldCount.Returns(fieldNames.Length);
        dataReader.GetName(default).ReturnsForAnyArgs(c => fieldNames[c.Arg<int>()]);
        dataReader.GetValue(default).ReturnsForAnyArgs(c => values[c.Arg<int>()]);
        dataReader.GetBoolean(default).ReturnsForAnyArgs(c => (bool)values[c.Arg<int>()]);
        dataReader.GetDataTypeName(default).ReturnsForAnyArgs(c => typeNames[c.Arg<int>()]);

        var result = dataReader.ReadObject<RichTestObject>();

        result.Should().Be(new RichTestObject(
            BoolValue: true,
            DecimalValue: 123.45m,
            NullableValue: null,
            NestedValue: new NestedTestObject("nested", 3),
            DateOnlyValue: new DateOnly(2026, 9, 25)));
    }
}

