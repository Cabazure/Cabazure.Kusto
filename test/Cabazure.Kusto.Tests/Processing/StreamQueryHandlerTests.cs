using System.Data;
using System.Runtime.CompilerServices;
using Cabazure.Kusto.Processing;
using Kusto.Data.Common;

namespace Cabazure.Kusto.Tests.Processing;

public class StreamQueryHandlerTests
{
    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Calls_QueryProvider(
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoStreamQuery<string> query,
        StreamQueryHandler<string> sut,
        string queryText,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        query.GetQueryText().Returns(queryText);
        query.GetParameters().Returns(parameters);
        query.ReadResults(default!, default).ReturnsForAnyArgs(_ => Empty(cancellationToken));

        await foreach (var _ in sut.ExecuteAsync(cancellationToken))
        {
        }

        _ = queryProvider
            .Received(1)
            .ExecuteQueryAsync(
                null,
                queryText,
                Arg.Is<ClientRequestProperties>(p
                    => p.ClientRequestId != null
                    && p.Parameters.SequenceEqual(query.GetCslParameters())),
                cancellationToken);
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Disposes_Reader_When_Enumerator_Is_Disposed_Early(
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoStreamQuery<string> query,
        StreamQueryHandler<string> sut,
        CancellationToken cancellationToken)
    {
        var reader = new TrackingDataReader();
        queryProvider
            .ExecuteQueryAsync(default, default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResults(default!, default)
            .ReturnsForAnyArgs(_ => YieldMany(cancellationToken));

        await using (var enumerator = sut.ExecuteAsync(cancellationToken).GetAsyncEnumerator(cancellationToken))
        {
            reader.IsDisposed.Should().BeFalse();
            (await enumerator.MoveNextAsync()).Should().BeTrue();
            enumerator.Current.Should().Be("first");
            reader.IsDisposed.Should().BeFalse();
        }

        reader.IsDisposed.Should().BeTrue();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Propagates_Exceptions_And_Disposes_Reader(
        [Frozen] ICslQueryProvider queryProvider,
        [Frozen] IKustoStreamQuery<string> query,
        StreamQueryHandler<string> sut,
        CancellationToken cancellationToken)
    {
        var reader = new TrackingDataReader();
        queryProvider
            .ExecuteQueryAsync(default, default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResults(default!, default)
            .ReturnsForAnyArgs(_ => ThrowAfterFirst(cancellationToken));

        var results = new List<string>();
        var act = async () =>
        {
            await foreach (var item in sut.ExecuteAsync(cancellationToken))
            {
                results.Add(item);
            }
        };

        await act.Should().ThrowAsync<InvalidOperationException>();
        results.Should().Equal("first");
        reader.IsDisposed.Should().BeTrue();
    }

    [Theory, AutoNSubstituteData]
    public async Task ExecuteAsync_Stops_On_Cancellation_And_Disposes_Reader(
        [Frozen] ICslQueryProvider queryProvider)
    {
        using var cts = new CancellationTokenSource();
        var reader = new TrackingDataReader();
        var query = Substitute.For<IKustoStreamQuery<string>>();
        queryProvider
            .ExecuteQueryAsync(default, default, default, default)
            .ReturnsForAnyArgs(reader);
        query
            .ReadResults(default!, default)
            .ReturnsForAnyArgs(_ => YieldUntilCancelled(cts));

        var sut = new StreamQueryHandler<string>(queryProvider, query);
        var act = async () =>
        {
            await foreach (var item in sut.ExecuteAsync(cts.Token))
            {
                item.Should().Be("first");
                cts.Cancel();
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        reader.IsDisposed.Should().BeTrue();
    }

    private static async IAsyncEnumerable<string> Empty(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<string> YieldMany(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return "first";
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return "second";
    }

    private static async IAsyncEnumerable<string> ThrowAfterFirst(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return "first";
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }

    private static async IAsyncEnumerable<string> YieldUntilCancelled(
        CancellationTokenSource cancellationTokenSource)
    {
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
        yield return "first";
        await Task.Yield();
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
        yield return "second";
    }

    private sealed class TrackingDataReader : IDataReader
    {
        public bool IsDisposed { get; private set; }

        public object this[int i] => throw new NotSupportedException();
        public object this[string name] => throw new NotSupportedException();
        public int Depth => throw new NotSupportedException();
        public bool IsClosed => IsDisposed;
        public int RecordsAffected => throw new NotSupportedException();
        public int FieldCount => 0;

        public void Close() => IsDisposed = true;
        public void Dispose() => IsDisposed = true;
        public bool GetBoolean(int i) => throw new NotSupportedException();
        public byte GetByte(int i) => throw new NotSupportedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public char GetChar(int i) => throw new NotSupportedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        public string GetDataTypeName(int i) => throw new NotSupportedException();
        public DateTime GetDateTime(int i) => throw new NotSupportedException();
        public decimal GetDecimal(int i) => throw new NotSupportedException();
        public double GetDouble(int i) => throw new NotSupportedException();
        public Type GetFieldType(int i) => throw new NotSupportedException();
        public float GetFloat(int i) => throw new NotSupportedException();
        public Guid GetGuid(int i) => throw new NotSupportedException();
        public short GetInt16(int i) => throw new NotSupportedException();
        public int GetInt32(int i) => throw new NotSupportedException();
        public long GetInt64(int i) => throw new NotSupportedException();
        public string GetName(int i) => throw new NotSupportedException();
        public int GetOrdinal(string name) => throw new NotSupportedException();
        public DataTable GetSchemaTable() => throw new NotSupportedException();
        public string GetString(int i) => throw new NotSupportedException();
        public object GetValue(int i) => throw new NotSupportedException();
        public int GetValues(object[] values) => throw new NotSupportedException();
        public bool IsDBNull(int i) => throw new NotSupportedException();
        public bool NextResult() => throw new NotSupportedException();
        public bool Read() => throw new NotSupportedException();
    }
}
