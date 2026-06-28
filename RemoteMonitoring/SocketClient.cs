using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace RemoteMonitoring
{
    public class SocketClient
    {
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private int _pingIntervalMs = 5000;
        private int _requestTimeoutMs = 10000;

        public async Task ConnectAsync()
        {
            _client = new TcpClient();

            int retry = 0;
            while (true)
            {
                try
                {
                    await _client.ConnectAsync("127.0.0.1", 5050);

                    NetworkStream stream = _client.GetStream();

                    _reader = new StreamReader(stream, Encoding.UTF8);
                    _writer = new StreamWriter(stream, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };

                    _ = StartPinging();
                }
                catch (Exception ex)
                {
                    if (retry < 10)
                    {
                        retry++;
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        throw;
                    }
                }

                break;
            }
        }

        public async Task<string?> SendAsync(string message)
        {
            if (_reader is null || _writer is null) return null;

            Task writeTask = _writer!.WriteLineAsync(message);
            Task completedWrite = await Task.WhenAny(
                writeTask,
                Task.Delay(_requestTimeoutMs)
            );
            if (completedWrite != writeTask)
                return null; // Timeout

            Task<string?> readTask = _reader.ReadLineAsync();
            Task completedRead = await Task.WhenAny(
                readTask,
                Task.Delay(_requestTimeoutMs)
            );
            if (completedRead != readTask)
                return null; // Timeout

            return await readTask;
        }

        public async Task StartPinging()
        {
            while (true)
            {
                Thread.Sleep(_pingIntervalMs);

                string? response = await SendAsync("Ping");
                if (response is null || !response.Equals("Pong"))
                {
                    Console.WriteLine("PING FAILED");
                    Debug.WriteLine("PING FAILED");
                    break;
                }
            }
        }
    }
}
