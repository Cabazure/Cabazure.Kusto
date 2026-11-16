using System.Globalization;
using System.Text;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

internal static class KeysetQueryBuilder
{
    public static Query Create(
        IKustoScript query,
        IReadOnlyList<KustoOrder.Column> columns,
        int maxItemCount,
        IReadOnlyList<KeysetCursorValue>? after = null)
        => Create(query.GetQueryText(), query.GetCslParameters().ToDictionary(p => p.Key, p => p.Value),
            columns, maxItemCount, after);

    public static Query Create(
        string queryText,
        IReadOnlyDictionary<string, string> parameters,
        IReadOnlyList<KustoOrder.Column> columns,
        int maxItemCount,
        IReadOnlyList<KeysetCursorValue>? after = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxItemCount);
        ArgumentOutOfRangeException.ThrowIfEqual(maxItemCount, int.MaxValue);

        var text = queryText.TrimEnd(' ', '\r', '\n', '\t', ';');
        var properties = new ClientRequestProperties(null, parameters)
        {
            ClientRequestId = Guid.NewGuid().ToString(),
        };
        var builder = new StringBuilder();
        var names = columns
            .Select(c => $"[{KustoScriptExtensions.GetCslValue(c.Name)}]")
            .ToArray();

        if (after is not null)
        {
            var prefix = "cabazureKeysetAfter";
            while (text.Contains(prefix, StringComparison.Ordinal)
                || properties.Parameters.Keys.Any(p => p.StartsWith(prefix, StringComparison.Ordinal)))
            {
                prefix += "_";
            }

            var declarations = new List<string>();
            var predicates = new List<string>();
            var equalities = new List<string>();

            for (var i = 0; i < columns.Count; i++)
            {
                var value = after[i];
                var name = names[i];
                var parameter = prefix + i.ToString(CultureInfo.InvariantCulture);
                var literal = value.GetCslValue();

                if (!value.IsNull)
                {
                    declarations.Add($"{parameter}:{value.Type}");
                    properties.SetParameter(parameter, literal);
                }

                var comparison = value.Type switch
                {
                    "null" => "false",
                    "string" => $"strcmp({name}, {parameter}) {(columns[i].Descending ? "<" : ">")} 0",
                    "bool" => columns[i].Descending
                        ? $"({name} == false and {parameter} == true)"
                        : $"({name} == true and {parameter} == false)",
                    _ => $"{name} {(columns[i].Descending ? "<" : ">")} {parameter}",
                };
                var afterKey = value.IsNull ? comparison : $"(isnull({name}) or {comparison})";
                predicates.Add($"({string.Join(" and ", equalities.Append(afterKey))})");
                equalities.Add(value.IsNull ? $"isnull({name})" : $"{name} == {parameter}");
            }

            if (declarations.Count > 0)
            {
                builder.Append("declare query_parameters (")
                    .AppendJoin(", ", declarations)
                    .Append(");\n");
            }

            builder.Append(text)
                .Append("\n| where ")
                .AppendJoin(" or ", predicates);
        }
        else
        {
            builder.Append(text);
        }

        builder.Append("\n| order by ")
            .AppendJoin(", ", columns.Select((column, i) =>
                $"{names[i]} {(column.Descending ? "desc" : "asc")} nulls last"))
            .Append("\n| take ")
            .Append((maxItemCount + 1).ToString(CultureInfo.InvariantCulture));

        return new(builder.ToString(), properties);
    }

    internal sealed record Query(string Text, ClientRequestProperties Properties);
}