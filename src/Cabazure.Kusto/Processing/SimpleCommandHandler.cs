using Kusto.Data.Common;

namespace Cabazure.Kusto.Processing;

public class SimpleCommandHandler(
    ICslAdminProvider adminProvider,
    IKustoCommand command)
    : IScriptHandler
{
    public async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        using var reader = await adminProvider
            .ExecuteControlCommandAsync(
                databaseName: null,
                command.GetQueryText(),
                command.GetRequestProperties());
    }
}
