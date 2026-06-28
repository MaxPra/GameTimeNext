using System.Net.Sockets;
using System.Text;

namespace RemoteMonitoring
{
    public class SocketClient
    {
        #region Properties
        private const int _requestTimeoutMs = 10000;
        private const int _retryTimeoutMs = 1000;
        private CancellationTokenSource? _cts;

        private const string _address = "127.0.0.1";
        private const int _port = 5050;
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private ConnectionState _state = ConnectionState.NeverConnected;
        public ConnectionState State
        {
            get => _state;
            set
            {
                _state = value;
                ConnectionStateChanged?.Invoke(this, _state);
            }
        }
        public event EventHandler<ConnectionState>? ConnectionStateChanged;

        private Task? _pingTask;
        private const int _pingIntervalMs = 5000;

        private event EventHandler? ReconnectRequested;
        #endregion

        #region Methods PUBLIC
        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _client = new TcpClient();

            ReconnectRequested -= HandleReconnectRequested;
            ReconnectRequested += HandleReconnectRequested;

            try
            {
                await ExecuteTaskWithRetryAsync(ConnectAsync);
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Error while connecting...", ex);
                State = ConnectionState.Disconnected;
                return;
            }

            _pingTask = PingLoopAsync();
        }

        public async Task<string?> SendAsync(string requestMessage)
        {
            if (_cts is null || _client is null || _reader is null || _writer is null) throw new WrongOrderException();

            Task writeTask = _writer.WriteLineAsync(requestMessage);
            bool writeSuccess = await ExecuteTaskWithTimeoutAsync(writeTask);
            if (!writeSuccess)
                return null;

            Task<string?> readTask = _reader.ReadLineAsync();
            bool readSuccess = await ExecuteTaskWithTimeoutAsync(readTask);
            if (!readSuccess)
                return null;

            return await readTask;
        }

        public void Reset()
        {
            State = ConnectionState.NeverConnected;
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
        }
        #endregion

        #region Methods PRIVATE
        private async Task ConnectAsync()
        {
            if (_cts is null || _client is null) throw new WrongOrderException();

            if (!State.Equals(ConnectionState.Reconnecting)) FnLog.AddInfo(this, "Connecting...");

            await _client.ConnectAsync(_address, _port, _cts.Token);
            NetworkStream stream = _client.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };

            State = ConnectionState.Connected;
            FnLog.AddInfo(this, "Connected.");
        }

        private async Task DisconnectAsync()
        {
            try
            {
                FnLog.AddInfo(this, "Disconnecting...");

                ReconnectRequested -= HandleReconnectRequested;

                _cts?.Cancel();

                _writer?.Dispose();
                _reader?.Dispose();
                _client?.Dispose();

                State = ConnectionState.Disconnected;

                if (_pingTask is not null)
                    await _pingTask;
            }
            catch (Exception ex)
            {
                FnLog.AddWarning(this, "Error while disconnecting...", ex);
            }
        }

        private void HandleReconnectRequested(object? sender, EventArgs e)
        {
            State = ConnectionState.Reconnecting;
            FnLog.AddInfo(this, "Reconnecting...");
            _ = StartAsync();
        }

        private async Task PingLoopAsync()
        {
            FnLog.AddInfo(this, "Starting ping loop...");
            while (!_cts!.IsCancellationRequested)
            {
                await Task.Delay(_pingIntervalMs, _cts!.Token);

                string? response = await SendAsync("Ping");
                if (response is null || !response.Equals("Pong"))
                {
                    FnLog.AddError(this, "Ping failed!");
                    ReconnectRequested?.Invoke(this, new EventArgs());
                    break;
                }
            }
        }

        private async Task<bool> ExecuteTaskWithTimeoutAsync(Task desiredTask)
        {
            Task completedWrite = await Task.WhenAny(
                desiredTask,
                Task.Delay(_requestTimeoutMs, _cts!.Token)
            );

            if (completedWrite != desiredTask)
                return false; // Timeout

            if (desiredTask.IsFaulted)
                return false;

            return true;
        }

        private async Task ExecuteTaskWithRetryAsync(Func<Task> desiredTask, int retryCount = 10)
        {
            if (_cts is null) throw new WrongOrderException();

            int currentRetry = 0;

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await desiredTask();

                    // Success
                    return;
                }
                catch (Exception)
                {
                    if (currentRetry == retryCount)
                    {
                        // Max retries reached
                        throw;
                    }

                    // Retry
                    currentRetry++;
                    await Task.Delay(_retryTimeoutMs);
                    continue;
                }
            }

            throw new Exception("Something unexpected happened inside retry logic.");
        }
        #endregion

        public class WrongOrderException : Exception
        {
            public WrongOrderException() : base("Wrong order, idiot!") { }
        }
    }

    public enum ConnectionState
    {
        NeverConnected = 0,
        Connected = 1,
        Reconnecting = 2,
        Disconnected = 9,
    }
}
