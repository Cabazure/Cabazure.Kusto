using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests.Processing;

public class QueryIdProviderTests
{
    public record SampleQuery(string Filter) : KustoQuery<string>
    {
        public override string GetQueryText() => "Table | where Name == filter";
    }

    public record OtherQuery(string Filter) : KustoQuery<string>
    {
        public override string GetQueryText() => "Table | where Name == filter";
    }

    private readonly QueryIdProvider sut = new();

    [Fact]
    public void CreateQueryId_Starts_With_Query_Type_Name()
        => sut.CreateQueryId(new SampleQuery("a"), "session", null)
            .Should()
            .StartWith(nameof(SampleQuery))
            .And
            .HaveLength(nameof(SampleQuery).Length + 32)
            .And
            .MatchRegex("^[a-zA-Z0-9]+$");

    [Fact]
    public void CreateQueryId_Does_Not_Contain_SessionId()
        => sut.CreateQueryId(new SampleQuery("a"), "mysessionid", null)
            .Should()
            .NotContain("mysessionid");

    [Fact]
    public void CreateQueryId_Is_Stable_For_Same_Session()
        => sut.CreateQueryId(new SampleQuery("a"), "session", null)
            .Should()
            .Be(sut.CreateQueryId(new SampleQuery("b"), "session", null));

    [Fact]
    public void CreateQueryId_Differs_Between_Sessions()
        => sut.CreateQueryId(new SampleQuery("a"), "session1", null)
            .Should()
            .NotBe(sut.CreateQueryId(new SampleQuery("a"), "session2", null));

    [Fact]
    public void CreateQueryId_Differs_Between_Nonces()
        => sut.CreateQueryId(new SampleQuery("a"), null, "nonce1")
            .Should()
            .NotBe(sut.CreateQueryId(new SampleQuery("a"), null, "nonce2"));

    [Fact]
    public void CreateQueryId_Differs_Between_Query_Types()
        => sut.CreateQueryId(new SampleQuery("a"), "session", null)[^32..]
            .Should()
            .NotBe(sut.CreateQueryId(new OtherQuery("a"), "session", null)[^32..]);

    [Fact]
    public void CreateFingerprint_Is_Stable_For_Same_Query()
        => sut.CreateFingerprint(new SampleQuery("a"))
            .Should()
            .Be(sut.CreateFingerprint(new SampleQuery("a")))
            .And
            .MatchRegex("^[a-f0-9]{32}$");

    [Fact]
    public void CreateFingerprint_Differs_For_Different_Parameters()
        => sut.CreateFingerprint(new SampleQuery("a"))
            .Should()
            .NotBe(sut.CreateFingerprint(new SampleQuery("b")));

    [Fact]
    public void CreateFingerprint_Differs_For_Different_Query_Types()
        => sut.CreateFingerprint(new SampleQuery("a"))
            .Should()
            .NotBe(sut.CreateFingerprint(new OtherQuery("a")));

    [Fact]
    public void CreateFingerprint_Is_Independent_Of_Parameter_Order()
    {
        var first = Substitute.For<IKustoScript>();
        first.GetQueryText().Returns("query");
        first.GetParameters().Returns(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 });
        var second = Substitute.For<IKustoScript>();
        second.GetQueryText().Returns("query");
        second.GetParameters().Returns(new Dictionary<string, object> { ["b"] = 2, ["a"] = 1 });

        sut.CreateFingerprint(first)
            .Should()
            .Be(sut.CreateFingerprint(second));
    }
}
