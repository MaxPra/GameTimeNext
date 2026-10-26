using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.ErrorMessages.Views;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ErrorMessages.Controller
{
    public class ErrorMessagesEditViewController : UIXViewControllerBase
    {
        public ErrorMessagesEditViewController(UIXApplication app) : base(app)
        {
        }

        public class ErrorMessagesEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new ErrorMessagesEditViewReturn();

            AddIdentifier("T1ERMSG", GetApp().T1ERMSG!);
        }

        protected override void BuildFirstImpl()
        {
            bool isNew = GetWnd().ViewIndicator.Contains("CN") || GetWnd().ViewIndicator.Contains("CO");

            if (isNew)
                GetWnd().txbNumber.Focus();
            else if (GetWnd().ViewIndicator.Contains("ED"))
                GetWnd().txbMessage.Focus();

            FnControls.SetEnabled(GetWnd().txbNumber, isNew);

            FnControls.SetVisible(GetWnd().btnSave, GetWnd().ViewIndicator.Count != 0);
        }

        protected override void BuildImpl()
        {
            // The number is the primary key: once the error message is saved it can no longer be changed
            if (GetApp().T1ERMSG!.State == UIXTableObjectState.Available)
                FnControls.SetEnabled(GetWnd().txbNumber, false);
        }

        protected override void CheckImpl()
        {
            string number = GetWnd().txbNumber.Text;

            if (FnString.IsNullEmptyOrWhitespace(number))
                AddViewError(GetWnd().txbNumber, GetErrorMessage("GLO0003", "Number"));
            else if (number.Length > 7)
                AddViewError(GetWnd().txbNumber, GetErrorMessage("GLO0004", "Number", "7"));
            else if (GetApp().T1ERMSG!.State == UIXTableObjectState.New && new TXERMSG().Read(number) != null)
                AddViewError(GetWnd().txbNumber, GetErrorMessage("GLO0008", "Error message", number));

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbMessage.Text))
                AddViewError(GetWnd().txbMessage, GetErrorMessage("GLO0003", "Message"));
            else if (GetWnd().txbMessage.Text.Length > 250)
                AddViewError(GetWnd().txbMessage, GetErrorMessage("GLO0004", "Message", "250"));

            if (GetWnd().txbHint.Text.Length > 500)
                AddViewError(GetWnd().txbHint, GetErrorMessage("GLO0004", "Hint", "500"));
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
            GetViewReturn<ErrorMessagesEditViewReturn>().Canceled = true;

            // The event pipeline saves valid changes immediately, so the overview has to refresh even when the window is closed
            GetViewReturn<ErrorMessagesEditViewReturn>().HasChanged = GetWnd().ViewIndicator.Count != 0;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private ErrorMessagesEditApp GetApp()
        {
            return (ErrorMessagesEditApp)App;
        }

        private ErrorMessagesEditView GetWnd()
        {
            return (ErrorMessagesEditView)View;
        }

        protected void EV_btnSave()
        {
            GetViewReturn<ErrorMessagesEditViewReturn>().HasChanged = true;

            Exit(true);
        }
    }
}
