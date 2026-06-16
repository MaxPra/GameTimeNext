using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesEditViewController : UIXWindowControllerBase
    {
        public CodetablesEditViewController(UIXApplication app) : base(app)
        {
        }

        public class CodetablesEditViewReturn : UIXViewReturn
        {
        }

        protected override void Init()
        {
            ViewReturn = new CodetablesEditViewReturn();

            AddIdentifier("T1CTABH", GetApp().T1CTABH!);
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void BuildFirst()
        {
            if (GetWnd().ViewIndicator.Contains("CN"))
                GetWnd().TxbTextType.Focus();
            else if (GetWnd().ViewIndicator.Contains("ED"))
                GetWnd().TxbDescription.Focus();
        }

        protected override void Build()
        {
            if (GetApp().T1CTABH!.State == UIXTableObjectState.Available)
                FnControls.SetEnabled(GetWnd().TxbTextType, false);
        }

        protected override void Check()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbTextType.Text))
                AddViewError(GetWnd().TxbTextType, "Text Type must not be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbDescription.Text))
                AddViewError(GetWnd().TxbDescription, "Description must not be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().CmbPermission.SelectedValue.ToString()!))
                AddViewError(GetWnd().CmbPermission, "Permission must be selected.");

            if (GetWnd().TxbDescription.Text.Length > 200)
                AddViewError(GetWnd().TxbDescription, "Description must not be longer than 200 characters.");

            if (GetWnd().TxbTextType.Text.Length > 2)
                AddViewError(GetWnd().TxbTextType, "Text Type must not be longer than 2 characters.");
        }

        protected override void FillViewImpl()
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
            GetViewReturn<CodetablesEditViewReturn>().Canceled = true;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private CodetablesEditApp GetApp()
        {
            return (CodetablesEditApp)App;
        }

        private CodetablesEditView GetWnd()
        {
            return (CodetablesEditView)View;
        }

        protected void EV_BtnSave()
        {
            if (GetApp().T1CTABH!.HasChanged())
                GetViewReturn<CodetablesEditViewReturn>().Canceled = false;

            Exit(true);
        }
    }
}
