using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.Playthroughs.Controller
{
    public class PlaythroughRatingViewController : UIXViewControllerBase<PlaythroughRatingApp, PlaythroughRatingView>
    {
        public PlaythroughRatingViewController(UIXApplication app) : base(app)
        {
        }

        public class PlaythroughRatingViewReturn : UIXViewReturn
        {
            public bool Rated { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new PlaythroughRatingViewReturn();

            AddIdentifier("T1PLTHR", GetApp().T1plthr);
        }

        protected override void BuildFirstImpl()
        {
            T1PROFI? t1profi = new TXPROFI().Read(GetApp().T1plthr.PFID);

            GetView().txGameName.Text = t1profi?.GANA ?? string.Empty;
            GetView().txPlaythrough.Text = GetApp().T1plthr.PTDE;
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
            Exit(false);
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
        }

        protected override void FillDBOImpl()
        {
            GetApp().T1plthr.PTRA = CFPlaythroughsApp.ToRatingValue(GetView().rtgRating.Value);
        }

        protected override void FillViewImpl()
        {
            GetView().rtgRating.Value = GetApp().T1plthr.PTRA;
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected void EV_btnSave()
        {
            GetViewReturn<PlaythroughRatingViewReturn>().Rated = true;

            Exit(true);
        }

        protected void EV_btnSkip()
        {
            Exit(true);
        }
    }
}
