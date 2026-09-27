using System.Data;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;

namespace Cabazure.Kusto.Tests.Processing;

public class ExistingStoredQueryHandlerTests
{
    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<IReadOnlyList<string>> query;
    private readonly int maxItemCount;
    private readonly string sessionId;
    private readonly string queryId;
    private readonly string fingerprint;
    private readonly int itemsReturned;

    public ExistingStoredQueryHandlerTests()
    {
        queryIdProvider = Substitute.For<IQueryIdProvider>();
        queryProvider = Substitute.For<ICslQueryProvider>();
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
            query,
            sessionId,
            maxItemCount,
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
        CancellationToken cancellationToken)
    {
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
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);

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
    public async Task ExecuteAsync_Returns_Null_When_Stored_Result_Is_Missing(
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
