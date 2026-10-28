using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kusto.Cloud.Platform.Utils;
using Newtonsoft.Json.Linq;

namespace Cabazure.Kusto.Tests;

public class DataReaderExtensionsTests
{
    private readonly JsonSerializerOptions serializerOptions
        = new CabazureKustoOptions().SerializerOptions;

    public enum TestState
    {
        Active,
    }

    public record TestObject(string Property1, string Property2, string Property3);

    public record CompatibilityTestObject(
        TestState State,
        int Count,
        string DisplayName);

    public record RichTestObject(
        bool BoolValue,
        decimal DecimalValue,
        string? NullableValue,
        NestedTestObject NestedValue,
        DateOnly DateOnlyValue);

    public record NestedTestObject(string Name, int Count);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReadObjects_Normalizes_Only_Empty_Nullable_Scalar_Strings_When_Enabled(
        bool enabled,
        bool sourceGenerated)
    {
        var options = new CabazureKustoOptions
        {
            ConvertEmptyNullableStringsToNull = enabled,
        };
        if (sourceGenerated)
        {
            options.SerializerOptions.TypeInfoResolver = NullableStringTestContext.Default;
        }

        var table = new DataTable();
        table.Columns.Add("WIRE_REASON", typeof(string));
        table.Columns.Add("REQUIRED", typeof(string));
        table.Columns.Add("NESTED", typeof(object));
        table.Columns.Add("unknownColumn", typeof(string));
        foreach (var value in new object[] { "", " ", "reason", DBNull.Value })
        {
            table.Rows.Add(value, "", JObject.Parse("""{"value":""}"""), "");
        }

        using var reader = table.CreateDataReader();
        var result = reader.ReadObjects<NullableStringTestRow>(options.SerializerOptions);

        result.Select(r => r.Optional).Should().Equal(enabled
            ? [null, " ", "reason", null]
            : ["", " ", "reason", null]);
        result.Should().OnlyContain(r => r.Required == "" && r.Nested.Value == "");

        JsonSerializer.Deserialize<NullableStringTestRow>(
            """{"wire_reason":"","required":"","nested":{"value":""}}""",
            options.SerializerOptions)!.Optional.Should().Be("");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadObject_Preserves_Dynamic_String_Values(bool wrapped)
    {
        var options = new CabazureKustoOptions
        {
            ConvertEmptyNullableStringsToNull = true,
        };
        var table = new DataTable();
        table.Columns.Add("wire_reason", typeof(object));
        table.Columns.Add("required", typeof(string));
        table.Columns.Add("nested", typeof(object));
        table.Rows.Add(wrapped ? new JValue("") : "", "", JObject.Parse("""{"value":null}"""));
        using var reader = table.CreateDataReader();
        reader.Read();

        var result = reader.ReadObject<NullableStringTestRow>(options.SerializerOptions);

        result.Optional.Should().Be("");
        result.Nested.Value.Should().BeNull();
    }

    [Fact]
    public void ReadObject_Uses_Case_Sensitive_Json_Names_When_Configured()
    {
        var options = new CabazureKustoOptions
        {
            ConvertEmptyNullableStringsToNull = true,
        };
        options.SerializerOptions.PropertyNameCaseInsensitive = false;
        var table = new DataTable();
        table.Columns.Add("wire_reason", typeof(string));
        table.Columns.Add("required", typeof(string));
        table.Columns.Add("nested", typeof(object));
        table.Rows.Add("", "", JObject.Parse("""{"value":""}"""));
        using var reader = table.CreateDataReader();
        reader.Read();

        reader.ReadObject<NullableStringTestRow>(options.SerializerOptions)
            .Optional.Should().BeNull();
    }

    [Fact]
    public void DateOnlyJsonConverter_Writes_Invariant_Iso_Date()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");

            string result = JsonSerializer.Serialize(
                new DateOnly(2026, 9, 25),
                serializerOptions);

            result.Should().Be("\"2026-09-25\"");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory, AutoNSubstituteData]
    public void ReadObjects_Rejects_Null_SerializerOptions(
        IDataReader dataReader)
    {
        Action act = () => dataReader.ReadObjects<TestObject>(null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory, AutoNSubstituteData]
    public void ReadObject_Rejects_Null_SerializerOptions(
        IDataReader dataReader)
    {
        Action act = () => dataReader.ReadObject<TestObject>(null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory, AutoNSubstituteData]
    public void ReadObjectsFromNextResult_Rejects_Null_SerializerOptions(
        IDataReader dataReader)
    {
        Action act = () => dataReader
            .ReadObjectsFromNextResult<TestObject>(null!);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

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
            .ReadObjects<TestObject>(dataReader, serializerOptions)
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
            .ReadObjectsFromNextResult<TestObject>(
                dataReader,
                serializerOptions)
            .Should()
            .BeEquivalentTo(data);

        dataReader
            .Received(1)
            .NextResult();
    }

    [Fact]
    public void ReadObjects_Will_Ignore_Stored_Query_Result_Paging_Columns()
    {
        var table = new DataTable();
        table.Columns.Add(nameof(TestObject.Property1), typeof(string));
        table.Columns.Add(nameof(TestObject.Property2), typeof(string));
        table.Columns.Add(nameof(TestObject.Property3), typeof(string));
        table.Columns.Add("row_number", typeof(long));
        table.Columns.Add("cabazure_fingerprint", typeof(string));
        table.Rows.Add("a", "b", "c", 1L, "abc123");

        var result = table
            .CreateDataReader()
            .ReadObjects<TestObject>(serializerOptions);

        result
            .Should()
            .BeEquivalentTo([new TestObject("a", "b", "c")]);
    }

    [Fact]
    public void ReadObject_Uses_Compatibility_Defaults()
    {
        var table = new DataTable();
        table.Columns.Add("STATE", typeof(string));
        table.Columns.Add("COUNT", typeof(string));
        table.Columns.Add("DISPLAYNAME", typeof(string));
        table.Rows.Add("Active", "42", "example");
        using DataTableReader reader = table.CreateDataReader();
        reader.Read().Should().BeTrue();

        CompatibilityTestObject result
            = reader.ReadObject<CompatibilityTestObject>(
                serializerOptions);

        result.Should().Be(new CompatibilityTestObject(
            TestState.Active,
            42,
            "example"));
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

        var result = dataReader.ReadObject<RichTestObject>(
            serializerOptions);

        result.Should().Be(new RichTestObject(
            BoolValue: true,
            DecimalValue: 123.45m,
            NullableValue: null,
            NestedValue: new NestedTestObject("nested", 3),
            DateOnlyValue: new DateOnly(2026, 9, 25)));
    }
}

internal record NullableStringTestRow(
    [property: JsonPropertyName("wire_reason")] string? Optional,
    string Required,
    NullableStringTestNested Nested);

internal record NullableStringTestNested(string? Value);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NullableStringTestRow))]
internal partial class NullableStringTestContext : JsonSerializerContext;