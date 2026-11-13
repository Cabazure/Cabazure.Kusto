using System.Data;
using System.Data.SqlTypes;
using System.Text.Json;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Tests.Processing;

public class KeysetQueryHandlerTests
{
    public record Row(DateTimeOffset? StartedAt, string? SessionId);
    public record DecimalRow(decimal Value, string SessionId);
    public record BoolRow(bool? Active, string SessionId);

    private record Query(string Filter) : KustoQuery<Row>
    {
        public JsonSerializerOptions? SeenOptions;

        public override string GetQueryText() => "declare query_parameters (filter:string);\nEvents | where category == filter";

        public override Row[]? ReadResult(IDataReader reader, JsonSerializerOptions serializerOptions)
        {
            SeenOptions = serializerOptions;
            return base.ReadResult(reader, serializerOptions);
        }
    }

    private sealed record PartialQuery(string Filter) : Query(Filter)
    {
        public override Row[] ReadResult(IDataReader reader, JsonSerializerOptions serializerOptions)
            => reader.Read() ? [reader.ReadObject<Row>(serializerOptions)] : [];
    }

    private sealed record DecimalQuery : KustoQuery<DecimalRow>
    {
        public override string GetQueryText() => "Events";
    }

    private sealed record BoolQuery : KustoQuery<BoolRow>
    {
        public override string GetQueryText() => "Events";
    }

    private sealed record ChangingQuery : KustoQuery<Row>
    {
        public const string FirstText = "declare query_parameters (filter:string);\nEvents | where category == filter";
        public int TextReads;
        public int ParameterReads;

        public string Filter => ++ParameterReads == 1 ? "first" : "second";

        public override string GetQueryText() => ++TextReads == 1 ? FirstText : "OtherEvents";
    }

