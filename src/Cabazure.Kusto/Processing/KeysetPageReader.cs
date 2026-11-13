using System.Collections.Immutable;
using System.Data;

namespace Cabazure.Kusto.Processing;

internal sealed class KeysetPageReader : IDataReader
{
    private readonly IDataReader reader;
    private readonly int maxItemCount;
    private readonly int[] ordinals;

    public KeysetPageReader(
        IDataReader reader,
        IReadOnlyList<KustoOrder.Column> columns,
        int maxItemCount)
    {
        this.reader = reader;
        this.maxItemCount = maxItemCount;
        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        ordinals = columns.Select(column =>
        {
            var ordinal = Array.FindIndex(names, name => string.Equals(name, column.Name, StringComparison.Ordinal));
            return ordinal >= 0
                ? ordinal
                : throw new InvalidOperationException(
                    $"Ordering column '{column.Name}' was not returned by the query.");
        }).ToArray();
    }

    public int RowsRead { get; private set; }

    public bool Completed { get; private set; }

    public bool HasMore { get; private set; }

    public ImmutableArray<KeysetCursorValue> LastValues { get; private set; } = [];

    public bool Read()
    {
        if (Completed)
        {
            return false;
        }

        if (RowsRead == maxItemCount)
        {
            HasMore = reader.Read();
            Completed = true;
            return false;
        }

        if (!reader.Read())
        {
            Completed = true;
            return false;
        }

        LastValues = ordinals.Select(ordinal => KeysetCursorValue.FromValue(
            reader.IsDBNull(ordinal)
                ? null
                : reader.GetDataTypeName(ordinal) == nameof(SByte)
                    ? reader.GetBoolean(ordinal)
                    : reader.GetValue(ordinal))).ToImmutableArray();
        RowsRead++;
        return true;
    }

    public bool NextResult()
        => throw new NotSupportedException("Keyset pagination supports a single tabular result.");

    public object this[int i] => reader[i];
    public object this[string name] => reader[name];
    public int Depth => reader.Depth;
    public int FieldCount => reader.FieldCount;
    public bool IsClosed => reader.IsClosed;
    public int RecordsAffected => reader.RecordsAffected;
    public void Close() => reader.Close();
    public void Dispose() => reader.Dispose();
    public DataTable? GetSchemaTable() => reader.GetSchemaTable();
    public bool GetBoolean(int i) => reader.GetBoolean(i);
    public byte GetByte(int i) => reader.GetByte(i);
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length)
        => reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
    public char GetChar(int i) => reader.GetChar(i);
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length)
        => reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
    public IDataReader GetData(int i) => reader.GetData(i);
    public string GetDataTypeName(int i) => reader.GetDataTypeName(i);
    public DateTime GetDateTime(int i) => reader.GetDateTime(i);
    public decimal GetDecimal(int i) => reader.GetDecimal(i);
    public double GetDouble(int i) => reader.GetDouble(i);
    public Type GetFieldType(int i) => reader.GetFieldType(i);
    public float GetFloat(int i) => reader.GetFloat(i);
    public Guid GetGuid(int i) => reader.GetGuid(i);
    public short GetInt16(int i) => reader.GetInt16(i);
    public int GetInt32(int i) => reader.GetInt32(i);
    public long GetInt64(int i) => reader.GetInt64(i);
    public string GetName(int i) => reader.GetName(i);
    public int GetOrdinal(string name) => reader.GetOrdinal(name);
    public string GetString(int i) => reader.GetString(i);
    public object GetValue(int i) => reader.GetValue(i);
    public int GetValues(object[] values) => reader.GetValues(values);
    public bool IsDBNull(int i) => reader.IsDBNull(i);
}