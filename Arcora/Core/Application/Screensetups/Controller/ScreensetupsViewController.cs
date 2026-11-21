using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIXMonitorTweaks;

namespace GameTimeNext.Core.Application.Screensetups.Controller
{
    public class ScreensetupsViewController : UIXViewControllerBase<ScreensetupsApp, ScreensetupsView>
    {
        public ScreensetupsViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Loading screen setups...",
                                            "Counting your monitors...",
                                            "Checking which displays are connected...",
                                            "Arranging your screens...",
                                            "Looking for the primary display...",
                                            "Tuning your audio devices..."
                                            );
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().DgScreensetups.LoadDataAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override async Task BuildImplAsync()
        {

        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {

        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }
        #endregion

        protected void EV_BtnAddScreensetup()
        {
            ScreensetupsEditApp screensetupsEditApp = GetApp().GetApplication<ScreensetupsEditApp>(UIXApplicationStartTarget.Window);
            screensetupsEditApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetView().DgScreensetups.LoadDataAsync();
            });
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

        protected async Task EV_BtnRefresh()
        {
            await GetView().DgScreensetups.LoadDataAsync();
        }
    }
}
