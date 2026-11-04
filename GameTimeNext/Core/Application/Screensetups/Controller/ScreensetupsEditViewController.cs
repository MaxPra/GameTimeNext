using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.ComboBoxes;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIXMonitorTweaks;

namespace GameTimeNext.Core.Application.Screensetups.Controller
{
    public class ScreensetupsEditViewController : UIXViewControllerBase<ScreensetupsEditApp, ScreensetupsEditView>
    {

        // Audio device id -> plain device name (without availability suffix)
        private Dictionary<string, string> _audioDeviceNames = new Dictionary<string, string>();

        // Keys of the display mode ComboBoxes -> values of the selected monitor
        private Dictionary<string, (long Width, long Height)> _resolutions = new Dictionary<string, (long Width, long Height)>();
        private Dictionary<string, long> _refreshRates = new Dictionary<string, long>();
        private Dictionary<string, long> _scales = new Dictionary<string, long>();

        // SuppressRunEventPipeline doesn't stop the EV_ handlers: refilling the mode ComboBoxes would reset the row to "Automatic"
        private bool _isFillingDisplayMode = false;

        public ScreensetupsEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ScreensetupsEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Detecting your monitors...",
                                            "Identifying connected displays...",
                                            "Checking which monitors are switched on...",
                                            "Finding your audio devices...",
                                            "Lining up your screens..."
                                            );

            ViewReturn = new ScreensetupsEditViewReturn();

