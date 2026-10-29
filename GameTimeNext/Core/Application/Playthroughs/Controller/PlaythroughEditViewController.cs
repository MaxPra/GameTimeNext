using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.Playthroughs.Controller
{
    public class PlaythroughEditViewController : UIXViewControllerBase<PlaythroughEditApp, PlaythroughEditView>
    {
        public PlaythroughEditViewController(UIXApplication app) : base(app)
        {
        }

        public class PlaythroughEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new PlaythroughEditViewReturn();

            AddSource("T1CTABD", new TXCTABD());
            AddIdentifier("T1PLTHR", GetApp().T1plthr);
        }

        protected override void BuildFirstImpl()
        {
            // Selecting the type is only allowed in create-new mode
            FnControls.SetEnabled(GetView().cmbType, GetView().ViewIndicator.Contains("CN"));

            // A playthrough that has not been played yet can't be rated
            bool isRatingVisible = !GetView().ViewIndicator.Contains("CN");
            FnControls.SetVisible(GetView().lblRating, isRatingVisible);
            FnControls.SetVisible(GetView().rtgRating, isRatingVisible);
            FnControls.SetVisible(GetView().grpRatingComment, isRatingVisible);

            UIXManualCodetable mCt = new UIXManualCodetable();

            List<T1CTABD> t1ctabds = new TXCTABD().GetEntries("pL");

            foreach (T1CTABD t1ctabd in t1ctabds)
            {
                if (t1ctabd.TXNUM != "IN" || GetView().ViewIndicator.Contains("ED"))
                    mCt.AddEntry(t1ctabd.TXNUM, t1ctabd.DESCR);
            }

            mCt.AddAdditionalEntry("(None)");

            mCt.ApplyTo(GetView().cmbType);
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetView().txbDescription.Text))
                AddViewError(GetView().txbDescription, GetErrorMessage("GLO0003", "Description"));

            if (FnString.IsNullEmptyOrWhitespace(GetView().cmbType.SelectedValue?.ToString()!))
                AddViewError(GetView().cmbType, GetErrorMessage("GLO0003", "Type"));
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
            if (source is ComboBox cmb && eventName == UIXEventNames.Selector.SelectionChanged)
            {
                if (cmb.Name == GetView().cmbType.Name)
                {
                    string value = GetView().cmbType.SelectedValue?.ToString()!;

                    if (value is null || FnString.IsNullEmptyOrWhitespace(value))
                        return;

                    if (FnString.IsNullEmptyOrWhitespace(GetView().txbDescription.Text) && value == PlaythroughType.NEW_PLAYTHROUGH)
                    {
                        GetView().txbDescription.Text = "Playthrough #" + (TFPLTHR.GetCurrentPlaythroughCount(GetApp().T1profi.PFID, PlaythroughType.NEW_PLAYTHROUGH) + 1);
                    }
                    else if (value == PlaythroughType.DLC && GetView().txbDescription.Text.StartsWith("Playthrough #"))
                    {
                        GetView().txbDescription.Text = string.Empty;
                    }
                }
            }
        }

        protected void EV_btnSave()
        {
            GetViewReturn<PlaythroughEditViewReturn>().HasChanged = GetApp().T1plthr!.HasChanged();

            Exit(true);
        }
    }
}
