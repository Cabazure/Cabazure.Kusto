using System.Data;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;

namespace Cabazure.Kusto.Tests.Processing;

public class ExistingStoredQueryHandlerTests
{
    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly ICslAdminProvider adminProvider;
    private readonly IKustoQuery<IReadOnlyList<string>> query;
    private readonly int maxItemCount;
    private readonly string sessionId;
    private readonly string queryId;
    private readonly string fingerprint;
    private readonly int itemsReturned;
    private readonly TimeSpan expiration = TimeSpan.FromMinutes(30);

    public ExistingStoredQueryHandlerTests()
    {
        queryIdProvider = Substitute.For<IQueryIdProvider>();
        queryProvider = Substitute.For<ICslQueryProvider>();
        adminProvider = Substitute.For<ICslAdminProvider>();
        query = Substitute.For<IKustoQuery<IReadOnlyList<string>>>();

        var fixture = FixtureFactory.Create();
        maxItemCount = 3;
        sessionId = fixture.Create<string>().ToAlphaNumeric();
        queryId = fixture.Create<string>().ToAlphaNumeric();
        fingerprint = fixture.Create<string>().ToAlphaNumeric();
        itemsReturned = fixture.Create<int>();

        queryIdProvider.CreateQueryId(default!, default, default).ReturnsForAnyArgs(queryId);
        queryIdProvider.CreateFingerprint(default!).ReturnsForAnyArgs(fingerprint);
    }

    private ExistingStoredQueryHandler<string> CreateSut(
        string continuationToken,
        string? sessionId = null)
        => new(
            queryIdProvider,
            queryProvider,
            adminProvider,
            query,
            sessionId,
            maxItemCount,
            expiration,
            continuationToken);

