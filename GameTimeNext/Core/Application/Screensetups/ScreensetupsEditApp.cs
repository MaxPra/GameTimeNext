using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Screensetups.Controller;
using GameTimeNext.Core.Application.Screensetups.Selectors;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Screensetups
{
    public class ScreensetupsEditApp : UIXApplication
    {

        public ScreensetupsEditView? ScreensetupsEditView { get; set; }
        public ScreensetupsEditViewController? ScreensetupsEditViewController { get; set; }
        public T1SCRSH? T1SCRSH { get; set; } = new T1SCRSH();

        // Screensetup whose monitors are taken over when copying (ViewIndicator "CO")
        public long CopySourceSSID { get; set; } = 0;

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ScreensetupsEditApp));
        }

        public override void InitializeApplicationOutput()
        {
            ScreensetupsEditView = new ScreensetupsEditView();
            MainView = ScreensetupsEditView;

            ScreensetupsEditViewController = new ScreensetupsEditViewController(this);
            ScreensetupsEditView.ViewController = ScreensetupsEditViewController;
            ScreensetupsEditView.DgMonitors.SelectorAdapter = new ScreensetupsEditView_MonitorsSelectorAdapter(this);
            ScreensetupsEditView.DgMonitors.SelectorController = new ScreensetupsEditView_MonitorsSelectorController(this);
        }

        public void CreateNew(Action<ScreensetupsEditViewController.ScreensetupsEditViewReturn> callback)
        {
            T1SCRSH = new TXSCRSH().CreateNew();
            ScreensetupsEditView!.ViewIndicator.Clear();
            ScreensetupsEditView!.ViewIndicator.Add("CN");
            ScreensetupsEditView!.Title = "Create New Screensetup";
            ScreensetupsEditViewController!.SetResultCallback(callback);

            ScreensetupsEditViewController.Show();
        }

        public void Copy(Action<ScreensetupsEditViewController.ScreensetupsEditViewReturn> callback, T1SCRSH t1scrshSource)
        {
            T1SCRSH = new TXSCRSH().Copy(t1scrshSource);
            CopySourceSSID = t1scrshSource.SSID;

            ScreensetupsEditView!.ViewIndicator.Clear();
            ScreensetupsEditView!.ViewIndicator.Add("CO");
            ScreensetupsEditView!.Title = "Copy Screensetup";
            ScreensetupsEditViewController!.SetResultCallback(callback);

            ScreensetupsEditViewController.Show();
        }

        public void Edit(Action<ScreensetupsEditViewController.ScreensetupsEditViewReturn> callback, T1SCRSH t1scrsh)
        {
            T1SCRSH = t1scrsh;
            ScreensetupsEditView!.ViewIndicator.Clear();
            ScreensetupsEditView!.ViewIndicator.Add("ED");
            ScreensetupsEditView!.Title = "Edit Screensetup";
            ScreensetupsEditViewController!.SetResultCallback(callback);

            ScreensetupsEditViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
