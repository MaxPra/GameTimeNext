using GameTimeNext.Shared.Base;
using RemoteMonitoring.ApiServices;

namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Sends the entries queued by FnLog to arcora. While there is no connection the entries stay in the queue,
    /// so the ones logged during a reconnect arrive afterwards.
    /// </summary>
    public class LogForwarder : BackgroundService
    {
        private const int _waitForConnectionMs = 1000;

        private readonly SocketClient _socket;
        private readonly SystemApiService _systemApiService;

        public LogForwarder(SocketClient socket, SystemApiService systemApiService)
        {
            _socket = socket;
            _systemApiService = systemApiService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FnLog.MarkAsForwarder();

            try
            {
                await foreach (LogRequestMessage entry in FnLog.ForwardQueue.ReadAllAsync(stoppingToken))
                {
                    while (_socket.State != ConnectionState.Connected)
                        await Task.Delay(_waitForConnectionMs, stoppingToken);

                    await _systemApiService.SendLog(entry);
                }
            }
            catch (OperationCanceledException)
            {
                // Application is shutting down
            }
        }
    }
}
