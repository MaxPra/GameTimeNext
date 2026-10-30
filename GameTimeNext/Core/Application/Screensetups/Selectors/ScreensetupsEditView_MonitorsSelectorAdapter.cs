using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Screensetups.Controller;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.ComponentModel;
using System.Globalization;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIXMonitorTweaks;

namespace GameTimeNext.Core.Application.Screensetups.Selectors
{
    public class ScreensetupsEditView_MonitorsSelectorAdapter : UIXSelectorAdapterBase<ScreensetupsEditApp, ScreensetupsEditView, UIXDataGrid, ScreensetupMonitorDataGridRow>
    {
        public ScreensetupsEditView_MonitorsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<ScreensetupMonitorDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // A copy starts with the monitors of its source screensetup
            long ssid = GetView().ViewIndicator.Contains("CO") ? GetApp().CopySourceSSID : GetApp().T1SCRSH!.SSID;

            // A new screensetup starts with the current Windows state
            bool isCreateNew = GetView().ViewIndicator.Contains("CN");

            List<T1SCRSP> t1scrsps = await Task.Run(() => TFSCRSP.GetMonitorsOfScreensetup(ssid), token);

            return BuildRows(t1scrsps, isCreateNew);
        }

        private List<ScreensetupMonitorDataGridRow> BuildRows(List<T1SCRSP> t1scrsps, bool isCreateNew)
        {
            IReadOnlyList<UIXMonitorInfo> monitors = UIXMonitorManager.GetMonitors();

            List<ScreensetupMonitorDataGridRow> rows = new List<ScreensetupMonitorDataGridRow>();

            // The order of GetMonitors() is stable, so the number matches the identify overlay
            for (int i = 0; i < monitors.Count; i++)
            {
                UIXMonitorInfo monitor = monitors[i];
                T1SCRSP? t1scrsp = t1scrsps.FirstOrDefault(p => p.MOID == monitor.Id);

                ScreensetupMonitorDataGridRow row = GetSelector().CreateNewRow<ScreensetupMonitorDataGridRow>();
                row.COMOID = monitor.Id;
                row.COMONA = monitor.Name;
                row.CONUMB = (i + 1).ToString(CultureInfo.CurrentCulture);
                row.COSTAT = FormatMonitorState(monitor);
                row.COISCON = true;
                row.COMOEN = t1scrsp?.MOEN ?? (isCreateNew && monitor.IsActive);
                row.COMOPR = t1scrsp?.MOPR ?? (isCreateNew && monitor.IsPrimary);

                rows.Add(row);
            }

            // Keep stored monitors that are not connected right now, otherwise they would be lost on save
            foreach (T1SCRSP t1scrsp in t1scrsps.Where(p => !monitors.Any(m => m.Id == p.MOID)))
            {
                ScreensetupMonitorDataGridRow row = GetSelector().CreateNewRow<ScreensetupMonitorDataGridRow>();
                row.COMOID = t1scrsp.MOID;
                row.COMONA = t1scrsp.MONA;
                row.CONUMB = "-";
                row.COSTAT = "Not connected";
                row.COISCON = false;
                row.COMOEN = t1scrsp.MOEN;
                row.COMOPR = t1scrsp.MOPR;

                rows.Add(row);
            }

            // Only one monitor can be primary
            foreach (ScreensetupMonitorDataGridRow row in rows)
                row.PropertyChanged += MonitorRow_PropertyChanged;

            return rows;
        }

        private void MonitorRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ScreensetupMonitorDataGridRow.COMOPR) || sender is not ScreensetupMonitorDataGridRow changedRow || !changedRow.COMOPR)
                return;

            foreach (ScreensetupMonitorDataGridRow row in GetItems())
            {
                if (!ReferenceEquals(row, changedRow))
                    row.COMOPR = false;
            }
        }

        private static string FormatMonitorState(UIXMonitorInfo monitor)
        {
            if (!monitor.IsActive)
                return "Off";

            string resolution = $"{monitor.Width} x {monitor.Height}";

            return monitor.IsPrimary ? $"On, primary ({resolution})" : $"On ({resolution})";
        }
    }
}
