using GameTimeNext.Core.Framework.Logging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    public class RemoteMonitoringSocketServer : UIXBackgroundProcess
    {
        private TcpListener? _listener;

        public int Port { get; set; }

        public RemoteMonitoringSocketServer() : base()
        {
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();
        }

        public override async Task LogicAsync()
        {
            if (_listener is null) return;

            try
            {
                var client = await _listener.AcceptTcpClientAsync(Cts!.Token);
                _ = Task.Run(async () => await HandleClient(client));
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };

            while (client.Connected)
            {
                string? message = await reader.ReadLineAsync();

                if (message == null)
                    break;

                FnLog.AddInfo(this, $"Received: {message}");

                await writer.WriteLineAsync(message);
            }
        }

        protected override void OnStop()
        {
            _listener?.Stop(); 
        }

        public override void InitializeApplicationOutput()
        {

        }

        protected override void InitializeInfos()
        {

        }
    }
}
