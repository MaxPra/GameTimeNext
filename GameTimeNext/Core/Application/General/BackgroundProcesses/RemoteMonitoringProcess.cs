using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Logging;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    public class RemoteMonitoringProcess : UIXBackgroundProcess
    {
        private Process? _blazorProcess;
        private RemoteMonitoringSocketServer? _socketServerProcess;

        private int _socketPort = 5050;
        
        public override void Logic()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.RemoteMonitoring) return;

            HandleSocket();
            HandleBlazor();
        }

        private void HandleSocket()
        {
            if (_socketServerProcess is not null) return;

            FnLog.AddInfo(this, "Starting socketServerProcess...");
            try
            {
                _socketPort = GetFreePort(desiredPort: _socketPort);
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Could not get free port.", ex);
                return;
            }

            // Run socket server
            _socketServerProcess = GetBackgroundProcess<RemoteMonitoringSocketServer>();
            _socketServerProcess.CallDispatcher = CallDispatcher;
            _socketServerProcess.Port = _socketPort;
            _socketServerProcess.Start(1000, runAsync: true);
            AppEnvironment.StartedBackgroundProcesses.Add(typeof(RemoteMonitoringSocketServer).FullName!, _socketServerProcess);
            FnLog.AddInfo(this, $"Started socketServerProcess on port {_socketPort}.");
        }

        private void HandleBlazor()
        {
            // CleanUp, when balzorProcess has exited
            if (_blazorProcess is not null && _blazorProcess.HasExited)
            {
                FnLog.AddInfo(this, $"Blazor process exited with code {_blazorProcess.ExitCode}.");
                _blazorProcess.Dispose();
                _blazorProcess = null;
            }

            // blazorProcess is running
            if (_blazorProcess is not null)
                return;

            // Start blazorProcess
            FnLog.AddInfo(this, "Starting blazorProcess...");

            int port = 50505;
            try
            {
                port = GetFreePort(desiredPort: port, IPAddress.Any);
                CallDispatcher!.Trigger("EXEV_RemoteMonitoringPortChanged", port); // SettingsViewController
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Could not get free port.", ex);
                return;
            }

            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RemoteMonitoring");
            string? exePath = Directory.GetFiles(baseDir, "*RemoteMonitoring*.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (exePath is null && FnSystem.IsDebug())
            {
                baseDir = Path.Combine(baseDir, @"..\..\..\..\RemoteMonitoring\bin\Release\net10.0\publish");
                exePath = Directory.GetFiles(baseDir, "*RemoteMonitoring*.exe", SearchOption.AllDirectories).FirstOrDefault();
            }

            if (exePath is null)
            {
                FnLog.AddError(this, "No executable found in application folder.");
                FnLog.AddInfo(this, baseDir);
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? baseDir
                };
                psi.Environment["ASPNETCORE_URLS"] = $"http://0.0.0.0:{port}";
                psi.Environment["GTN_SOCKET_PORT"] = $"{_socketPort}";

                _blazorProcess = Process.Start(psi);
                FnLog.AddInfo(this, $"Started blazorProcess on port {port}.");
                CallDispatcher!.Trigger("EXEV_RemoteMonitoringStarted", port); // MainWindowController
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Failed to start blazorProcess.", ex);
                _blazorProcess = null;
            }
        }

        private int GetFreePort(int? desiredPort = null, IPAddress? desiredEndpoint = null)
        {
            int port = 0;
            if (desiredEndpoint is null) desiredEndpoint = IPAddress.Loopback;

            // Try get desired port
            if (desiredPort is not null && desiredPort != 0)
            {
                try
                {
                    using (TcpListener l = new TcpListener(desiredEndpoint, (int)desiredPort))
                    {
                        l.Start();
                        port = ((IPEndPoint)l.LocalEndpoint).Port;
                        l.Stop();
                    }

                    return port;
                }
                catch
                {
                    // ignored -> Desired port not free
                }
            }

            // Get random port
            using (TcpListener l = new TcpListener(desiredEndpoint, port))
            {

                l.Start();
                port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
            }

            return port;
        }

        protected override void OnStop()
        {
            if (_blazorProcess is not null)
            {
                try
                {
                    _blazorProcess.Kill(true);
                    _blazorProcess.WaitForExit(5000);
                    _blazorProcess.Dispose();
                    _blazorProcess = null;

                    FnLog.AddInfo(this, $"Stopped blazorProcess.");
                }
                catch (Exception ex)
                {
                    FnLog.AddError(this, $"Stopping blazorProcess failed.", ex);
                }
            }

            if (_socketServerProcess is not null)
            {
                _socketServerProcess.Stop();
            }
        }

        public override void InitializeApplicationOutput()
        {

        }

        protected override void InitializeInfos()
        {
            ProcessName = "RemoteMonitoringProcess";
        }
    }
}