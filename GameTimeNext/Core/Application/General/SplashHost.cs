using GameTimeNext.Core.Framework.Config;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General
{
    /// <summary>
    /// Shows the <see cref="SplashScreen"/> in its own process (this executable started with <see cref="Argument"/>).
    /// Starting the application builds the main window on the UI thread and loads a lot of data; in the same process
    /// that stalls the splash animation (shared render thread, garbage collection), in its own process it keeps running.
    /// The application side is <see cref="Start"/> / <see cref="Close"/>, the splash process runs <see cref="RunAsSplashProcess"/>.
    /// A splash that fails to open never stops the start.
    /// </summary>
    public sealed class SplashHost
    {
        /// <summary>First start argument of the splash process; followed by the process ID of the application and 1 for development.</summary>
        public const string Argument = "--splash-process";

        private EventWaitHandle? closeEvent;

        /// <summary>True if the executable was started as splash process.</summary>
        public static bool IsSplashProcess(string[] args)
            => args.Length >= 3 && args[0] == Argument;

        private static string GetCloseEventName(int applicationProcessId)
            => $"Local\\{AppConfig.Root.InternalApplicationName}_Splash_{applicationProcessId}";

        /// <summary>Starts the splash process; does not wait for it.</summary>
        public static SplashHost Start(bool isDevelopment)
        {
            SplashHost host = new SplashHost();

            try
            {
                string? executable = Environment.ProcessPath;
                if (executable is null)
                    return host;

                // Created here, so the splash process finds it and a close request before it is up is not lost
                host.closeEvent = new EventWaitHandle(false, EventResetMode.ManualReset, GetCloseEventName(Environment.ProcessId));

                Process.Start(new ProcessStartInfo(executable, $"{Argument} {Environment.ProcessId} {(isDevelopment ? 1 : 0)}")
                {
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", $"Splash screen could not be started: {ex.Message}");
            }

            return host;
        }

        /// <summary>Closes the splash.</summary>
        public void Close()
        {
            try
            {
                closeEvent?.Set();
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", $"Splash screen could not be closed: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs in the splash process instead of the application: shows the splash until the application closes it
        /// (or ends, or after a safety timeout) and shuts the process down.
        /// </summary>
        public static void RunAsSplashProcess(System.Windows.Application app, string[] args)
        {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (!int.TryParse(args[1], out int applicationProcessId))
            {
                app.Shutdown();
                return;
            }

            EventWaitHandle close;
            try
            {
                close = new EventWaitHandle(false, EventResetMode.ManualReset, GetCloseEventName(applicationProcessId));
            }
            catch
            {
                app.Shutdown();
                return;
            }

            // The application is already up (fast start): nothing to show
            if (close.WaitOne(0))
            {
                app.Shutdown();
                return;
            }

            SplashScreen window = new SplashScreen(args[2] == "1") { Title = AppConfig.Root.ApplicationName };
            window.Show();

            // The application brings its main window to the front as soon as it is shown; it may only do that with our permission
            AllowSetForegroundWindow(applicationProcessId);

            Process? applicationProcess = null;
            try
            {
                applicationProcess = Process.GetProcessById(applicationProcessId);
            }
            catch (ArgumentException)
            {
                // Application already gone
            }

            _ = Task.Run(async () =>
            {
                Task closeRequested = Task.Run(() => close.WaitOne());
                Task applicationEnded = applicationProcess?.WaitForExitAsync() ?? Task.CompletedTask;

                await Task.WhenAny(closeRequested, applicationEnded, Task.Delay(TimeSpan.FromSeconds(90)));

                await app.Dispatcher.InvokeAsync(() =>
                {
                    window.Close();
                    app.Shutdown();
                });
            });
        }

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int processId);
    }
}
