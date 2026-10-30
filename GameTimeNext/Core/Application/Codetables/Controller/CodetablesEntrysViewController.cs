using GameTimeNext.Core.Application.Codetables.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesEntrysViewController : UIXViewControllerBase<CodetablesEntrysApp, CodetablesEntrysView>
    {

        public CodetablesEntrysViewController(UIXApplication app) : base(app)
        {
        }

        public class CodetablesEntrysViewReturn : UIXViewReturn
        {
        }

        protected override void Init()
        {
            ViewReturn = new CodetablesEntrysViewReturn();

            AddIdentifier("T1CTABH", GetApp().T1CTABH!);
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            FnControls.SetVisible(GetView().BtnAdd, GetView().ViewIndicator.Contains("ED"));
            await GetView().DgCodetableEntrys.LoadDataAsync();
        }

        protected override void BuildImpl()
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

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void Event_Closing()
        {
            GetViewReturn<CodetablesEntrysViewReturn>().Canceled = true;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected void EV_BtnAdd()
        {
            CodetablesEntryEditApp app = GetApp().GetApplication<CodetablesEntryEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            app.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetView().DgCodetableEntrys.LoadDataAsync();
            }, GetApp().T1CTABH!.TXTYP);

        }


        protected async Task EV_BtnRefresh()
        {
            await GetView().DgCodetableEntrys.LoadDataAsync();
        }
    }
}
