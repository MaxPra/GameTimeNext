using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Playthroughs.Controller
{
    public class PlaythroughEditViewController : UIXViewControllerBase
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
            // Nur bei Create New wird eine Auswahl des Typs erlaubt
            FnControls.SetEnabled(GetView().cmbType, GetView().ViewIndicator.Contains("CN"));

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

        protected override void Check()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetView().txbDescription.Text))
                AddViewError(GetView().txbDescription, "Invalid input: description has to be specified.");

            if (FnString.IsNullEmptyOrWhitespace(GetView().cmbType.SelectedValue?.ToString()!))
                AddViewError(GetView().cmbType, "Invalid input: type has to be specified.");
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

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {

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
                    else
                    {
                        GetView().txbDescription.Text = string.Empty;
                    }
                }
            }
        }

        private PlaythroughEditView GetView()
        {
            return (PlaythroughEditView)View;
        }

        private PlaythroughEditApp GetApp()
        {
            return (PlaythroughEditApp)App;
        }

        protected void EV_btnSave()
        {
            GetViewReturn<PlaythroughEditViewReturn>().HasChanged = GetApp().T1plthr!.HasChanged();

            Exit(true);
        }
    }
}
