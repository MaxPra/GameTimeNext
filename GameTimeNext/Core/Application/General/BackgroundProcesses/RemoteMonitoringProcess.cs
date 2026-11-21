using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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

        // Set from the UI thread ("Reset pairing"), handled in the next cycle of the process thread
        private volatile bool _webAppRestartRequested;

        // Written by the process thread, read by others (module "Diagnostics")
        private readonly object _blazorStatusLock = new();
        private BlazorProcessStatus _blazorStatus = BlazorProcessStatus.NotStarted;

        /// <summary>
        /// Current state of the Blazor process (thread safe snapshot).
        /// </summary>
        public BlazorProcessStatus BlazorStatus
        {
            get { lock (_blazorStatusLock) return _blazorStatus; }
            private set { lock (_blazorStatusLock) _blazorStatus = value; }
        }
        
        public override void Logic()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.RemoteMonitoring)
            {
                StopRemoteMonitoring();

                if (BlazorStatus.State == BlazorProcessState.StartedSeparately)
                    BlazorStatus = BlazorProcessStatus.NotStarted;

                return;
            }

            HandleSocket();

            if (_webAppRestartRequested)
            {
                _webAppRestartRequested = false;
                FnLog.AddInfo(this, "Restarting the web app (pairing reset).");
                StopBlazor();
            }

            if (!FnSystem.IsDebug() || _DEBUG_STARTBLAZOR)
                HandleBlazor();
            else
                BlazorStatus = new BlazorProcessStatus(BlazorProcessState.StartedSeparately);
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
            _socketServerProcess.Port = _socketPort;
            _socketServerProcess.Start(1000, runAsync: true);
            AppEnvironment.StartedBackgroundProcesses.Add(typeof(RemoteMonitoringSocketServer).FullName!, _socketServerProcess);
            FnLog.AddInfo(this, $"Started Socket-Server process on port {_socketPort}.");

            if (FnPowerRequest.KeepSystemAwake("Remote monitoring is on"))
                FnLog.AddInfo(this, "The PC is kept awake while the remote monitoring is on.");
        }

        private void HandleBlazor()
        {
            // CleanUp, when blazorProcess has exited
            if (_blazorProcess is not null && _blazorProcess.HasExited)
            {
                // Lets the error output be read to its end
                _blazorProcess.WaitForExit(2000);
                LogBlazorExit(_blazorProcess.ExitCode);
                BlazorStatus = BlazorStatus with { State = BlazorProcessState.Exited, EndedAt = DateTime.Now, ExitCode = _blazorProcess.ExitCode };

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
                    port = GetFreeWebAppPort(desiredPort: port);
                }
                catch (Exception ex)
                {
                    FnLog.AddError(this, "Could not get free port.", ex);
                    return;
                }
            }
            string? exePath = FindWebAppExecutable();
            if (exePath is null)
            {
                FnLog.AddError(this, "No executable of the web app found in the application folder.");
                return;
            }

            string baseDir = Path.GetDirectoryName(exePath)!;

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

                if (_blazorProcess is not null)
                {
                    BlazorProcessStatus previous = BlazorStatus;
                    BlazorStatus = new BlazorProcessStatus(
                        BlazorProcessState.Running,
                        _blazorProcess.Id,
                        port,
                        DateTime.Now,
                        RestartCount: previous.State == BlazorProcessState.Exited ? previous.RestartCount + 1 : previous.RestartCount);
                }

                FnLog.AddInfo(this, $"Started blazorProcess on port {port}.");
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Failed to start blazorProcess.", ex);
                _blazorProcess = null;
                return;
            }

            NotifyWebAppStarted(port);
        }

        private void NotifyWebAppStarted(int port)
        {
            try
            {
                CallDispatcher!.Trigger("EXEV_RemoteMonitoringStarted", port); // MainWindowController

                // Only registered while the settings are open
                if (CallDispatcher.IsRegistered("EXEV_RemoteMonitoringPortChanged"))
                    CallDispatcher.Trigger("EXEV_RemoteMonitoringPortChanged", port); // SettingsViewController
            }
            catch (Exception ex)
            {
                FnLog.AddError(this, "Failed to notify the UI about the started web app.", ex);
            }
        }

        public static string? FindWebAppExecutable()
        {
            string baseDir = Path.Combine(AppConfig.Root.ApplicationDirectoryPath, "RemoteMonitoring");
            FnDirectory.EnsureDirectoryExists(baseDir);
            string? exePath = Directory.GetFiles(baseDir, "*RemoteMonitoring*.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (exePath is null && FnSystem.IsDebug())
            {
                string devDir = Path.GetFullPath(Path.Combine(baseDir, AppConfig.Dev.RemoteMonitoringBinDirectoryPath));
                if (Directory.Exists(devDir))
                    exePath = Directory.GetFiles(devDir, "*RemoteMonitoring*.exe", SearchOption.AllDirectories).FirstOrDefault();
            }

            return exePath is null ? null : Path.GetFullPath(exePath);
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

        /// <summary>
        /// Port for the web app, which listens on all addresses: the desired one or the next one no listener uses.
        /// Checked against the active listeners instead of a test listener: a listener of arcora on all addresses
        /// makes Windows ask to allow arcora itself in the firewall (only the web app needs the rule).
        /// </summary>
        private int GetFreeWebAppPort(int desiredPort)
        {
            HashSet<int> usedPorts = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Select(endpoint => endpoint.Port).ToHashSet();

            for (int port = desiredPort; port < desiredPort + 100 && port <= IPEndPoint.MaxPort; port++)
            {
                if (!usedPorts.Contains(port))
                    return port;
            }

            // All taken: a random port, tested on loopback only (no firewall prompt)
            return GetFreePort();
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

        /// <summary>
        /// Restarts the web app within the next cycle (5 s): ends every open page on the phones, they load again and
        /// have to be paired again after "Reset pairing". Does nothing to a web app started separately (debug).
        /// </summary>
        public void RequestWebAppRestart()
        {
            _webAppRestartRequested = true;
        }

        private void StopRemoteMonitoring()
        {
            StopBlazor();
            StopSocketServer();
        }

        private void StopBlazor()
        {
            if (_blazorProcess is not null)
            {
                try
                {
                    _blazorProcess.Kill(true);
                    _blazorProcess.WaitForExit(5000);

                    FnLog.AddInfo(this, $"Stopped Blazor process.");
                    BlazorStatus = BlazorStatus with { State = BlazorProcessState.Stopped, EndedAt = DateTime.Now, ExitCode = null };
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
        }

        private void StopSocketServer()
        {
            if (_socketServerProcess is not null)
            {
                _socketServerProcess.Stop();
                AppEnvironment.StartedBackgroundProcesses.Remove(typeof(RemoteMonitoringSocketServer).FullName!);
                _socketServerProcess = null;

                FnLog.AddInfo(this, $"Stopped Socket-Server process.");

                FnPowerRequest.ReleaseSystemAwake();
                FnLog.AddInfo(this, "The PC follows the sleep settings again.");
            }
        }
    }
}
