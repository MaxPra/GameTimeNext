using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Screensetups.Viewmodels;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIXMonitorTweaks;

namespace GameTimeNext.Core.Application.Screensetups.Controller
{
    public class ScreensetupsEditViewController : UIXViewControllerBase
    {

        private ScreensetupsEditViewModel? _viewModel;

        // Audio device id -> plain device name (without availability suffix)
        private Dictionary<string, string> _audioDeviceNames = new Dictionary<string, string>();

        public ScreensetupsEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ScreensetupsEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new ScreensetupsEditViewReturn();

            AddIdentifier("T1SCRSH", GetApp().T1SCRSH!);
        }

        protected override void BuildFirstImpl()
        {
            using (SuppressRunEventPipeline())
            {
                FnControls.LoadTabContent(GetWnd().tabControl);

                BuildMonitorGrid();
                BuildAudioDevices();
            }

            GetWnd().TxbName.Focus();
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbName.Text))
                AddViewError(GetWnd().TxbName, GetErrorMessage("GLO0003", "Name"));

            if (GetWnd().TxbName.Text.Length > 200)
                AddViewError(GetWnd().TxbName, GetErrorMessage("GLO0004", "Name", "200"));

            if (GetWnd().TxbDescription.Text.Length > 200)
                AddViewError(GetWnd().TxbDescription, GetErrorMessage("GLO0004", "Description", "200"));

            List<ScreensetupMonitorDataGridRow> monitorRows = _viewModel?.MonitorDataGridRows?.ToList() ?? new List<ScreensetupMonitorDataGridRow>();

            if (!monitorRows.Any(r => r.COMOEN))
                AddViewError(GetWnd().DgMonitors, GetErrorMessage("SCS0001"));
            else if (!monitorRows.Any(r => r.COMOPR))
                AddViewError(GetWnd().DgMonitors, GetErrorMessage("GLO0003", "A primary monitor"));
            else if (monitorRows.Any(r => r.COMOPR && !r.COMOEN))
                AddViewError(GetWnd().DgMonitors, GetErrorMessage("SCS0002"));
        }

        protected override void FillViewImpl()
        {
        }

        protected override void FillDBOImpl()
        {
            string adid = (GetWnd().CmbAudioDevice.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

            GetApp().T1SCRSH!.ADID = adid;
            GetApp().T1SCRSH!.ADNA = _audioDeviceNames.TryGetValue(adid, out string? adna) ? adna : string.Empty;
        }

        protected override void SaveDBOImpl()
        {
            long ssid = GetApp().T1SCRSH!.SSID;

            if (ssid == 0 || _viewModel?.MonitorDataGridRows == null)
                return;

            // Replace all monitors of this screensetup
            TXSCRSP txscrsp = new TXSCRSP();
            txscrsp.DeleteAllWhereSSID(ssid);

            foreach (ScreensetupMonitorDataGridRow monitorRow in _viewModel.MonitorDataGridRows)
            {
                T1SCRSP t1scrsp = txscrsp.CreateNew();
                t1scrsp.SSID = ssid;
                t1scrsp.MOID = monitorRow.COMOID;
                t1scrsp.MONA = monitorRow.COMONA;
                t1scrsp.MOEN = monitorRow.COMOEN;
                t1scrsp.MOPR = monitorRow.COMOPR && monitorRow.COMOEN;
                txscrsp.Save(t1scrsp);
            }
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void Event_Closing()
        {
            GetViewReturn<ScreensetupsEditViewReturn>().Canceled = true;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private ScreensetupsEditApp GetApp()
        {
            return (ScreensetupsEditApp)App;
        }

        private ScreensetupsEditView GetWnd()
        {
            return (ScreensetupsEditView)View;
        }

        private void BuildMonitorGrid()
        {
            // A copy starts with the monitors of its source screensetup
            long ssid = GetWnd().ViewIndicator.Contains("CO") ? GetApp().CopySourceSSID : GetApp().T1SCRSH!.SSID;

            IReadOnlyList<UIXMonitorInfo> monitors = UIXMonitorManager.GetMonitors();
            List<T1SCRSP> t1scrsps = TFSCRSP.GetMonitorsOfScreensetup(ssid);

            // A new screensetup starts with the current Windows state
            bool isCreateNew = GetWnd().ViewIndicator.Contains("CN");

            List<ScreensetupMonitorDataGridRow> rows = new List<ScreensetupMonitorDataGridRow>();

            // The order of GetMonitors() is stable, so the number matches the identify overlay
            for (int i = 0; i < monitors.Count; i++)
            {
                UIXMonitorInfo monitor = monitors[i];
                T1SCRSP? t1scrsp = t1scrsps.FirstOrDefault(p => p.MOID == monitor.Id);

                ScreensetupMonitorDataGridRow row = GetWnd().DgMonitors.CreateNewRow<ScreensetupMonitorDataGridRow>();
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
                ScreensetupMonitorDataGridRow row = GetWnd().DgMonitors.CreateNewRow<ScreensetupMonitorDataGridRow>();
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

            _viewModel = new ScreensetupsEditViewModel();
            _viewModel.MonitorDataGridRows = new System.Collections.ObjectModel.ObservableCollection<ScreensetupMonitorDataGridRow>(rows);

            View.DataContext = _viewModel;
        }

        private void MonitorRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ScreensetupMonitorDataGridRow.COMOPR) || sender is not ScreensetupMonitorDataGridRow changedRow || !changedRow.COMOPR)
                return;

            foreach (ScreensetupMonitorDataGridRow row in _viewModel?.MonitorDataGridRows ?? Enumerable.Empty<ScreensetupMonitorDataGridRow>())
            {
                if (!ReferenceEquals(row, changedRow))
                    row.COMOPR = false;
            }
        }

        private void BuildAudioDevices()
        {
            T1SCRSH t1scrsh = GetApp().T1SCRSH!;

            _audioDeviceNames = new Dictionary<string, string>();

            ComboBox cmbAudioDevice = GetWnd().CmbAudioDevice;
            cmbAudioDevice.Items.Clear();
            cmbAudioDevice.Items.Add(new ComboBoxItem { Content = "(Unchanged)", Tag = string.Empty });

            // Unavailable devices (e.g. a TV that is switched off) are listed on purpose
            foreach (UIXAudioDevice audioDevice in CFScreensetupsApp.GetAudioManager().GetPlaybackDevices())
            {
                _audioDeviceNames[audioDevice.Id] = audioDevice.Name;

                cmbAudioDevice.Items.Add(new ComboBoxItem
                {
                    Content = audioDevice.IsAvailable ? audioDevice.Name : $"{audioDevice.Name} (not available)",
                    Tag = audioDevice.Id
                });
            }

            // Show the stored device even if Windows does not know it anymore, so it is not lost on save
            if (!FnString.IsNullEmptyOrWhitespace(t1scrsh.ADID) && !_audioDeviceNames.ContainsKey(t1scrsh.ADID))
            {
                _audioDeviceNames[t1scrsh.ADID] = t1scrsh.ADNA;

                cmbAudioDevice.Items.Add(new ComboBoxItem
                {
                    Content = $"{t1scrsh.ADNA} (not found)",
                    Tag = t1scrsh.ADID
                });
            }

            cmbAudioDevice.SelectedItem = cmbAudioDevice.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => (i.Tag?.ToString() ?? string.Empty) == t1scrsh.ADID)
                ?? cmbAudioDevice.Items[0];
        }

        private static string FormatMonitorState(UIXMonitorInfo monitor)
        {
            if (!monitor.IsActive)
                return "Off";

            string resolution = $"{monitor.Width} x {monitor.Height}";

            return monitor.IsPrimary ? $"On, primary ({resolution})" : $"On ({resolution})";
        }

        protected void EV_BtnUseCurrentState()
        {
            if (_viewModel?.MonitorDataGridRows == null)
                return;

            foreach (ScreensetupMonitorDataGridRow monitorRow in _viewModel.MonitorDataGridRows)
            {
                UIXMonitorInfo? monitor = monitorRow.COISCON ? UIXMonitorManager.GetMonitor(monitorRow.COMOID) : null;

                monitorRow.COMOEN = monitor?.IsActive ?? false;
                monitorRow.COMOPR = monitor?.IsPrimary ?? false;
            }
        }

        protected void EV_BtnIdentify()
        {
            IReadOnlyList<string> identifiedMonitors = UIXMonitorManager.Identify(CFScreensetupsApp.CreateIdentifyOptions());

            // Identify only works on monitors that are switched on
            if (identifiedMonitors.Count == 0)
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("SCS0003"), CFMBOXResult.Ok, CFMBOXIcon.Info);
            }
        }

        protected void EV_BtnSave()
        {
            GetViewReturn<ScreensetupsEditViewReturn>().HasChanged = true;

            Exit(true);
        }
    }
}
