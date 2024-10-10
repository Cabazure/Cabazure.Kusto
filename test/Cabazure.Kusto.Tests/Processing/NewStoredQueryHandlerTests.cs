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
    }
}