    private void SetupPage(IDataReader reader, string[] items)
    {
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResult(default)
            .ReturnsForAnyArgs(items);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_QueryProvider_With_Fingerprint(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        SetupPage(reader, ["a"]);
        await CreateSut($"v2;;{itemsReturned}").ExecuteAsync(cancellationToken);

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                null,
                $"stored_query_result('{queryId}') " +
                $"| where cabazure_fingerprint == '{fingerprint}' " +
                $"and row_number between({itemsReturned + 1} .. {itemsReturned + maxItemCount})",
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Derives_QueryId_From_SessionId_And_Nonce(
        CancellationToken cancellationToken)
    {
        await CreateSut($"v2;abc123;{itemsReturned}", sessionId).ExecuteAsync(cancellationToken);

        queryIdProvider
            .Received(1)
            .CreateQueryId(query, sessionId, "abc123");
        queryIdProvider
            .Received(1)
            .CreateFingerprint(query);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Uses_No_Nonce_For_Empty_Token_Nonce(
        CancellationToken cancellationToken)
    {
        await CreateSut($"v2;;{itemsReturned}", sessionId).ExecuteAsync(cancellationToken);

        queryIdProvider
            .Received(1)
            .CreateQueryId(query, sessionId, Arg.Is<string?>(n => n == null));
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_Query_With_DataReader(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        SetupPage(reader, ["a"]);

        await CreateSut($"v2;;{itemsReturned}").ExecuteAsync(cancellationToken);

        _ = query
            .Received(1)
            .ReadResult(reader);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_Result_From_Query(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        string[] queryResult = ["a", "b", "c"];
        SetupPage(reader, queryResult);

        var result = await CreateSut($"v2;abc;{itemsReturned}").ExecuteAsync(cancellationToken);

        result!.Items
            .Should()
            .BeEquivalentTo(queryResult);
        result.ContinuationToken
            .Should()
            .Be($"v2;abc;{itemsReturned + queryResult.Length}");
        result.TotalCount
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_No_ContinuationToken_For_Partial_Page(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        SetupPage(reader, ["a"]);

        var result = await CreateSut($"v2;;{itemsReturned}").ExecuteAsync(cancellationToken);

        result!.ContinuationToken
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_TotalCount_From_ContinuationToken(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        var totalCount = itemsReturned + 100L;
        string[] queryResult = ["a", "b", "c"];
        SetupPage(reader, queryResult);

        var result = await CreateSut($"v2;;{itemsReturned};{totalCount}").ExecuteAsync(cancellationToken);

        result!.TotalCount
            .Should()
            .Be(totalCount);
        result.ContinuationToken
            .Should()
            .Be($"v2;;{itemsReturned + queryResult.Length};{totalCount}");
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_No_ContinuationToken_When_TotalCount_Is_Reached(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        string[] queryResult = ["a", "b", "c"];
        var totalCount = itemsReturned + queryResult.Length;
        SetupPage(reader, queryResult);

        var result = await CreateSut($"v2;;{itemsReturned};{totalCount}").ExecuteAsync(cancellationToken);

        result!.TotalCount
            .Should()
            .Be(totalCount);
        result.ContinuationToken
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Does_Not_Recreate_When_Page_Is_Found(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        SetupPage(reader, ["a", "b", "c"]);

        await CreateSut($"v2;;{itemsReturned}").ExecuteAsync(cancellationToken);

        _ = adminProvider
            .DidNotReceiveWithAnyArgs()
            .ExecuteControlCommandAsync(default, default, default);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Recreates_Expired_Stored_Result(
        string queryText,
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        string[] queryResult = ["a", "b", "c"];
        query.GetQueryText().Returns(queryText);
        query.ReadResult(reader).Returns(queryResult);
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(
                _ => throw new SemanticException(),
                _ => Task.FromResult(reader));

        var result = await CreateSut($"v2;abc;{itemsReturned}").ExecuteAsync(cancellationToken);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                Arg.Is<string>(s => s == null),
                $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {maxItemCount}, expiresAfter = 1800s) <|\n"
                + queryText + "\n"
                + "| serialize row_number = row_number()\n"
                + $"| extend cabazure_fingerprint = '{fingerprint}'",
                Arg.Is<ClientRequestProperties>(p => p.ClientRequestId != null));
        result!.Items
            .Should()
            .BeEquivalentTo(queryResult);
        result.ContinuationToken
            .Should()
            .Be($"v2;abc;{itemsReturned + queryResult.Length}");
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Recreates_Stored_Result_When_Fingerprint_Does_Not_Match(
        IDataReader emptyReader,
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        string[] queryResult = ["a", "b", "c"];
        query.ReadResult(emptyReader).Returns([]);
        query.ReadResult(reader).Returns(queryResult);
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(emptyReader, reader);

        var result = await CreateSut($"v2;;{itemsReturned}").ExecuteAsync(cancellationToken);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                Arg.Any<string>(),
                Arg.Is<string>(s => s.StartsWith(".set-or-replace")),
                Arg.Any<ClientRequestProperties>());
        _ = queryProvider
            .Received(3)
            .ExecuteQueryAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>(),
                Arg.Any<CancellationToken>());
        result!.Items
            .Should()
            .BeEquivalentTo(queryResult);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_Empty_Page_When_Recreated_Result_Has_No_More_Items(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        SetupPage(reader, []);

        var result = await CreateSut($"v2;;{itemsReturned}").ExecuteAsync(cancellationToken);

        result!.Items
            .Should()
            .BeEmpty();
        result.ContinuationToken
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Refreshes_TotalCount_When_Recreating(
        IDataReader emptyReader,
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        const long refreshedTotal = 1000;
        string[] queryResult = ["a", "b", "c"];
        query.ReadResult(emptyReader).Returns([]);
        query.ReadResult(reader).Returns(queryResult);
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(emptyReader, reader);
        var table = new DataTable();
        table.Columns.Add("RowCount", typeof(long));
        table.Rows.Add(refreshedTotal);
        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string>(),
                Arg.Is<string>(s => s.StartsWith(".show")),
                Arg.Any<ClientRequestProperties>())
            .Returns(_ => table.CreateDataReader());

        var result = await CreateSut($"v2;;{itemsReturned};{itemsReturned + 10}").ExecuteAsync(cancellationToken);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                Arg.Is<string>(s => s == null),
                $".show stored_query_results ['{queryId}']",
                Arg.Any<ClientRequestProperties>());
        result!.TotalCount
            .Should()
            .Be(refreshedTotal);
        result.ContinuationToken
            .Should()
            .Be($"v2;;{itemsReturned + queryResult.Length};{refreshedTotal}");
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_Null_When_Stored_Result_Cannot_Be_Recreated(
        CancellationToken cancellationToken)
    {
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs<Task<IDataReader>>(_ => throw new SemanticException());

        var result = await CreateSut($"v2;;{itemsReturned}").ExecuteAsync(cancellationToken);

        result
            .Should()
            .BeNull();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("query;10")]
    [InlineData("query;10;20")]
    [InlineData("v1;;10")]
    [InlineData("v2;;abc")]
    [InlineData("v2;;-1")]
    [InlineData("v2;;10;abc")]
    [InlineData("v2;;10;20;30")]
    [InlineData("v2;not-alpha;10")]
    public async Task ExecuteAsync_Returns_Null_For_Invalid_ContinuationToken(
        string invalidToken)
    {
        var result = await CreateSut(invalidToken).ExecuteAsync(CancellationToken.None);

        result
            .Should()
            .BeNull();
        _ = queryProvider
            .DidNotReceiveWithAnyArgs()
            .ExecuteQueryAsync(default, default, default);
    }
}
