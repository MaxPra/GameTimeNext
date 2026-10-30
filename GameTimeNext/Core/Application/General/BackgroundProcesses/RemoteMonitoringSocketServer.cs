using GameTimeNext.Core.Application.Profiles.Batch;
using GameTimeNext.Core.Framework;
using GameTimeNext.Shared;
using GameTimeNext.Shared.Base;
using GameTimeNext.Shared.Profiles;
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

        public override void Init()
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
            catch (Exception ex)
            {
                FnLog.AddError(this, "Error in LogicAsync", ex);
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

                await HandleRequest(writer, message);
            }
        }

        private async Task HandleRequest(StreamWriter writer, string requestMessage)
        {
            using JsonDocument document = JsonDocument.Parse(requestMessage);
            AvailableRequests request = document.RootElement.GetProperty(nameof(RequestMessageBase.Request)).Deserialize<AvailableRequests>();

            if (request.Equals(AvailableRequests.Ping))
            {
                writer.WriteLine("Pong");
                return;
            }

            FnLog.AddInfo(this, $"Received: {request.ToString()}");
            // OFDOI ProfilesBatchApp
            //ProfilesBatchApp profilesBatchApp = GetProfilesBatchApp();
            //if (profilesBatchApp is null)
            //{
            //    writer.WriteLine(string.Empty);
            //    return;
            //}

            // OFDOI: ?Generic? conversion between T1 and Dto

            switch (request)
            {
                case AvailableRequests.AllProfiles:
                    List<ProfileDto> t1profis = new List<ProfileDto>
                    {
                        new ProfileDto
                        {
                            Id = 0,
                            Title = "over the hill Demo",
                            AccentColorHex = "4FBAD9",
                            //ImageUrl = "https://cdn2.steamgriddb.com/thumb/02410847e3dabcc0a3c4889f1abaf025.jpg"
                        },
                        new ProfileDto
                        {
                            Id = 1,
                            Title = "Euro Truck Simulator 2",
                            AccentColorHex = "EC9C3C",
                            //ImageUrl = "https://cdn2.steamgriddb.com/thumb/8227c2ce0e9af9745a927e89d6107994.jpg"
                        },
                        new ProfileDto
                        {
                            Id = 2,
                            Title = "Palworld",
                            AccentColorHex = "1B91CC",
                        },
                        new ProfileDto
                        {
                            Id = 3,
                            Title = "BioShock Remastered",
                            AccentColorHex = "0A6066",
                        },
                        new ProfileDto
                        {
                            Id = 4,
                            Title = "Paralives",
                            AccentColorHex = "1EC4E9",
                        },
                        new ProfileDto
                        {
                            Id = 5,
                            Title = "Minecraft (1.2.5)",
                            AccentColorHex = "2F47B6",
                        },
                        new ProfileDto
                        {
                            Id = 6,
                            Title = "Portal Reloaded",
                            AccentColorHex = "15940F",
                        },
                        new ProfileDto
                        {
                            Id = 7,
                            Title = "Red Dead Redemption 2",
                            AccentColorHex = "B0311B",
                        },
                        new ProfileDto
                        {
                            Id = 8,
                            Title = "Portal Stories: Mel",
                            AccentColorHex = "C45626",
                        },
                        new ProfileDto
                        {
                            Id = 9,
                            Title = "Thinking with Time Machine",
                            AccentColorHex = "15B0D5",
                        },
                        new ProfileDto
                        {
                            Id = 10,
                            Title = "Portal 2: Community Edition",
                            AccentColorHex = "D15C23",
                        }
                    };
                    await SendResponse(writer, t1profis);
                    return;
                case AvailableRequests.Profile:
                    ProfileDto t1profi = new ProfileDto
                    {
                        Id = 1,
                        Title = "Euro Truck Simulator 2",
                        AccentColorHex = "EC9C3C",
                        //ImageUrl = "https://cdn2.steamgriddb.com/thumb/8227c2ce0e9af9745a927e89d6107994.jpg"
                    };
                    await SendResponse(writer, t1profi);
                    return;
            }
        }

        private async Task SendResponse(StreamWriter writer, object response)
        {
            string serialized = JsonSerializer.Serialize(response);
            writer.WriteLine(serialized);
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

        private ProfilesBatchApp GetProfilesBatchApp()
        {
            if (AppEnvironment.StartedApplications.ContainsKey(typeof(ProfilesBatchApp).FullName!)
                && AppEnvironment.StartedApplications[typeof(ProfilesBatchApp).FullName!] is ProfilesBatchApp profilesBatchApp)
                return profilesBatchApp;

            return null!;
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(RemoteMonitoringSocketServer));
        }
    }
}
