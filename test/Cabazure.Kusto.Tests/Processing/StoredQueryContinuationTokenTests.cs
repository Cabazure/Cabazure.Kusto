using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests.Processing;

public class StoredQueryContinuationTokenTests
{
    [Theory]
    [InlineData("v2;;10", null, 10L, null)]
    [InlineData("v2;abc123;10", "abc123", 10L, null)]
    [InlineData("v2;abc123;10;42", "abc123", 10L, 42L)]
    public void Parse_Returns_Token(
        string value,
        string? nonce,
        long itemsReturned,
        long? totalCount)
        => StoredQueryContinuationToken.Parse(value)
            .Should()
            .Be(new StoredQueryContinuationToken(nonce, itemsReturned, totalCount));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("query;10")]
    [InlineData("query;10;42")]
    [InlineData("v2;;+10")]
    [InlineData("v2;;-1")]
    [InlineData("v2;;10;-1")]
    [InlineData("v2;a b;10")]
    public void Parse_Returns_Null_For_Invalid_Token(string? value)
        => StoredQueryContinuationToken.Parse(value)
            .Should()
            .BeNull();

    [Fact]
    public void Parse_Returns_Null_For_Too_Long_Nonce()
        => StoredQueryContinuationToken.Parse($"v2;{new string('a', 65)};10")
            .Should()
            .BeNull();

    [Theory]
    [InlineData(null, 10L, null, "v2;;10")]
    [InlineData("abc", 10L, null, "v2;abc;10")]
    [InlineData("abc", 10L, 42L, "v2;abc;10;42")]
    public void ToString_Formats_Token(
        string? nonce,
        long itemsReturned,
        long? totalCount,
        string expected)
        => new StoredQueryContinuationToken(nonce, itemsReturned, totalCount)
            .ToString()
            .Should()
            .Be(expected);

    [Theory]
    [InlineData(2, null, false)]
    [InlineData(3, null, true)]
    [InlineData(3, 10L, true)]
    [InlineData(3, 6L, false)]
    public void CreateNext_Returns_Token_Only_When_More_Items_Exist(
        int pageCount,
        long? totalCount,
        bool expectToken)
        => StoredQueryContinuationToken
            .CreateNext("abc", 6, pageCount, 3, totalCount)
            .Should()
            .Match<StoredQueryContinuationToken?>(t => (t != null) == expectToken);
}
