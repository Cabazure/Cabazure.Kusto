namespace Cabazure.Kusto;

public interface IKustoScript
{
    string GetQueryText();

    IDictionary<string, object> GetParameters();
}
