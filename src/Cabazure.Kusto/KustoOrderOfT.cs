using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Cabazure.Kusto;

public sealed class KustoOrder<T> : KustoOrder
{
    private KustoOrder(ImmutableArray<Key> keys)
        : base(keys)
    {
    }

    public static KustoOrder<T> Ascending<TKey>(
        Expression<Func<T, TKey>> property,
        string? columnName = null)
        => new([CreateKey(property, columnName, descending: false)]);

    public static KustoOrder<T> Descending<TKey>(
        Expression<Func<T, TKey>> property,
        string? columnName = null)
        => new([CreateKey(property, columnName, descending: true)]);

    public KustoOrder<T> ThenAscending<TKey>(
        Expression<Func<T, TKey>> property,
        string? columnName = null)
        => new(Keys.Add(CreateKey(property, columnName, descending: false)));

    public KustoOrder<T> ThenDescending<TKey>(
        Expression<Func<T, TKey>> property,
        string? columnName = null)
        => new(Keys.Add(CreateKey(property, columnName, descending: true)));

    private static Key CreateKey<TKey>(
        Expression<Func<T, TKey>> property,
        string? columnName,
        bool descending)
    {
        if (property.Body is not MemberExpression
            {
                Member: PropertyInfo { GetMethod.IsPublic: true } selectedProperty,
                Expression: ParameterExpression parameter,
            }
            || parameter != property.Parameters[0]
            || selectedProperty.GetIndexParameters().Length != 0)
        {
            throw new ArgumentException(
                "The selector must select a readable property directly from the result.",
                nameof(property));
        }

        return columnName is not null
            ? CreateKey(columnName, descending)
            : new(null, selectedProperty, descending);
    }
}