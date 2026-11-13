using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cabazure.Kusto.Tests;

public class KustoOrderTests
{
    public record Row(DateTimeOffset StartedAt, string SessionId)
    {
        [JsonPropertyName("external_id")]
        public string ExternalId { get; init; } = "";

        public Row? Parent { get; init; }

        public string Field = "";

        public static string StaticId => "";
    }

    [Fact]
    public void ThenAscending_Preserves_Previous_Order_Definition()
    {
        var original = KustoOrder.Descending("startedAt");
        var extended = original.ThenAscending("sessionId").ThenDescending("revision");
        var options = new JsonSerializerOptions();

        original.ResolveColumns(options)
            .Select(c => (c.Name, c.Descending))
            .Should().Equal(("startedAt", true));
        extended.ResolveColumns(options)
            .Select(c => (c.Name, c.Descending))
            .Should().Equal(("startedAt", true), ("sessionId", false), ("revision", true));
    }

    [Fact]
    public void Typed_Order_Uses_The_Same_Definition_As_Column_Order()
    {
        KustoOrder typed = KustoOrder<Row>
            .Descending(r => r.StartedAt)
            .ThenAscending(r => r.SessionId);
        var columns = KustoOrder.Descending("startedAt").ThenAscending("sessionId");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        typed.ResolveColumns(options)
            .Should().Equal(columns.ResolveColumns(options));
    }

    [Fact]
    public void Typed_Order_Preserves_Previous_Order_Definition()
    {
        var original = KustoOrder<Row>.Ascending(r => r.SessionId);
        var extended = original.ThenDescending(r => r.StartedAt);
        var options = new JsonSerializerOptions();

        original.ResolveColumns(options)
            .Select(c => (c.Name, c.Descending))
            .Should().Equal(("SessionId", false));
        extended.ResolveColumns(options)
            .Select(c => (c.Name, c.Descending))
            .Should().Equal(("SessionId", false), ("StartedAt", true));
    }

    [Fact]
    public void ResolveColumns_Uses_Each_Connections_Naming_Policy()
    {
        var order = KustoOrder<Row>.Ascending(r => r.SessionId);
        var camelCase = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var snakeCase = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        order.ResolveColumns(camelCase).Single().Name.Should().Be("sessionId");
        order.ResolveColumns(snakeCase).Single().Name.Should().Be("session_id");
        order.ResolveColumns(new JsonSerializerOptions()).Single().Name.Should().Be("SessionId");
    }

    [Fact]
    public void ResolveColumns_Uses_JsonPropertyName_And_Explicit_Overrides()
    {
        var order = KustoOrder<Row>
            .Descending(r => r.StartedAt, columnName: "sessionStartedAt")
            .ThenAscending(r => r.ExternalId)
            .ThenAscending(r => r.ExternalId, columnName: "externalAlias");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        order.ResolveColumns(options)
            .Select(c => c.Name)
            .Should().Equal("sessionStartedAt", "external_id", "externalAlias");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Ordering_Rejects_Missing_Column_Names(string? columnName)
    {
        var create = () => KustoOrder.Ascending(columnName!);
        var append = () => KustoOrder.Descending("id").ThenDescending(columnName!);

        create.Should().Throw<ArgumentException>();
        append.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Typed_Ordering_Rejects_Empty_Column_Overrides(string columnName)
    {
        var create = () => KustoOrder<Row>.Ascending(r => r.SessionId, columnName);
        var append = () => KustoOrder<Row>.Ascending(r => r.StartedAt).ThenAscending(r => r.SessionId, columnName);

        create.Should().Throw<ArgumentException>();
        append.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResolveColumns_Rejects_Duplicate_Resolved_Names()
    {
        var columns = KustoOrder.Ascending("sessionId").ThenDescending("sessionId");
        var typed = KustoOrder<Row>
            .Ascending(r => r.SessionId)
            .ThenDescending(r => r.ExternalId, columnName: "sessionId");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var resolveColumns = () => columns.ResolveColumns(options);
        var resolveTyped = () => typed.ResolveColumns(options);

        resolveColumns.Should().Throw<ArgumentException>().WithMessage("*sessionId*");
        resolveTyped.Should().Throw<ArgumentException>().WithMessage("*sessionId*");
    }

    [Fact]
    public void Typed_Ordering_Rejects_Null_Selectors()
    {
        var create = () => KustoOrder<Row>.Descending<string>(null!);

        create.Should().Throw<ArgumentNullException>();
    }

    public static TheoryData<Expression<Func<Row, string>>> InvalidSelectors => new()
    {
        r => r.Parent!.SessionId,
        r => r.SessionId.ToUpperInvariant(),
        r => r.Field,
        r => Row.StaticId,
        r => r.SessionId + "suffix",
    };

    [Theory]
    [MemberData(nameof(InvalidSelectors))]
    public void Typed_Ordering_Rejects_Anything_Other_Than_Direct_Properties(
        Expression<Func<Row, string>> selector)
    {
        var create = () => KustoOrder<Row>.Ascending(selector);
        var append = () => KustoOrder<Row>.Ascending(r => r.StartedAt).ThenDescending(selector);

        create.Should().Throw<ArgumentException>().WithParameterName("property");
        append.Should().Throw<ArgumentException>().WithParameterName("property");
    }
}