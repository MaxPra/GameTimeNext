namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Holds the one connection of the web app to arcora: connects at startup (independent of page loads,
    /// so logs are forwarded before anyone opens a page) and disconnects when the app stops.
    /// </summary>
    public class SocketConnectionService : BackgroundService
    {
        private readonly SocketClient _socket;

        public SocketConnectionService(SocketClient socket)
        {
            _socket = socket;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _socket.StartAsync(stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await _socket.DisconnectAsync();
        }
    }
}
