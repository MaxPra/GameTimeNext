using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Playthroughs.Controller
{
    public class PlaythroughsViewController : UIXViewControllerBase<PlaythroughsApp, PlaythroughsView>
    {
        public PlaythroughsViewController(UIXApplication app) : base(app)
        {
        }

        public class PlaythroughsViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new PlaythroughsViewReturn();

            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {

            GetView().TxbProfileName.Text = GetApp().AppRunSelections.Gana;

            FnControls.SetEnabled(GetView().TxbProfileName, FnString.IsNullEmptyOrWhitespace(GetApp().AppRunSelections.Gana) && !GetView().ViewIndicator.Contains("SE"));
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().DgPlaythroughs.LoadDataAsync();
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

        protected override void Event_Closing()
        {
            Exit(true);
        }

        protected async void EV_BtnRefresh()
        {
            await GetView().DgPlaythroughs.LoadDataAsync();
        }
    }
}
