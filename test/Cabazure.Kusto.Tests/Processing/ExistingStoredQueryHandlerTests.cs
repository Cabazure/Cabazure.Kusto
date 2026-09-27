using System.Data;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Tests.Processing;

public class ExistingStoredQueryHandlerTests
{
    private readonly ICslQueryProvider queryProvider;
    private readonly IKustoQuery<IReadOnlyList<string>> query;
    private readonly int maxItemCount;
    private readonly string queryId;
    private readonly int itemsReturned;
    private readonly string continuationToken;
    private readonly ExistingStoredQueryHandler<string> sut;

    public ExistingStoredQueryHandlerTests()
    {
        queryProvider = Substitute.For<ICslQueryProvider>();
        query = Substitute.For<IKustoQuery<IReadOnlyList<string>>>();

        var fixture = FixtureFactory.Create();
        maxItemCount = 3;
        queryId = fixture.Create<string>().ToAlphaNumeric();
        itemsReturned = fixture.Create<int>();
        continuationToken = $"{queryId};{itemsReturned}";

        sut = new(queryProvider, query, maxItemCount, continuationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_QueryProvider(
        CancellationToken cancellationToken)
    {
        await sut.ExecuteAsync(cancellationToken);

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                null,
                $"stored_query_result('{queryId}') " +
                $"| where row_number between({itemsReturned + 1} .. {itemsReturned + maxItemCount})",
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null));
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_Query_With_DataReader(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);

        await sut.ExecuteAsync(cancellationToken);

        _ = query
            .Received(1)
            .ReadResult(reader);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_Result_From_Query(
        IDataReader reader,
        string[] queryResult,
        CancellationToken cancellationToken)
    {
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResult(default)
            .ReturnsForAnyArgs(queryResult);

        var result = await sut.ExecuteAsync(cancellationToken);
        result.Items
            .Should()
            .BeEquivalentTo(queryResult);
        result.ContinuationToken
            .Should()
            .BeEquivalentTo($"{queryId};{itemsReturned + queryResult.Length}");
        result.TotalCount
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
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResult(default)
            .ReturnsForAnyArgs(queryResult);
        var sut = new ExistingStoredQueryHandler<string>(
            queryProvider,
            query,
            maxItemCount,
            $"{queryId};{itemsReturned};{totalCount}");

        var result = await sut.ExecuteAsync(cancellationToken);

        result!.TotalCount
            .Should()
            .Be(totalCount);
        result.ContinuationToken
            .Should()
            .Be($"{queryId};{itemsReturned + queryResult.Length};{totalCount}");
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_No_ContinuationToken_When_TotalCount_Is_Reached(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        string[] queryResult = ["a", "b", "c"];
        var totalCount = itemsReturned + queryResult.Length;
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResult(default)
            .ReturnsForAnyArgs(queryResult);
        var sut = new ExistingStoredQueryHandler<string>(
            queryProvider,
            query,
            maxItemCount,
            $"{queryId};{itemsReturned};{totalCount}");

        var result = await sut.ExecuteAsync(cancellationToken);

        result!.TotalCount
            .Should()
            .Be(totalCount);
        result.ContinuationToken
            .Should()
            .BeNull();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("query;abc")]
    [InlineData("query;10;abc")]
    [InlineData("query;10;20;30")]
    public async Task ExecuteAsync_Returns_Null_For_Invalid_ContinuationToken(
        string invalidToken)
    {
        var sut = new ExistingStoredQueryHandler<string>(
            queryProvider,
            query,
            maxItemCount,
            invalidToken);

        var result = await sut.ExecuteAsync(CancellationToken.None);

        result
            .Should()
            .BeNull();
        _ = queryProvider
            .DidNotReceiveWithAnyArgs()
            .ExecuteQueryAsync(default, default, default);
    }
}
