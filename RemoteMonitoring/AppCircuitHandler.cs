using Microsoft.AspNetCore.Components.Server.Circuits;

namespace RemoteMonitoring
{
    public class AppCircuitHandler : CircuitHandler
    {
        public CancellationTokenSource Cts { get; } = new();

        public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            Cts.Cancel();
            return Task.CompletedTask;
        }
    }
}
