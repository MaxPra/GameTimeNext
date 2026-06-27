using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Logging;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    public class RemoteMonitoringProcess : UIXBackgroundProcess
    {
        private Process? _blazorProcess;
        
        public override void Logic()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.RemoteMonitoring) return;

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
            FnLog.AddInfo(this, "Starting...");

            int port;
            try
            {
                port = GetFreePort();
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Could not get free port.", ex);
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? exePath = Directory.GetFiles(baseDir, "*RemoteMonitoring*.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (exePath is null)
            {
                FnLog.AddError(this, "No executable found in application folder.");
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
                psi.Environment["ASPNETCORE_URLS"] = $"http://localhost:{port}";

                _blazorProcess = Process.Start(psi);
                FnLog.AddInfo(this, $"Started blazorProcess on port {port}.");
                CallDispatcher!.Trigger("EXEV_remoteMonitoringPortChanged", port);
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Failed to start blazorProcess.", ex);
                _blazorProcess = null;
            }
        }

        private int GetFreePort()
        {
            int port = 0;
            using (TcpListener l = new TcpListener(IPAddress.Loopback, port))
            {

                l.Start();
                port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
            }
            return port;
        }

        protected override void OnStop()
        {
            if (_blazorProcess is null) return;

            try
            {
                _blazorProcess.Kill();
                _blazorProcess.WaitForExit(1000);
                _blazorProcess.Dispose();
                _blazorProcess = null;

                FnLog.AddInfo(this, $"Stopped blazorProcess.");
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, $"Stopping blazorProcess failed.", ex);
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