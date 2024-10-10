using Cabazure.Kusto.Processing;

namespace Cabazure.Kusto.Tests.Processing;

public class QueryIdProviderTests
{
    [Theory, AutoNSubstituteData]
    public void CanCreate_QueryId(
        QueryIdProvider sut,
        Type queryType,
        string sessionId)
        => sut.Create(queryType, sessionId)
            .Should()
            .Be($"{queryType.Name}{sessionId.ToAlphaNumeric()}");

    [Theory, AutoNSubstituteData]
    public void CanCreate_QueryId_Without_SessionId(
        QueryIdProvider sut,
        Type queryType)
        => sut.Create(queryType, null)
            .Should()
            .StartWith(queryType.Name)
            .And
            .HaveLength(queryType.Name.Length + Guid.NewGuid().ToString("N").Length);
}
