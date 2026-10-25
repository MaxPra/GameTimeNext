using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ApplicationProperties.Controller
{
    public class ApplicationPropertiesEditViewController : UIXViewControllerBase
    {
        public ApplicationPropertiesEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ApplicationPropertiesEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new ApplicationPropertiesEditViewReturn();

            AddIdentifier("T1APRDH", GetApp().T1APRDH!);
        }

        protected override void BuildFirstImpl()
        {
            bool isNew = GetWnd().ViewIndicator.Contains("CN") || GetWnd().ViewIndicator.Contains("CO");

            if (isNew)
                GetWnd().TxbId.Focus();
            else if (GetWnd().ViewIndicator.Contains("ED"))
                GetWnd().TxbDescription.Focus();

            FnControls.SetEnabled(GetWnd().TxbId, isNew);

            FnControls.SetVisible(GetWnd().BtnSave, GetWnd().ViewIndicator.Count != 0);
        }

        protected override void BuildImpl()
        {
            // The ID is the primary key: once the definition is saved it can no longer be changed
            if (GetApp().T1APRDH!.State == UIXTableObjectState.Available)
                FnControls.SetEnabled(GetWnd().TxbId, false);
        }

        protected override void CheckImpl()
        {
            string id = GetWnd().TxbId.Text;

            if (FnString.IsNullEmptyOrWhitespace(id))
                AddViewError(GetWnd().TxbId, FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("App name"));
            else if (id.Length > 300)
                AddViewError(GetWnd().TxbId, FnErrorMessage.ErrorMessage.CannotExceedChars.GetMessage("App name", "300"));
            else if (GetApp().T1APRDH!.State == UIXTableObjectState.New && new TXAPRDH().Read(id) != null)
                AddViewError(GetWnd().TxbId, FnErrorMessage.ErrorMessage.GetCustomMessage($"An application properties definition with the app name \"{id}\" already exists."));

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbDescription.Text))
                AddViewError(GetWnd().TxbDescription, FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Description"));
            else if (GetWnd().TxbDescription.Text.Length > 300)
                AddViewError(GetWnd().TxbDescription, FnErrorMessage.ErrorMessage.CannotExceedChars.GetMessage("Description", "300"));
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
            GetViewReturn<ApplicationPropertiesEditViewReturn>().Canceled = true;

            // The event pipeline saves valid changes immediately, so the overview has to refresh even when the window is closed
            GetViewReturn<ApplicationPropertiesEditViewReturn>().HasChanged = GetWnd().ViewIndicator.Count != 0;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private ApplicationPropertiesEditApp GetApp()
        {
            return (ApplicationPropertiesEditApp)App;
        }

        private ApplicationPropertiesEditView GetWnd()
        {
            return (ApplicationPropertiesEditView)View;
        }

        protected void EV_BtnSave()
        {
            GetViewReturn<ApplicationPropertiesEditViewReturn>().HasChanged = true;

            Exit(true);
        }
    }
}
