using Arcora.Core.Application.Diagnostics.Controller;
using Arcora.Core.Application.Diagnostics.Views;
using Arcora.Core.Application.General.BackgroundProcesses;
using Arcora.Core.Framework;
using Arcora.Core.Framework.Utils;
using System.Globalization;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Application.Diagnostics.Selectors
{
    public class DiagnosticsView_BackgroundProcessesSelectorAdapter : UIXSelectorAdapterBase<DiagnosticsApp, DiagnosticsView, UIXDataGrid, BackgroundProcessesDataGridRow>
    {
        public DiagnosticsView_BackgroundProcessesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<BackgroundProcessesDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // The list of started processes belongs to the UI thread: copied here, the rows are built in the background
            // (the statistics of the processes are thread safe)
            List<UIXBackgroundProcess> processes = AppEnvironment.StartedBackgroundProcesses.Values.OrderBy(p => p.GetType().Name).ToList();

            return await Task.Run(() => BuildRows(processes), token);
        }

        private List<BackgroundProcessesDataGridRow> BuildRows(List<UIXBackgroundProcess> processes)
        {
            List<BackgroundProcessesDataGridRow> rows = new();

            foreach (UIXBackgroundProcess process in processes)
            {
                rows.Add(BuildRow(process));

                // The web app is an own Windows process, not a background process: shown below the process that runs it
                if (process is RemoteMonitoringProcess remoteMonitoringProcess)
                    rows.Add(BuildBlazorRow(remoteMonitoringProcess.BlazorStatus));
            }

            return rows;
        }

        private BackgroundProcessesDataGridRow BuildRow(UIXBackgroundProcess process)
        {
            bool isRunning = process.IsRunning();
            DateTime? startedAt = process.StartedAt;
            DateTime? endOfRun = isRunning ? DateTime.Now : process.StoppedAt;

            BackgroundProcessesDataGridRow row = GetSelector().CreateNewRow<BackgroundProcessesDataGridRow>();
            row.COPROC = process.GetType().Name;
            row.CODESC = FnString.IsNullEmptyOrWhitespace(process.AppProperties.ApplicationDescription) ? process.ProcessName : process.AppProperties.ApplicationDescription;

            // Status Cell 
            row.COSTAT = isRunning ? "Running" : "Stopped";
            row.COSTAT.Foreground = isRunning ? FnColor.Success() : FnColor.Danger();

            row.COSTRT = CFDiagnosticsApp.FormatTimestamp(startedAt);
            row.CORUNF = startedAt != null && endOfRun != null ? CFDiagnosticsApp.FormatUptime(endOfRun.Value - startedAt.Value) : string.Empty;
            row.COINTV = string.Format(CultureInfo.CurrentCulture, "{0} ms", process.Interval);
            row.COMODE = process.RunsAsync ? "Async" : "Sync";
            row.COCYCL = process.CycleCount.ToString("N0", CultureInfo.CurrentCulture);

            row.COLAST = CFDiagnosticsApp.FormatTimestamp(process.LastCycleAt);
            row.COLDUR = CFDiagnosticsApp.FormatDuration(process.LastCycleDuration);
            row.COAVGD = CFDiagnosticsApp.FormatDuration(process.AverageCycleDuration);
            row.COMAXD = CFDiagnosticsApp.FormatDuration(process.MaxCycleDuration);

            row.COTHRD = process.ManagedThreadId?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            row.RowObject = process;

            return row;
        }

        /// <summary>
        /// Row of the Blazor process of the remote monitoring. Only start and run time; the cycle columns don't apply.
        /// </summary>
        private BackgroundProcessesDataGridRow BuildBlazorRow(BlazorProcessStatus status)
        {
            bool isRunning = status.State == BlazorProcessState.Running;
            DateTime? endOfRun = isRunning ? DateTime.Now : status.EndedAt;

            BackgroundProcessesDataGridRow row = GetSelector().CreateNewRow<BackgroundProcessesDataGridRow>();
            row.COPROC = "RemoteMonitoringWebApp";
            row.CODESC = BuildBlazorDescription(status);

            row.COSTAT = status.State switch
            {
                BlazorProcessState.Running => "Running",
                BlazorProcessState.StartedSeparately => "Started separately (debug)",
                BlazorProcessState.Exited => string.Format(CultureInfo.CurrentCulture, "Exited (code {0} / 0x{0:X8})", status.ExitCode),
                BlazorProcessState.Stopped => "Stopped",
                _ => "Not started"
            };

            if (isRunning)
                row.COSTAT.Foreground = FnColor.Success();
            else if (status.State is BlazorProcessState.Exited or BlazorProcessState.Stopped)
                row.COSTAT.Foreground = FnColor.Danger();

            row.COSTRT = CFDiagnosticsApp.FormatTimestamp(status.StartedAt);
            row.CORUNF = status.StartedAt != null && endOfRun != null ? CFDiagnosticsApp.FormatUptime(endOfRun.Value - status.StartedAt.Value) : string.Empty;
            row.RowObject = status;

            return row;
        }

        private static string BuildBlazorDescription(BlazorProcessStatus status)
        {
            List<string> parts = ["Blazor web app of the remote monitoring"];

            if (status.Port != null)
                parts.Add(string.Format(CultureInfo.CurrentCulture, "port {0}", status.Port));

            if (status.ProcessId != null)
                parts.Add(string.Format(CultureInfo.CurrentCulture, "PID {0}", status.ProcessId));

            if (status.RestartCount > 0)
                parts.Add(string.Format(CultureInfo.CurrentCulture, "{0} restarts", status.RestartCount));

            return string.Join(" · ", parts);
        }
    }
}
