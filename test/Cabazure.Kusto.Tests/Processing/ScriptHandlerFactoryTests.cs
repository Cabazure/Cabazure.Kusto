using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests.Processing;

public class ScriptHandlerFactoryTests
{
    [Theory, AutoNSubstituteData]
    public void CanCreate_SimpleQueryHandler(
        ScriptHandlerFactory sut,
        IKustoQuery<string> query)
        => sut
            .Create(query)
            .Should()
            .BeAssignableTo<SimpleQueryHandler<string>>();

    [Theory, AutoNSubstituteData]
    public void CanCreate_NewStoredQueryHandler(
        ScriptHandlerFactory sut,
        IKustoQuery<IReadOnlyList<string>> query,
        int maxItemCount,
        string sessionId)
        => sut
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken: null)
            .Should()
            .BeAssignableTo<NewStoredQueryHandler<string>>();

    [Theory, AutoNSubstituteData]
    public void CanCreate_ExistingStoredQueryHandler(
        ScriptHandlerFactory sut,
        IKustoQuery<IReadOnlyList<string>> query,
        int maxItemCount,
        string sessionId,
        string continuationToken)
        => sut
            .Create(
                query,
                sessionId,
                maxItemCount,
                continuationToken)
            .Should()
            .BeAssignableTo<ExistingStoredQueryHandler<string>>();
}
