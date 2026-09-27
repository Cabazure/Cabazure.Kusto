using System.Data;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Tests.Processing;

public class NewStoredQueryHandlerTests
{
    private readonly IQueryIdProvider queryIdProvider;
    private readonly ICslAdminProvider adminProvider;
    private readonly IKustoQuery<IReadOnlyList<string>> query;
    private readonly int maxItemCount;
    private readonly string sessionId;
    private readonly string queryId;
    private readonly NewStoredQueryHandler<string> sut;

    public NewStoredQueryHandlerTests()
    {
        queryIdProvider = Substitute.For<IQueryIdProvider>();
        adminProvider = Substitute.For<ICslAdminProvider>();
        query = Substitute.For<IKustoQuery<IReadOnlyList<string>>>();

        var fixture = FixtureFactory.Create();
        maxItemCount = 3;
        sessionId = fixture.Create<string>().ToAlphaNumeric();
        queryId = fixture.Create<string>().ToAlphaNumeric();

        queryIdProvider.Create(default, default).ReturnsForAnyArgs(queryId);

        sut = new(queryIdProvider, adminProvider, query, sessionId, maxItemCount);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Creates_A_QueryId(
        CancellationToken cancellationToken)
    {
        await sut.ExecuteAsync(cancellationToken);

        queryIdProvider
            .Received(1)
            .Create(
                query.GetType(),
                sessionId);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_QueryProvider(
        string queryText,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        query.GetQueryText().Returns(queryText);
        query.GetParameters().Returns(parameters);

        await sut.ExecuteAsync(cancellationToken);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                null,
                $".set-or-replace stored_query_result ['{queryId}'] with (previewCount = {maxItemCount}, expiresAfter = 1h) <|\n"
                + queryText + "\n"
                + $"| serialize row_number = row_number()",
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null));
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_Query_With_DataReader(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        adminProvider
            .ExecuteControlCommandAsync(default, default, default)
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
        adminProvider
            .ExecuteControlCommandAsync(default, default, default)
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
            .BeEquivalentTo($"{queryId};{queryResult.Length}");
        result.TotalCount
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Does_Not_Fetch_TotalCount_When_Not_Requested(
        IDataReader reader,
        string[] queryResult,
        CancellationToken cancellationToken)
    {
        adminProvider
            .ExecuteControlCommandAsync(default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResult(default)
            .ReturnsForAnyArgs(queryResult);

        await sut.ExecuteAsync(cancellationToken);

        _ = adminProvider
            .DidNotReceive()
            .ExecuteControlCommandAsync(
                Arg.Any<string>(),
                Arg.Is<string>(s => s.StartsWith(".show")),
                Arg.Any<ClientRequestProperties>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_TotalCount_From_Stored_Query_Result(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        const long totalCount = 42;
        string[] queryResult = ["a", "b", "c"];
        SetupStoredQueryResult(reader, queryResult, totalCount);
        var sut = CreateSut(includeTotalCount: true);

        var result = await sut.ExecuteAsync(cancellationToken);

        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                null,
                $".show stored_query_results ['{queryId}']",
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null));
        result!.Items
            .Should()
            .BeEquivalentTo(queryResult);
        result.TotalCount
            .Should()
            .Be(totalCount);
        result.ContinuationToken
            .Should()
            .Be($"{queryId};{queryResult.Length};{totalCount}");
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_No_ContinuationToken_When_TotalCount_Equals_Page_Size(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        string[] queryResult = ["a", "b", "c"];
        SetupStoredQueryResult(reader, queryResult, queryResult.Length);
        var sut = CreateSut(includeTotalCount: true);

        var result = await sut.ExecuteAsync(cancellationToken);

        result!.TotalCount
            .Should()
            .Be(queryResult.Length);
        result.ContinuationToken
            .Should()
            .BeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_Item_Count_As_TotalCount_When_Page_Is_Not_Full(
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        string[] queryResult = ["a", "b"];
        adminProvider
            .ExecuteControlCommandAsync(default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResult(default)
            .ReturnsForAnyArgs(queryResult);
        var sut = CreateSut(includeTotalCount: true);

        var result = await sut.ExecuteAsync(cancellationToken);

        result!.TotalCount
            .Should()
            .Be(queryResult.Length);
        result.ContinuationToken
            .Should()
            .BeNull();
        _ = adminProvider
            .Received(1)
            .ExecuteControlCommandAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<ClientRequestProperties>());
    }

    private NewStoredQueryHandler<string> CreateSut(bool includeTotalCount)
        => new(queryIdProvider, adminProvider, query, sessionId, maxItemCount, includeTotalCount);

    private void SetupStoredQueryResult(
        IDataReader reader,
        string[] queryResult,
        long totalCount)
    {
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("RowCount", typeof(long));
        table.Rows.Add(queryId, totalCount);

        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string>(),
                Arg.Is<string>(s => s.StartsWith(".set-or-replace")),
                Arg.Any<ClientRequestProperties>())
            .Returns(reader);
        adminProvider
            .ExecuteControlCommandAsync(
                Arg.Any<string>(),
                Arg.Is<string>(s => s.StartsWith(".show")),
                Arg.Any<ClientRequestProperties>())
            .Returns(_ => table.CreateDataReader());
        query
            .ReadResult(reader)
            .Returns(queryResult);
    }
}
