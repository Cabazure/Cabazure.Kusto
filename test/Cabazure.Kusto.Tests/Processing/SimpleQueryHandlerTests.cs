using System.Data;
using System.Text.Json;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Tests.Processing;

public class SimpleQueryHandlerTests
{
    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_QueryProvider(
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoQuery<string> query,
        SimpleQueryHandler<string> sut,
        string queryText,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        query.GetQueryText().Returns(queryText);
        query.GetParameters().Returns(parameters);

        await sut.ExecuteAsync(cancellationToken);

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                null,
                queryText,
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null
                    && p.Parameters.SequenceEqual(query.GetCslParameters())));
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_Query_With_DataReader(
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoQuery<string> query,
        JsonSerializerOptions serializerOptions,
        IDataReader reader,
        CancellationToken cancellationToken)
    {
        var sut = new SimpleQueryHandler<string>(
            queryProvider,
            query,
            serializerOptions);
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);

        await sut.ExecuteAsync(cancellationToken);

        _ = query
            .Received(1)
            .ReadResult(
                reader,
                serializerOptions);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Returns_Result_From_Query(
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoQuery<string> query,
        SimpleQueryHandler<string> sut,
        IDataReader reader,
        string queryResult,
        CancellationToken cancellationToken)
    {
        queryProvider
            .ExecuteQueryAsync(default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResult(default, default!)
            .ReturnsForAnyArgs(queryResult);

        var result = await sut.ExecuteAsync(cancellationToken);
        result
            .Should()
            .Be(queryResult);
    }
}