            AddIdentifier("T1SCRSH", GetApp().T1SCRSH!);
        }

        protected override void BuildFirstImpl()
        {
            using (SuppressRunEventPipeline())
            {
                FnControls.LoadTabContent(GetView().tabControl);

                BuildAudioDevices();
            }

            // Fixed entries: filled once, only the selection follows the selected monitor
            FillDisplayMode(() =>
            {
                FillCodetable(GetView().cmbOrientation, "mO");
                FillCodetable(GetView().cmbPlacement, "mP");
            });

            // No monitor is selected yet
            ShowDisplayMode(null);

            GetView().TxbName.Focus();
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().DgMonitors.LoadDataAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetView().TxbName.Text))
                AddViewError(GetView().TxbName, GetErrorMessage("GLO0003", "Name"));

            if (GetView().TxbName.Text.Length > 200)
                AddViewError(GetView().TxbName, GetErrorMessage("GLO0004", "Name", "200"));

            if (GetView().TxbDescription.Text.Length > 200)
                AddViewError(GetView().TxbDescription, GetErrorMessage("GLO0004", "Description", "200"));

            List<ScreensetupMonitorDataGridRow> monitorRows = GetMonitorRows();

            if (!monitorRows.Any(r => r.COMOEN))
                AddViewError(GetView().DgMonitors, GetErrorMessage("SCS0001"));
            else if (!monitorRows.Any(r => r.COMOPR))
                AddViewError(GetView().DgMonitors, GetErrorMessage("GLO0003", "A primary monitor"));
            else if (monitorRows.Any(r => r.COMOPR && !r.COMOEN))
                AddViewError(GetView().DgMonitors, GetErrorMessage("SCS0002"));
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
        }

        protected override void FillDBOImpl()
        {
            string adid = (GetView().CmbAudioDevice.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;

            GetApp().T1SCRSH!.ADID = adid;
            GetApp().T1SCRSH!.ADNA = _audioDeviceNames.TryGetValue(adid, out string? adna) ? adna : string.Empty;
        }

        protected override void SaveDBOImpl()
        {
            long ssid = GetApp().T1SCRSH!.SSID;

            if (ssid == 0)
                return;

            // Replace all monitors of this screensetup
            TXSCRSP txscrsp = new TXSCRSP();
            txscrsp.DeleteAllWhereSSID(ssid);

            foreach (ScreensetupMonitorDataGridRow monitorRow in GetMonitorRows())
            {
                T1SCRSP t1scrsp = txscrsp.CreateNew();
                t1scrsp.SSID = ssid;
                t1scrsp.MOID = monitorRow.COMOID;
                t1scrsp.MONA = monitorRow.COMONA;
                t1scrsp.MOEN = monitorRow.COMOEN;
                t1scrsp.MOPR = monitorRow.COMOPR && monitorRow.COMOEN;
                t1scrsp.MOWI = monitorRow.ModeWidth;
                t1scrsp.MOHE = monitorRow.ModeHeight;
                t1scrsp.MORF = monitorRow.ModeRefreshRate;
                t1scrsp.MOOR = monitorRow.ModeOrientation;
                t1scrsp.MOPL = monitorRow.ModePlacement;
                t1scrsp.MOSC = monitorRow.ModeScale;
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

        private List<ScreensetupMonitorDataGridRow> GetMonitorRows()
        {
            return GetView().DgMonitors.Items.OfType<ScreensetupMonitorDataGridRow>().ToList();
        }

        /// <summary>
        /// Shows the display mode of the monitor selected in the list in the mode ComboBoxes (disabled without a selection).
        /// </summary>
        public void ShowDisplayMode(ScreensetupMonitorDataGridRow? monitorRow)
        {
            FillDisplayMode(() =>
            {
                BuildResolutions(monitorRow);
                BuildRefreshRates(monitorRow);
                SelectEntry(GetView().cmbOrientation, monitorRow?.ModeOrientation ?? string.Empty);
                SelectEntry(GetView().cmbPlacement, monitorRow?.ModePlacement ?? string.Empty);
                BuildScales(monitorRow);
            });

            FnControls.SetEnabled(GetView().cmbResolution, monitorRow != null);
            FnControls.SetEnabled(GetView().cmbRefreshRate, monitorRow != null);
            FnControls.SetEnabled(GetView().cmbOrientation, monitorRow != null);
            FnControls.SetEnabled(GetView().cmbPlacement, monitorRow != null);
            FnControls.SetEnabled(GetView().cmbScaling, monitorRow != null);
        }

        private void BuildScales(ScreensetupMonitorDataGridRow? monitorRow)
        {
            _scales = new Dictionary<string, long>();

            UIXManualCodetable codetable = new UIXManualCodetable();

            if (monitorRow != null)
            {
                foreach (int scale in monitorRow.SupportedScales)
                {
                    string key = GetScaleKey(scale);
                    _scales[key] = scale;
                    codetable.AddEntry(key, CFScreensetupsApp.FormatScale(scale));
                }

                // Keep the stored scaling even if it can't be checked right now, so it is not lost on save
                string storedKey = GetScaleKey(monitorRow.ModeScale);
                if (monitorRow.ModeScale > 0 && !_scales.ContainsKey(storedKey))
                {
                    string suffix = monitorRow.SupportedScales.Count == 0 ? "(monitor off)" : "(not supported)";
                    _scales[storedKey] = monitorRow.ModeScale;
                    codetable.AddEntry(storedKey, $"{CFScreensetupsApp.FormatScale(monitorRow.ModeScale)} {suffix}");
                }
            }

            codetable.ApplyTo(GetView().cmbScaling);

            SelectEntry(GetView().cmbScaling, monitorRow != null && monitorRow.ModeScale > 0 ? GetScaleKey(monitorRow.ModeScale) : string.Empty);
        }

        private static string GetScaleKey(long scalePercent)
        {
            return scalePercent.ToString(CultureInfo.InvariantCulture);
        }

        private static void FillCodetable(UIXComboBox comboBox, string txtyp)
        {
            UIXManualCodetable codetable = new UIXManualCodetable();

            foreach (T1CTABD entry in new TXCTABD().GetEntries(txtyp))
                codetable.AddEntry(entry.TXNUM, entry.DESCR);

            codetable.ApplyTo(comboBox);
        }

        private void FillDisplayMode(Action fill)
        {
            _isFillingDisplayMode = true;

            try
            {
                using (SuppressRunEventPipeline())
                    fill();
            }
            finally
            {
                _isFillingDisplayMode = false;
            }
        }

        private void BuildResolutions(ScreensetupMonitorDataGridRow? monitorRow)
        {
            _resolutions = new Dictionary<string, (long Width, long Height)>();

            UIXManualCodetable codetable = new UIXManualCodetable();

            if (monitorRow != null)
            {
                foreach (UIXDisplayMode mode in monitorRow.SupportedModes.DistinctBy(m => (m.Width, m.Height)))
                {
                    string key = GetResolutionKey(mode.Width, mode.Height);
                    _resolutions[key] = (mode.Width, mode.Height);
                    codetable.AddEntry(key, CFScreensetupsApp.FormatResolution(mode.Width, mode.Height));
                }

                // Keep the stored resolution even if it can't be checked right now, so it is not lost on save
                string storedKey = GetResolutionKey(monitorRow.ModeWidth, monitorRow.ModeHeight);
                if (monitorRow.ModeWidth > 0 && monitorRow.ModeHeight > 0 && !_resolutions.ContainsKey(storedKey))
                {
                    _resolutions[storedKey] = (monitorRow.ModeWidth, monitorRow.ModeHeight);
                    codetable.AddEntry(storedKey, $"{CFScreensetupsApp.FormatResolution(monitorRow.ModeWidth, monitorRow.ModeHeight)} {GetUnverifiedSuffix(monitorRow)}");
                }
            }

            codetable.ApplyTo(GetView().cmbResolution);

            SelectEntry(GetView().cmbResolution, monitorRow != null && monitorRow.ModeWidth > 0 ? GetResolutionKey(monitorRow.ModeWidth, monitorRow.ModeHeight) : string.Empty);
        }

        private void BuildRefreshRates(ScreensetupMonitorDataGridRow? monitorRow)
        {
            _refreshRates = new Dictionary<string, long>();

            UIXManualCodetable codetable = new UIXManualCodetable();

            if (monitorRow != null)
            {
                // Without a resolution of its own the refresh rate applies to the one Windows chooses, usually the current one
                (long width, long height) = GetEffectiveResolution(monitorRow);

                foreach (UIXDisplayMode mode in monitorRow.SupportedModes.Where(m => m.Width == width && m.Height == height))
                {
                    string key = GetRefreshRateKey(mode.RefreshRate);
                    _refreshRates[key] = mode.RefreshRate;
                    codetable.AddEntry(key, CFScreensetupsApp.FormatRefreshRate(mode.RefreshRate));
                }

                string storedKey = GetRefreshRateKey(monitorRow.ModeRefreshRate);
                if (monitorRow.ModeRefreshRate > 0 && !_refreshRates.ContainsKey(storedKey))
                {
                    _refreshRates[storedKey] = monitorRow.ModeRefreshRate;
                    codetable.AddEntry(storedKey, $"{CFScreensetupsApp.FormatRefreshRate(monitorRow.ModeRefreshRate)} {GetUnverifiedSuffix(monitorRow)}");
                }
            }

            codetable.ApplyTo(GetView().cmbRefreshRate);

            SelectEntry(GetView().cmbRefreshRate, monitorRow != null && monitorRow.ModeRefreshRate > 0 ? GetRefreshRateKey(monitorRow.ModeRefreshRate) : string.Empty);
        }

        private static (long Width, long Height) GetEffectiveResolution(ScreensetupMonitorDataGridRow monitorRow)
        {
            return monitorRow.ModeWidth > 0 && monitorRow.ModeHeight > 0
                ? (monitorRow.ModeWidth, monitorRow.ModeHeight)
                : (monitorRow.CurrentWidth, monitorRow.CurrentHeight);
        }

        private static string GetUnverifiedSuffix(ScreensetupMonitorDataGridRow monitorRow)
        {
            // Modes are only known while the monitor is on
            return monitorRow.SupportedModes.Count == 0 ? "(monitor off)" : "(not supported)";
        }

        private static string GetResolutionKey(long width, long height)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{width}x{height}");
        }

        private static string GetRefreshRateKey(long refreshRate)
        {
            return refreshRate.ToString(CultureInfo.InvariantCulture);
        }

        private static void SelectEntry(UIXComboBox comboBox, string key)
        {
            comboBox.SelectedItem = comboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => (i.Tag?.ToString() ?? string.Empty) == key)
                ?? comboBox.Items[0];
        }

        private static string GetSelectedKey(UIXComboBox comboBox)
        {
            return (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
        }

        private ScreensetupMonitorDataGridRow? GetSelectedMonitorRow()
        {
            return GetView().DgMonitors.SelectedItem as ScreensetupMonitorDataGridRow;
        }

        private static void UpdateModeCell(ScreensetupMonitorDataGridRow monitorRow)
        {
            monitorRow.COMODE = CFScreensetupsApp.FormatMode(monitorRow.ModeWidth, monitorRow.ModeHeight, monitorRow.ModeRefreshRate, monitorRow.ModeOrientation, monitorRow.ModeScale);
        }

        private void BuildAudioDevices()
        {
            T1SCRSH t1scrsh = GetApp().T1SCRSH!;

            _audioDeviceNames = new Dictionary<string, string>();

            UIXComboBox cmbAudioDevice = GetView().CmbAudioDevice;

            UIXManualCodetable codetable = new UIXManualCodetable();

            // Unavailable devices (e.g. a TV that is switched off) are listed on purpose
            foreach (UIXAudioDevice audioDevice in CFScreensetupsApp.GetAudioManager().GetPlaybackDevices())
            {
                _audioDeviceNames[audioDevice.Id] = audioDevice.Name;

                codetable.AddEntry(audioDevice.Id, audioDevice.IsAvailable ? audioDevice.Name : $"{audioDevice.Name} (not available)");
            }

            // Show the stored device even if Windows does not know it anymore, so it is not lost on save
            if (!FnString.IsNullEmptyOrWhitespace(t1scrsh.ADID) && !_audioDeviceNames.ContainsKey(t1scrsh.ADID))
            {
                _audioDeviceNames[t1scrsh.ADID] = t1scrsh.ADNA;

                codetable.AddEntry(t1scrsh.ADID, $"{t1scrsh.ADNA} (not found)");
            }

            codetable.ApplyTo(cmbAudioDevice);

            cmbAudioDevice.SelectedItem = cmbAudioDevice.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => (i.Tag?.ToString() ?? string.Empty) == t1scrsh.ADID)
                ?? cmbAudioDevice.Items[0];
        }

        protected void EV_BtnUseCurrentState()
        {
            foreach (ScreensetupMonitorDataGridRow monitorRow in GetMonitorRows())
            {
                UIXMonitorInfo? monitor = monitorRow.COISCON ? UIXMonitorManager.GetMonitor(monitorRow.COMOID) : null;

                monitorRow.COMOEN = monitor?.IsActive ?? false;
                monitorRow.COMOPR = monitor?.IsPrimary ?? false;

                // Monitors that are off keep "Automatic": Windows doesn't report a mode for them
                bool isActive = monitor?.IsActive ?? false;
                monitorRow.ModeWidth = isActive ? monitor!.Mode?.Width ?? 0 : 0;
                monitorRow.ModeHeight = isActive ? monitor!.Mode?.Height ?? 0 : 0;
                monitorRow.ModeRefreshRate = isActive ? monitor!.RefreshRate : 0;
                monitorRow.ModeOrientation = isActive ? CFScreensetupsApp.GetOrientationKey(monitor!.Orientation) : string.Empty;
                monitorRow.ModePlacement = CFScreensetupsApp.GetPlacementKey(monitor?.Placement);
                monitorRow.ModeScale = isActive ? monitor!.ScalePercent : 0;
                UpdateModeCell(monitorRow);
                monitorRow.COPLAC = CFScreensetupsApp.FormatPlacement(monitorRow.ModePlacement);
            }

            ShowDisplayMode(GetSelectedMonitorRow());
        }

        protected void EV_cmbResolution()
        {
            ScreensetupMonitorDataGridRow? monitorRow = GetSelectedMonitorRow();
            if (monitorRow == null || _isFillingDisplayMode)
                return;

            (long width, long height) = _resolutions.TryGetValue(GetSelectedKey(GetView().cmbResolution), out (long Width, long Height) resolution) ? resolution : (0, 0);

            monitorRow.ModeWidth = width;
            monitorRow.ModeHeight = height;

            // A refresh rate the new resolution doesn't offer goes back to "Automatic"
            (long effectiveWidth, long effectiveHeight) = GetEffectiveResolution(monitorRow);
            if (monitorRow.ModeRefreshRate > 0 && monitorRow.SupportedModes.Count > 0
                && !monitorRow.SupportedModes.Any(m => m.Width == effectiveWidth && m.Height == effectiveHeight && m.RefreshRate == monitorRow.ModeRefreshRate))
                monitorRow.ModeRefreshRate = 0;

            FillDisplayMode(() => BuildRefreshRates(monitorRow));

            UpdateModeCell(monitorRow);
        }

        protected void EV_cmbOrientation()
        {
            ScreensetupMonitorDataGridRow? monitorRow = GetSelectedMonitorRow();
            if (monitorRow == null || _isFillingDisplayMode)
                return;

            // Modes are listed unrotated, so the resolution and refresh rate stay valid
            monitorRow.ModeOrientation = GetSelectedKey(GetView().cmbOrientation);

            UpdateModeCell(monitorRow);
        }

        protected void EV_cmbScaling()
        {
            ScreensetupMonitorDataGridRow? monitorRow = GetSelectedMonitorRow();
            if (monitorRow == null || _isFillingDisplayMode)
                return;

            monitorRow.ModeScale = _scales.TryGetValue(GetSelectedKey(GetView().cmbScaling), out long scale) ? scale : 0;

            UpdateModeCell(monitorRow);
        }

        protected void EV_cmbPlacement()
        {
            ScreensetupMonitorDataGridRow? monitorRow = GetSelectedMonitorRow();
            if (monitorRow == null || _isFillingDisplayMode)
                return;

            monitorRow.ModePlacement = GetSelectedKey(GetView().cmbPlacement);
            monitorRow.COPLAC = CFScreensetupsApp.FormatPlacement(monitorRow.ModePlacement);
        }

        protected void EV_cmbRefreshRate()
        {
            ScreensetupMonitorDataGridRow? monitorRow = GetSelectedMonitorRow();
            if (monitorRow == null || _isFillingDisplayMode)
                return;

            monitorRow.ModeRefreshRate = _refreshRates.TryGetValue(GetSelectedKey(GetView().cmbRefreshRate), out long refreshRate) ? refreshRate : 0;

            UpdateModeCell(monitorRow);
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
