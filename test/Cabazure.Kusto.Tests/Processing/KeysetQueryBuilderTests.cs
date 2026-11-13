using System.Text.Json;
using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests.Processing;

public class KeysetQueryBuilderTests
{
    private sealed record Query(string Filter) : KustoQuery<string>
    {
        public override string GetQueryText()
            => "declare query_parameters (filter:string);\nEvents | where category == filter;";
    }

    private static readonly KustoOrder.Column[] Columns =
    [
        new("startedAt", true),
        new("sessionId", false),
    ];

    [Fact]
    public void First_Page_Wraps_The_Existing_Query_With_Stable_Order_And_Lookahead()
    {
        var query = new Query("test");

        var result = KeysetQueryBuilder.Create(query, Columns, 50);

        result.Text.Should().Be(
            "declare query_parameters (filter:string);\nEvents | where category == filter\n"
            + "| order by [\"startedAt\"] desc nulls last, [\"sessionId\"] asc nulls last\n| take 51");
        result.Properties.Parameters.Should().ContainKey("filter").WhoseValue.Should().Be("\"test\"");
        result.Properties.ClientRequestId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Next_Page_Uses_Lexicographic_Keys_And_Binds_Cursor_Parameters()
    {
        var timestamp = new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero).AddTicks(1);
        var identifier = "'); .drop database example";

        var result = KeysetQueryBuilder.Create(new Query("test"), Columns, 50,
        [
            KeysetCursorValue.FromValue(timestamp),
            KeysetCursorValue.FromValue(identifier),
        ]);

        result.Text.Should().StartWith(
            "declare query_parameters (cabazureKeysetAfter0:datetime, cabazureKeysetAfter1:string);\n");
        result.Text.Should().Contain(
            "| where ((isnull([\"startedAt\"]) or [\"startedAt\"] < cabazureKeysetAfter0))"
            + " or ([\"startedAt\"] == cabazureKeysetAfter0 and "
            + "(isnull([\"sessionId\"]) or strcmp([\"sessionId\"], cabazureKeysetAfter1) > 0))");
        result.Text.Should().NotContain(identifier);
        result.Properties.Parameters.Should().ContainKey("cabazureKeysetAfter0")
            .WhoseValue.Should().Be("datetime(2026-11-13T10:00:00.0000001Z)");
        result.Properties.Parameters.Should().ContainKey("cabazureKeysetAfter1")
            .WhoseValue.Should().Be(KustoScriptExtensions.GetCslValue(identifier));
    }

    [Fact]
    public void Next_Page_Handles_Descending_Strings_And_Ascending_Numbers()
    {
        var result = KeysetQueryBuilder.Create(new Query("test"),
            [new("name", true), new("sequence", false)], 10,
            [KeysetCursorValue.FromValue("b"), KeysetCursorValue.FromValue(42L)]);

        result.Text.Should().Contain("strcmp([\"name\"], cabazureKeysetAfter0) < 0");
        result.Text.Should().Contain(
            "[\"name\"] == cabazureKeysetAfter0 and "
            + "(isnull([\"sequence\"]) or [\"sequence\"] > cabazureKeysetAfter1)");
    }

    [Fact]
    public void Null_Cursor_Keys_Continue_Inside_The_Nulls_Last_Group()
    {
        var result = KeysetQueryBuilder.Create(new Query("test"), Columns, 2,
        [
            KeysetCursorValue.FromValue(DBNull.Value),
            KeysetCursorValue.FromValue("b"),
        ]);

        result.Text.Should().StartWith("declare query_parameters (cabazureKeysetAfter1:string);\n");
        result.Text.Should().Contain(
            "| where (false) or (isnull([\"startedAt\"]) and "
            + "(isnull([\"sessionId\"]) or strcmp([\"sessionId\"], cabazureKeysetAfter1) > 0))");
        result.Properties.Parameters.Should().NotContainKey("cabazureKeysetAfter0");
    }

    [Fact]
    public void All_Null_Cursor_Keys_Do_Not_Declare_Unknown_Types()
    {
        var result = KeysetQueryBuilder.Create(new Query("test"), Columns, 2,
        [
            KeysetCursorValue.FromValue(null),
            KeysetCursorValue.FromValue(null),
        ]);

        result.Text.Should().StartWith("declare query_parameters (filter:string);");
        result.Text.Should().Contain("| where (false) or (isnull([\"startedAt\"]) and false)");
        result.Properties.Parameters.Should().HaveCount(1);
    }

    [Fact]
    public void Generated_Parameters_Do_Not_Collide_With_Existing_Declarations_Or_Parameters()
    {
        var query = Substitute.For<IKustoScript>();
        query.GetQueryText().Returns("declare query_parameters (cabazureKeysetAfter0:long = 0);\nEvents");
        query.GetParameters().Returns(new Dictionary<string, object> { ["cabazureKeysetAfter_0"] = 42L });

        var result = KeysetQueryBuilder.Create(query, [new("id", false)], 1, [KeysetCursorValue.FromValue(10L)]);

        result.Text.Should().StartWith("declare query_parameters (cabazureKeysetAfter__0:long);\n");
        result.Properties.Parameters.Should().ContainKey("cabazureKeysetAfter_0").WhoseValue.Should().Be("42");
        result.Properties.Parameters.Should().ContainKey("cabazureKeysetAfter__0").WhoseValue.Should().Be("10");
    }

    [Fact]
    public void Projected_Column_Names_Are_Quoted_As_Identifiers()
    {
        var columnName = "key\"] | take 0 //";
        var columns = KustoOrder.Ascending(columnName).ResolveColumns(new JsonSerializerOptions());

        var result = KeysetQueryBuilder.Create(new Query("test"), columns, 1, [KeysetCursorValue.FromValue("id")]);

        result.Text.Should().Contain($"strcmp([{KustoScriptExtensions.GetCslValue(columnName)}], cabazureKeysetAfter0) > 0");
        result.Text.Should().Contain($"| order by [{KustoScriptExtensions.GetCslValue(columnName)}] asc nulls last");
    }

    [Theory]
    [InlineData(true, "([\"active\"] == false and cabazureKeysetAfter0 == true)")]
    [InlineData(false, "([\"active\"] == true and cabazureKeysetAfter0 == false)")]
    public void Boolean_Keys_Use_Logical_Rather_Than_Numeric_Comparisons(bool descending, string comparison)
    {
        var result = KeysetQueryBuilder.Create(new Query("test"), [new("active", descending)], 1,
            [KeysetCursorValue.FromValue(true)]);

        result.Text.Should().Contain(comparison);
        result.Properties.Parameters.Should().ContainKey("cabazureKeysetAfter0").WhoseValue.Should().Be("true");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Create_Rejects_Page_Sizes_Without_Room_For_Lookahead(int maxItemCount)
    {
        var create = () => KeysetQueryBuilder.Create(new Query("test"), Columns, maxItemCount);

        create.Should().Throw<ArgumentOutOfRangeException>();
    }

}