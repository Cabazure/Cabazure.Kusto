using System.Collections.Immutable;
using System.Data.SqlTypes;
using System.Globalization;
using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests.Processing;

public class KeysetContinuationTokenTests
{
    [Fact]
    public void Roundtrip_Preserves_Timestamps_Decimals_And_Null_Keys()
    {
        var timestamp = DateTimeOffset.Parse("2026-11-13T10:00:00.0000001+00:00", CultureInfo.InvariantCulture);
        var number = SqlDecimal.Parse("123456789012345678901234567890.12345678");
        var values = ImmutableArray.Create(
            KeysetCursorValue.FromValue(timestamp),
            KeysetCursorValue.FromValue(number),
            KeysetCursorValue.FromValue(null),
            KeysetCursorValue.FromValue("a;\"\\\r\n\u00e9"));
        var token = new KeysetContinuationToken("fingerprint", values).ToString();

        var parsed = KeysetContinuationToken.Parse(token, "fingerprint", values.Length);

        parsed.Should().NotBeNull();
        parsed!.Values.Should().Equal(values);
        parsed.Values[0].GetCslValue().Should().Be("datetime(2026-11-13T10:00:00.0000001Z)");
        parsed.Values[1].GetCslValue().Should().Be("decimal(123456789012345678901234567890.12345678)");
        token.Should().MatchRegex("^ks1\\.[A-Za-z0-9_-]+$");
    }

    public static TheoryData<object> ScalarValues => new()
    {
        "",
        true,
        false,
        int.MinValue,
        long.MaxValue,
        long.MinValue,
        double.Epsilon,
        decimal.MaxValue,
        TimeSpan.MinValue,
        new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.FromHours(2)).AddTicks(1),
    };

    [Theory]
    [MemberData(nameof(ScalarValues))]
    public void Roundtrip_Preserves_Supported_Scalar_Keys(object value)
    {
        var cursor = KeysetCursorValue.FromValue(value);
        var token = new KeysetContinuationToken("expected", [cursor]).ToString();

        var parsed = KeysetContinuationToken.Parse(token, "expected", 1);

        parsed.Should().NotBeNull();
        parsed!.Values.Single().Should().Be(cursor);
        parsed.Values.Single().GetCslValue().Should().Be(cursor.GetCslValue());
    }

    [Fact]
    public void Fingerprint_Binds_Query_Ordering_And_Execution_Scope()
    {
        var columns = new[] { new KustoOrder.Column("startedAt", true), new KustoOrder.Column("sessionId", false) };
        var expected = KeysetContinuationToken.CreateFingerprint("query", columns, "connection", "tenant");

        KeysetContinuationToken.CreateFingerprint("query", columns, "connection", "tenant")
            .Should().Be(expected);

        var alternatives = new[]
        {
            KeysetContinuationToken.CreateFingerprint("other-query", columns, "connection", "tenant"),
            KeysetContinuationToken.CreateFingerprint("query", columns.Reverse().ToArray(), "connection", "tenant"),
            KeysetContinuationToken.CreateFingerprint("query", [columns[0] with { Descending = false }, columns[1]], "connection", "tenant"),
            KeysetContinuationToken.CreateFingerprint("query", [columns[0], columns[1] with { Name = "otherId" }], "connection", "tenant"),
            KeysetContinuationToken.CreateFingerprint("query", columns, "other-connection", "tenant"),
            KeysetContinuationToken.CreateFingerprint("query", columns, "connection", "other-tenant"),
        };

        alternatives.Should().OnlyContain(value => value != expected);
    }

    [Fact]
    public void Parse_Rejects_Mismatched_Fingerprints_And_Key_Counts()
    {
        var token = new KeysetContinuationToken("expected", [KeysetCursorValue.FromValue("id")]).ToString();

        KeysetContinuationToken.Parse(token, "other", 1).Should().BeNull();
        KeysetContinuationToken.Parse(token, "expected", 2).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("v2;nonce;50")]
    [InlineData("ks2.abc")]
    [InlineData("ks1.!")]
    [InlineData("ks1.e30")]
    [InlineData("ks1.bnVsbA")]
    public void Parse_Rejects_Invalid_Tokens(string? token)
        => KeysetContinuationToken.Parse(token, "expected", 1).Should().BeNull();

    [Theory]
    [InlineData("dynamic", "'); .drop database example")]
    [InlineData("long", "1 or true")]
    [InlineData("long", "9223372036854775808")]
    [InlineData("datetime", "invalid")]
    [InlineData("decimal", "1); .drop database example")]
    [InlineData("decimal", null)]
    [InlineData("decimal", "Null")]
    [InlineData("real", "NaN")]
    [InlineData("null", "not-null")]
    [InlineData("string", null)]
    public void Parse_Rejects_Invalid_Or_Executable_Cursor_Values(string type, string? value)
    {
        var token = new KeysetContinuationToken("expected", [new(type, value)]).ToString();

        KeysetContinuationToken.Parse(token, "expected", 1).Should().BeNull();
    }

    [Fact]
    public void Token_Length_Is_Bounded_When_Reading_And_Writing()
    {
        var token = new KeysetContinuationToken("expected", [KeysetCursorValue.FromValue(new string('a', 65536))]);
        var serialize = () => token.ToString();

        serialize.Should().Throw<InvalidOperationException>();
        KeysetContinuationToken.Parse("ks1." + new string('a', 65536), "expected", 1).Should().BeNull();
    }

    [Fact]
    public void Cursor_Literals_Are_Culture_Independent_And_Preserve_Raw_Values()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var decimalValue = SqlDecimal.Parse("123456789012345678901234567890.12345678");

            KeysetCursorValue.FromValue(decimalValue).GetCslValue()
                .Should().Be("decimal(123456789012345678901234567890.12345678)");
            KeysetCursorValue.FromValue(123.25m).GetCslValue().Should().Be("decimal(123.25)");
            KeysetCursorValue.FromValue(123.25d).GetCslValue().Should().Be("123.25");
            KeysetCursorValue.FromValue(TimeSpan.FromTicks(1)).GetCslValue().Should().Be("time(00:00:00.0000001)");
            KeysetCursorValue.FromValue(DateTime.SpecifyKind(new DateTime(2026, 11, 13), DateTimeKind.Unspecified))
                .GetCslValue().Should().Be("datetime(2026-11-13T00:00:00.0000000Z)");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Cursor_Rejects_Non_Finite_Real_Keys(double value)
    {
        var create = () => KeysetCursorValue.FromValue(value);

        create.Should().Throw<NotSupportedException>();
    }
}