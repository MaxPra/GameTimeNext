using GameTimeNext.Shared.Base;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace RemoteMonitoring.Base
{
    public class SocketClient
    {
        #region Properties
        private const int _retryTimeoutMs = 1000;
        private const int _slowRetryTimeoutMs = 5000;
        private const int _retriesBeforeDisconnected = 10;
        private CancellationTokenSource? _cts;
        private CancellationToken _stoppingToken;

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
        /// <summary>
        /// Connects to arcora once for the whole web app (called by SocketConnectionService at startup). Never gives up:
        /// a failed or lost connection is retried until stoppingToken is cancelled.
        /// </summary>
        public Task StartAsync(CancellationToken stoppingToken)
        {
            _stoppingToken = stoppingToken;

            ReconnectRequested -= HandleReconnectRequested;
            ReconnectRequested += HandleReconnectRequested;

            return ConnectWithRetryAsync();
        }

        /// <summary>
        /// Sends the request and waits for its answer, null if there is none within the timeout (SocketTimeouts.Request
        /// unless given); a timeout connects again.
        /// </summary>
        public async Task<string?> SendAsync(RequestMessageBase requestMessage, TimeSpan? timeout = null)
        {
            if (_cts is null || _client is null || _reader is null || _writer is null) throw new WrongOrderException();

            TimeSpan requestTimeout = timeout ?? SocketTimeouts.Request;

            await _sendLock.WaitAsync();
            try
            {
                if (State != ConnectionState.Connected || _reader is null || _writer is null)
                    return null;

                string requestJson = JsonSerializer.Serialize(requestMessage, requestMessage.GetType());

                Task writeTask = _writer.WriteLineAsync(requestJson);
                bool writeSuccess = await ExecuteTaskWithTimeoutAsync(writeTask, requestTimeout);
                if (!writeSuccess)
                {
                    ReconnectRequested?.Invoke(this, EventArgs.Empty);
                    return null;
                }

                Task<string?> readTask = _reader.ReadLineAsync();
                bool readSuccess = await ExecuteTaskWithTimeoutAsync(readTask, requestTimeout);
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

        public async Task DisconnectAsync()
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
        #endregion

        #region Methods PRIVATE
        /// <summary>
        /// Tries to connect until it succeeds or the app stops: every second at first, after that every few seconds
        /// with the state Disconnected.
        /// </summary>
        private async Task ConnectWithRetryAsync()
        {
            if (State != ConnectionState.Reconnecting) FnLog.AddInfo(this, "Connecting...");

            int failedAttempts = 0;

            while (!_stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync();
                    _pingTask = PingLoopAsync(_cts!.Token);
                    return;
                }
                catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    CloseConnection();

                    failedAttempts++;
                    if (failedAttempts == _retriesBeforeDisconnected)
                    {
                        FnLog.AddError(this, $"No connection after {failedAttempts} attempts, retrying every {_slowRetryTimeoutMs / 1000} s...", ex);
                        State = ConnectionState.Disconnected;
                    }
                }

                try
                {
                    await Task.Delay(failedAttempts < _retriesBeforeDisconnected ? _retryTimeoutMs : _slowRetryTimeoutMs, _stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task ConnectAsync()
        {
            // A new client per attempt: a socket whose connect failed can't be used again
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken);
            _client = new TcpClient();

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
                    return ConnectWithRetryAsync();
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

                    string? response = await SendAsync(new PingRequestMessage(), SocketTimeouts.Ping);
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

        private async Task<bool> ExecuteTaskWithTimeoutAsync(Task desiredTask, TimeSpan timeout)
        {
            Task completedWrite = await Task.WhenAny(
                desiredTask,
                Task.Delay(timeout, _cts!.Token)
            );

            if (completedWrite != desiredTask)
                return false; // Timeout

            if (desiredTask.IsFaulted)
                return false;

            return true;
        }
        #endregion

        public class WrongOrderException : Exception
        {
            public WrongOrderException() : base("Wrong order, idiot!") { }
        }
    }
}
