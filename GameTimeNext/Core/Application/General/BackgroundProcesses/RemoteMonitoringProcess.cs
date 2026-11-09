using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    public class RemoteMonitoringProcess : UIXBackgroundProcess
    {
        private static bool _DEBUG_STARTBLAZOR = false;
        private const int _MAX_BLAZOR_ERROR_OUTPUT = 32000;

        private Process? _blazorProcess;
        private RemoteMonitoringSocketServer? _socketServerProcess;

        // Error output of the Blazor process (e.g. an unhandled exception at startup), logged when it exits
        private readonly StringBuilder _blazorErrorOutput = new();

        private int _socketPort = 5050;
        
        public override void Logic()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.RemoteMonitoring)
            {
                StopRemoteMonitoring();
                return;
            }

            HandleSocket();

            if (!FnSystem.IsDebug() || _DEBUG_STARTBLAZOR)
                HandleBlazor();
        }

        protected override void OnStop()
        {
            StopRemoteMonitoring();
        }

        public override void InitializeApplicationOutput()
        {

        }

        protected override void InitializeInfos()
        {
            ProcessName = "RemoteMonitoringProcess";
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(RemoteMonitoringProcess));
        }

        private void HandleSocket()
        {
            if (_socketServerProcess is not null) return;

            FnLog.AddInfo(this, "Starting socketServerProcess...");
            if (!FnSystem.IsDebug())
            {
                try
                {
                    _socketPort = GetFreePort(desiredPort: _socketPort);
                }
                catch (Exception ex)
                {
                    FnLog.AddError(this, "Could not get free port.", ex);
                    return;
                }
            }

            // Run socket server
            _socketServerProcess = GetBackgroundProcess<RemoteMonitoringSocketServer>();
            _socketServerProcess.CallDispatcher = CallDispatcher;
            _socketServerProcess.Port = _socketPort;
            _socketServerProcess.Start(1000, runAsync: true);
            AppEnvironment.StartedBackgroundProcesses.Add(typeof(RemoteMonitoringSocketServer).FullName!, _socketServerProcess);
            FnLog.AddInfo(this, $"Started Socket-Server process on port {_socketPort}.");
        }

        private void HandleBlazor()
        {
            // CleanUp, when blazorProcess has exited
            if (_blazorProcess is not null && _blazorProcess.HasExited)
            {
                // Lets the error output be read to its end
                _blazorProcess.WaitForExit(2000);
                LogBlazorExit(_blazorProcess.ExitCode);

                _blazorProcess.Dispose();
                _blazorProcess = null;
            }

            // blazorProcess is running
            if (_blazorProcess is not null)
                return;

            // Start blazorProcess
            FnLog.AddInfo(this, "Starting Blazor process...");

            int port = 5131;
            if (!FnSystem.IsDebug())
            {
                try
                {
                    port = GetFreePort(desiredPort: port, IPAddress.Any);
                }
                catch (Exception ex)
                {
                    FnLog.AddError(this, "Could not get free port.", ex);
                    return;
                }
            }
            CallDispatcher!.Trigger("EXEV_RemoteMonitoringPortChanged", port); // SettingsViewController

            string baseDir = Path.Combine(AppConfig.Root.ApplicationDirectoryPath, "RemoteMonitoring");
            FnDirectory.EnsureDirectoryExists(baseDir);
            string? exePath = Directory.GetFiles(baseDir, "*RemoteMonitoring*.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (exePath is null && FnSystem.IsDebug())
            {
                baseDir = Path.Combine(baseDir, AppConfig.Dev.RemoteMonitoringBinDirectoryPath);
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
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? baseDir
                };
                psi.Environment["ASPNETCORE_URLS"] = $"http://0.0.0.0:{port}";
                psi.Environment["GTN_SOCKET_PORT"] = $"{_socketPort}";

                lock (_blazorErrorOutput)
                    _blazorErrorOutput.Clear();

                _blazorProcess = Process.Start(psi);
                if (_blazorProcess is not null)
                {
                    FnChildProcesses.Attach(_blazorProcess);
                    _blazorProcess.ErrorDataReceived += BlazorErrorDataReceived;
                    _blazorProcess.BeginErrorReadLine();
                }

                FnLog.AddInfo(this, $"Started blazorProcess on port {port}.");
                CallDispatcher!.Trigger("EXEV_RemoteMonitoringStarted", port); // MainWindowController
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Failed to start blazorProcess.", ex);
                _blazorProcess = null;
            }
        }

        private void BlazorErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
                return;

            lock (_blazorErrorOutput)
            {
                // The beginning holds the exception, a process that keeps writing must not fill the memory
                if (_blazorErrorOutput.Length < _MAX_BLAZOR_ERROR_OUTPUT)
                    _blazorErrorOutput.AppendLine(e.Data);
            }
        }

        /// <summary>
        /// Logs the end of the Blazor process; a crash as error with its error output (e.g. the unhandled exception), which is
        /// otherwise lost because the process has no window.
        /// </summary>
        private void LogBlazorExit(int exitCode)
        {
            string errorOutput;
            lock (_blazorErrorOutput)
            {
                errorOutput = _blazorErrorOutput.ToString().TrimEnd();
                _blazorErrorOutput.Clear();
            }

            if (exitCode == 0 && errorOutput.Length == 0)
            {
                FnLog.AddInfo(this, $"Blazor process exited with code {exitCode}.");
                return;
            }

            FnLog.AddError(this, $"Blazor process exited with code {exitCode} (0x{exitCode:X8}).",
                errorOutput.Length == 0 ? null : new ForwardedException(errorOutput));
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

        private void StopRemoteMonitoring()
        {
            if (_blazorProcess is not null)
            {
                try
                {
                    _blazorProcess.Kill(true);
                    _blazorProcess.WaitForExit(5000);

                    FnLog.AddInfo(this, $"Stopped Blazor process.");
                }
                catch (Exception ex)
                {
                    FnLog.AddError(this, $"Stopping Blazor process failed.", ex);
                }
                finally
                {
                    _blazorProcess.Dispose();
                    _blazorProcess = null;
                }
            }

            if (_socketServerProcess is not null)
            {
                _socketServerProcess.Stop();
                AppEnvironment.StartedBackgroundProcesses.Remove(typeof(RemoteMonitoringSocketServer).FullName!);
                _socketServerProcess = null;

                FnLog.AddInfo(this, $"Stopped Socket-Server process.");
            }
        }
    }
}
