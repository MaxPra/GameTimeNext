using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
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
                FnControls.LoadTabContent(GetView().tabControl);

                BuildAudioDevices();
            }

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
