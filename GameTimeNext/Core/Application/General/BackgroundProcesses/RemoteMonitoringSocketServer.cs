using GameTimeNext.Core.Application.Profiles.Batch;
using GameTimeNext.Shared;
using GameTimeNext.Shared.Base;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    public class RemoteMonitoringSocketServer : UIXBackgroundProcess
    {
        private TcpListener? _listener;

        public int Port { get; set; }

        private ProfilesBatchApp _profilesBatchApp = null!;

        public override void Init()
        {
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();

            InitBatchApps();
        }

        private void InitBatchApps()
        {
            _profilesBatchApp = GetApplication<ProfilesBatchApp>();
            _profilesBatchApp.Start(HostApplication);
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
            catch (Exception ex)
            {
                FnLog.AddError(this, "Error in LogicAsync", ex);
            }
        }

        protected override void OnStop()
        {
            _listener?.Stop();
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(RemoteMonitoringSocketServer));
        }

        protected override void InitializeInfos()
        {
        }

        public override void InitializeApplicationOutput()
        {
        }

        private async Task HandleClient(TcpClient client)
        {
            CancellationToken token = Cts!.Token;

            try
            {
                using (client)
                {
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    using var writer = new StreamWriter(stream, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };

                    FnLog.AddInfo(this, "Client connected.");

                    while (!token.IsCancellationRequested)
                    {
                        string? message = await reader.ReadLineAsync(token);

                        if (message == null)
                            break;

                        await HandleRequest(writer, message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                FnLog.AddInfo(this, "Client disconnected by server.");
                // Server stopped: the connection is closed with the client
            }
            catch (IOException)
            {
                FnLog.AddInfo(this, "Client disconnected by user.");
                // Connection closed by the client (e.g. the Blazor process was ended)
            }
        }

        private async Task HandleRequest(StreamWriter writer, string requestMessage)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(requestMessage);
                AvailableRequests request = document.RootElement.GetProperty(nameof(RequestMessageBase.Request)).Deserialize<AvailableRequests>();

                if (request.Equals(AvailableRequests.Ping))
                {
                    writer.WriteLine("Pong");
                    return;
                }

                FnLog.AddInfo(this, $"Received: {request.ToString()}");

                switch (request)
                {
                    // System
                    case AvailableRequests.Log:
                        // OFDOI: Log
                        await SendResponse(writer, true);
                        return;

                    // Profiles
                    case AvailableRequests.AllProfiles:
                        await SendResponse(writer, _profilesBatchApp.GetAllProfiles());
                        return;
                    case AvailableRequests.Profile:
                        ProfileRequestMessage reqMsg = JsonSerializer.Deserialize<ProfileRequestMessage>(requestMessage)!;
                        await SendResponse(writer, _profilesBatchApp.GetProfile(reqMsg.PFID));
                        return;

                    default:
                        FnLog.AddWarning(this, $"Request {request} is not supported.");
                        await SendResponse(writer, null);
                        return;
                }
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, $"Handling the request failed: {requestMessage}", ex);
                await SendResponse(writer, null);
            }
        }

        private async Task SendResponse(StreamWriter writer, object? response)
        {
            string serialized = JsonSerializer.Serialize(response);
            writer.WriteLine(serialized);
        }
    }
}
