namespace Cabazure.Kusto.Processing;

public class QueryIdProvider : IQueryIdProvider
{
    public string Create(
        Type queryType,
        string? sessionId)
        => string
            .Concat(
                queryType.Name,
                sessionId switch
                {
                    string s => s,
                    _ => Guid.NewGuid().ToString("N"),
                })
            .ToAlphaNumeric();
}
