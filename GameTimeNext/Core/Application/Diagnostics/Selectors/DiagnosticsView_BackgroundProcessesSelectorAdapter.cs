using GameTimeNext.Core.Application.Diagnostics.Controller;
using GameTimeNext.Core.Application.Diagnostics.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Diagnostics.Selectors
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

            return await Task.Run(() => processes.Select(BuildRow).ToList(), token);
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
    }
}