    private static readonly DateTimeOffset Timestamp = new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero).AddTicks(1);
    private readonly ICslQueryProvider provider = Substitute.For<ICslQueryProvider>();
    private readonly CabazureKustoOptions options = new();
    private readonly Query query = new("test");
    private readonly KeysetPagination pagination = new(
        KustoOrder<Row>.Descending(r => r.StartedAt).ThenAscending(r => r.SessionId),
        2);

    private KeysetQueryHandler<Row> CreateHandler(
        KeysetPagination? request = null,
        Query? script = null,
        string connection = "connection",
        string database = "tenant",
        JsonSerializerOptions? serializerOptions = null)
        => new(new QueryIdProvider(), provider, script ?? query, request ?? pagination,
            serializerOptions ?? options.SerializerOptions, connection, database);

    private static DataTable CreateTable(params string[] ids)
    {
        var table = new DataTable();
        table.Columns.Add("startedAt", typeof(DateTime)).DateTimeMode = DataSetDateTime.Utc;
        table.Columns.Add("sessionId", typeof(string));
        foreach (var id in ids)
        {
            table.Rows.Add(Timestamp.UtcDateTime, id);
        }

        return table;
    }

    private void ReturnTables(params DataTable[] tables)
    {
        var queue = new Queue<DataTable>(tables);
        provider.ExecuteQueryAsync(default, default, default, default)
            .ReturnsForAnyArgs(_ => Task.FromResult<IDataReader>(queue.Dequeue().CreateDataReader()));
    }

    private KeysetContinuationToken ParseToken(PagedResult<Row> result, KeysetPagination? request = null)
    {
        var columns = (request ?? pagination).Order.ResolveColumns(options.SerializerOptions);
        var fingerprint = KeysetContinuationToken.CreateFingerprint(
            new QueryIdProvider().CreateFingerprint(query), columns, "connection", "tenant");
        return KeysetContinuationToken.Parse(result.ContinuationToken, fingerprint, columns.Length)!;
    }

    [Fact]
    public async Task ExecuteAsync_Binds_The_Cursor_To_Inputs_Captured_Once_From_The_Original_Query_Type()
    {
        ReturnTables(CreateTable("a", "b", "c"));
        var script = new ChangingQuery();
        var sut = new KeysetQueryHandler<Row>(new QueryIdProvider(), provider, script, pagination,
            options.SerializerOptions, "connection", "tenant");

        var result = await sut.ExecuteAsync(CancellationToken.None);

        script.TextReads.Should().Be(1);
        script.ParameterReads.Should().Be(1);
        _ = provider.Received(1).ExecuteQueryAsync(Arg.Is<string?>(database => database == null),
            Arg.Is<string>(text => text.StartsWith(ChangingQuery.FirstText)),
            Arg.Is<ClientRequestProperties>(properties => properties.Parameters["filter"] == "\"first\""),
            CancellationToken.None);
        var fingerprint = KeysetContinuationToken.CreateFingerprint(
            new QueryIdProvider().CreateFingerprint(typeof(ChangingQuery), ChangingQuery.FirstText,
                new Dictionary<string, string> { ["filter"] = "\"first\"" }),
            pagination.Order.ResolveColumns(options.SerializerOptions), "connection", "tenant");
        KeysetContinuationToken.Parse(result!.ContinuationToken, fingerprint, 2).Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Returns_Pages_Using_The_Last_Returned_Raw_Row()
    {
        ReturnTables(CreateTable("a", "b", "c"), CreateTable("c", "d"));
        using var cts = new CancellationTokenSource();

        var first = await CreateHandler().ExecuteAsync(cts.Token);
        var second = await CreateHandler(pagination with { ContinuationToken = first!.ContinuationToken })
            .ExecuteAsync(cts.Token);

        first.Items.Select(r => r.SessionId).Should().Equal("a", "b");
        first.Items.Should().OnlyContain(r => r.StartedAt == Timestamp);
        first.TotalCount.Should().BeNull();
        var cursor = ParseToken(first);
        cursor.Values[0].GetCslValue().Should().Be("datetime(2026-11-13T10:00:00.0000001Z)");
        cursor.Values[1].Value.Should().Be("b");
        second!.Items.Select(r => r.SessionId).Should().Equal("c", "d");
        second.ContinuationToken.Should().BeNull();
        second.TotalCount.Should().BeNull();
        query.SeenOptions.Should().BeSameAs(options.SerializerOptions);
        _ = provider.Received(1).ExecuteQueryAsync(Arg.Is<string?>(database => database == null),
            Arg.Is<string>(text => text.Contains("strcmp") && text.EndsWith("| take 3")),
            Arg.Is<ClientRequestProperties>(p => p.Parameters["cabazureKeysetAfter1"] == "\"b\""
                && p.Parameters["filter"] == "\"test\""),
            cts.Token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ExecuteAsync_Stops_Without_A_Continuation_When_No_Lookahead_Exists(int count)
    {
        ReturnTables(CreateTable(new[] { "a", "b" }.Take(count).ToArray()));

        var result = await CreateHandler().ExecuteAsync(CancellationToken.None);

        result!.Items.Should().HaveCount(count);
        result.ContinuationToken.Should().BeNull();
        result.TotalCount.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("v2;nonce;50")]
    public async Task ExecuteAsync_Rejects_Invalid_Tokens_Without_Querying(string token)
    {
        var result = await CreateHandler(pagination with { ContinuationToken = token })
            .ExecuteAsync(CancellationToken.None);

        result.Should().BeNull();
        _ = provider.DidNotReceiveWithAnyArgs().ExecuteQueryAsync(default, default, default, default);
    }

    [Theory]
    [InlineData("query")]
    [InlineData("order")]
    [InlineData("connection")]
    [InlineData("database")]
    [InlineData("naming")]
    public async Task ExecuteAsync_Rejects_Tokens_From_Another_Query_Order_Or_Scope(string change)
    {
        ReturnTables(CreateTable("a", "b", "c"));
        var first = await CreateHandler().ExecuteAsync(CancellationToken.None);
        provider.ClearReceivedCalls();
        var request = pagination with { ContinuationToken = first!.ContinuationToken };
        var script = change == "query" ? query with { Filter = "other" } : query;
        if (change == "order")
        {
            request = request with { Order = KustoOrder.Ascending("startedAt").ThenAscending("sessionId") };
        }
        var serializerOptions = change == "naming"
            ? new JsonSerializerOptions(options.SerializerOptions) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
            : options.SerializerOptions;

        var result = await CreateHandler(request, script,
            change == "connection" ? "other" : "connection",
            change == "database" ? "other" : "tenant",
            serializerOptions).ExecuteAsync(CancellationToken.None);

        result.Should().BeNull();
        _ = provider.DidNotReceiveWithAnyArgs().ExecuteQueryAsync(default, default, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_Preserves_Null_Cursor_Keys()
    {
        var table = CreateTable("a", "b", "c");
        table.Rows[1]["startedAt"] = DBNull.Value;
        table.Rows[2]["startedAt"] = DBNull.Value;
        ReturnTables(table);

        var result = await CreateHandler().ExecuteAsync(CancellationToken.None);

        result!.Items[1].StartedAt.Should().BeNull();
        ParseToken(result).Values[0].IsNull.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Captures_Empty_Strings_Before_Nullable_String_Normalization()
    {
        options.ConvertEmptyNullableStringsToNull = true;
        ReturnTables(CreateTable("", "a"));
        var request = pagination with { MaxItemCount = 1 };

        var result = await CreateHandler(request).ExecuteAsync(CancellationToken.None);

        result!.Items.Single().SessionId.Should().BeNull();
        ParseToken(result, request).Values[1].Should().Be(new KeysetCursorValue("string", ""));
    }

    [Fact]
    public async Task ExecuteAsync_Captures_Decimals_Before_Dto_Rounding()
    {
        var raw = SqlDecimal.Parse("123.12345678901234567890123456789012345");
        var table = new DataTable();
        table.Columns.Add("value", typeof(SqlDecimal));
        table.Columns.Add("sessionId", typeof(string));
        table.Rows.Add(raw, "a");
        table.Rows.Add(raw, "b");
        ReturnTables(table);
        var script = new DecimalQuery();
        var request = new KeysetPagination(KustoOrder.Descending("value").ThenAscending("sessionId"), 1);
        var sut = new KeysetQueryHandler<DecimalRow>(new QueryIdProvider(), provider, script, request,
            options.SerializerOptions, "connection", "tenant");

        var result = await sut.ExecuteAsync(CancellationToken.None);

        var columns = request.Order.ResolveColumns(options.SerializerOptions);
        var fingerprint = KeysetContinuationToken.CreateFingerprint(
            new QueryIdProvider().CreateFingerprint(script), columns, "connection", "tenant");
        var cursor = KeysetContinuationToken.Parse(result!.ContinuationToken, fingerprint, 2)!;
        cursor.Values[0].Value.Should().Be(raw.ToString());
        cursor.Values[0].Value.Should().NotBe(KeysetCursorValue.FromValue(result.Items.Single().Value).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_Normalizes_Sdk_Boolean_Keys_Reported_As_SByte(bool? value)
    {
        var reader = Substitute.For<IDataReader>();
        var row = -1;
        reader.FieldCount.Returns(2);
        reader.GetName(0).Returns("active");
        reader.GetName(1).Returns("sessionId");
        reader.GetDataTypeName(0).Returns(nameof(SByte));
        reader.GetDataTypeName(1).Returns(nameof(String));
        reader.IsDBNull(0).Returns(value is null);
        reader.GetBoolean(0).Returns(value ?? false);
        reader.GetValue(0).Returns(value is null ? DBNull.Value : (sbyte)(value.Value ? 1 : 0));
        reader.GetValue(1).Returns(_ => row == 0 ? "a" : "b");
        reader.Read().Returns(_ => ++row < 2);
        provider.ExecuteQueryAsync(default, default, default, default).ReturnsForAnyArgs(reader);
        var script = new BoolQuery();
        var request = new KeysetPagination(KustoOrder.Ascending("active").ThenAscending("sessionId"), 1);
        var sut = new KeysetQueryHandler<BoolRow>(new QueryIdProvider(), provider, script, request,
            options.SerializerOptions, "connection", "tenant");

        var result = await sut.ExecuteAsync(CancellationToken.None);

        result!.Items.Single().Active.Should().Be(value);
        var fingerprint = KeysetContinuationToken.CreateFingerprint(new QueryIdProvider().CreateFingerprint(script),
            request.Order.ResolveColumns(options.SerializerOptions), "connection", "tenant");
        var cursor = KeysetContinuationToken.Parse(result.ContinuationToken, fingerprint, 2)!;
        cursor.Values[0].Should().Be(KeysetCursorValue.FromValue(value));
        reader.Received(1).Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_Disposes_The_Reader_When_Ordering_Columns_Are_Missing()
    {
        var table = new DataTable();
        table.Columns.Add("other", typeof(string));
        using var reader = table.CreateDataReader();
        provider.ExecuteQueryAsync(default, default, default, default).ReturnsForAnyArgs(reader);

        var execute = () => CreateHandler().ExecuteAsync(CancellationToken.None);

        await execute.Should().ThrowAsync<InvalidOperationException>().WithMessage("*startedAt*");
        reader.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Rejects_Partial_Custom_Result_Mapping_And_Disposes_The_Reader()
    {
        using var reader = CreateTable("a", "b", "c").CreateDataReader();
        provider.ExecuteQueryAsync(default, default, default, default).ReturnsForAnyArgs(reader);

        var execute = () => CreateHandler(script: new PartialQuery("test")).ExecuteAsync(CancellationToken.None);

        await execute.Should().ThrowAsync<InvalidOperationException>().WithMessage("*consume the page*");
        reader.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_Honors_PreCanceled_Tokens_Without_Querying()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var execute = () => CreateHandler().ExecuteAsync(cts.Token);

        (await execute.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(cts.Token);
        _ = provider.DidNotReceiveWithAnyArgs().ExecuteQueryAsync(default, default, default, default);
    }
}