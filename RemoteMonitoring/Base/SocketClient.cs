using GameTimeNext.Shared.Base;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace RemoteMonitoring.Base
{
    public class SocketClient
    {
        #region Properties
        private const int _requestTimeoutMs = 10000;
        private const int _retryTimeoutMs = 1000;
        private CancellationTokenSource? _cts;

        private const string _address = "127.0.0.1";
        private int _port = int.TryParse(Environment.GetEnvironmentVariable("GTN_SOCKET_PORT"), out var value) ? value : 5050;
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
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

            _pingTask = PingLoopAsync(_cts.Token);
        }

        public async Task<string?> SendAsync(RequestMessageBase requestMessage)
        {
            if (_cts is null || _client is null || _reader is null || _writer is null) throw new WrongOrderException();

            await _sendLock.WaitAsync();
            try
            {
                if (State != ConnectionState.Connected || _reader is null || _writer is null)
                    return null;

                string requestJson = JsonSerializer.Serialize(requestMessage, requestMessage.GetType());

                Task writeTask = _writer.WriteLineAsync(requestJson);
                bool writeSuccess = await ExecuteTaskWithTimeoutAsync(writeTask);
                if (!writeSuccess)
                {
                    ReconnectRequested?.Invoke(this, EventArgs.Empty);
                    return null;
                }

                Task<string?> readTask = _reader.ReadLineAsync();
                bool readSuccess = await ExecuteTaskWithTimeoutAsync(readTask);
                if (!readSuccess)
                {
                    ReconnectRequested?.Invoke(this, EventArgs.Empty);
                    return null;
                }

                return await readTask;
            }
            finally
            {
                _sendLock.Release();
            }
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

                CloseConnection();

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
            if (State == ConnectionState.Reconnecting)
                return;

            State = ConnectionState.Reconnecting;

            CloseConnection();

            // Not in the caller's context: a reconnect requested by the LogForwarder would pass its "don't forward" flag
            // to the new connection and its ping loop
            using (ExecutionContext.SuppressFlow())
            {
                _ = Task.Run(() =>
                {
                    FnLog.AddInfo(this, "Reconnecting...");
                    return StartAsync();
                });
            }
        }

        private void CloseConnection()
        {
            _cts?.Cancel();
            _writer?.Dispose();
            _reader?.Dispose();
            _client?.Dispose();
        }

        private async Task PingLoopAsync(CancellationToken token)
        {
            FnLog.AddInfo(this, "Starting ping loop...");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(_pingIntervalMs, token);

                    string? response = await SendAsync(new PingRequestMessage());
                    if (!IsPong(response))
                    {
                        FnLog.AddError(this, "Ping failed!");
                        ReconnectRequested?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Connection closed (reconnect or disconnect)
            }
        }

        /// <summary>
        /// True if the response is the JSON string "Pong". Anything else (no response, no valid JSON) counts as a failed ping.
        /// </summary>
        private static bool IsPong(string? response)
        {
            if (response is null)
                return false;

            try
            {
                return JsonSerializer.Deserialize<string>(response) == "Pong";
            }
            catch (JsonException)
            {
                return false;
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
