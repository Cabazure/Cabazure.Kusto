namespace Cabazure.Kusto.Processing;

public interface IQueryIdProvider
{
    string Create(
        Type queryType,
        string? sessionId);
}