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
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.ErrorMessages.Controller
{
    public class ErrorMessagesEditViewController : UIXViewControllerBase<ErrorMessagesEditApp, ErrorMessagesEditView>
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
            bool isNew = GetView().ViewIndicator.Contains("CN") || GetView().ViewIndicator.Contains("CO");

            if (isNew)
                GetView().txbNumber.Focus();
            else if (GetView().ViewIndicator.Contains("ED"))
                GetView().txbMessage.Focus();

            FnControls.SetEnabled(GetView().txbNumber, isNew);

            FnControls.SetVisible(GetView().btnSave, GetView().ViewIndicator.Count != 0);
        }

        protected override void BuildImpl()
        {
            // The number is the primary key: once the error message is saved it can no longer be changed
            if (GetApp().T1ERMSG!.State == UIXTableObjectState.Available)
                FnControls.SetEnabled(GetView().txbNumber, false);
        }

        protected override void CheckImpl()
        {
            string number = GetView().txbNumber.Text;

            if (FnString.IsNullEmptyOrWhitespace(number))
                AddViewError(GetView().txbNumber, GetErrorMessage("GLO0003", "Number"));
            else if (number.Length > 7)
                AddViewError(GetView().txbNumber, GetErrorMessage("GLO0004", "Number", "7"));
            else if (GetApp().T1ERMSG!.State == UIXTableObjectState.New && new TXERMSG().Read(number) != null)
                AddViewError(GetView().txbNumber, GetErrorMessage("GLO0008", "Error message", number));

            if (FnString.IsNullEmptyOrWhitespace(GetView().txbMessage.Text))
                AddViewError(GetView().txbMessage, GetErrorMessage("GLO0003", "Message"));
            else if (GetView().txbMessage.Text.Length > 250)
                AddViewError(GetView().txbMessage, GetErrorMessage("GLO0004", "Message", "250"));

            if (GetView().txbHint.Text.Length > 500)
                AddViewError(GetView().txbHint, GetErrorMessage("GLO0004", "Hint", "500"));
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
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
            GetViewReturn<ErrorMessagesEditViewReturn>().HasChanged = GetView().ViewIndicator.Count != 0;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected void EV_btnSave()
        {
            GetViewReturn<ErrorMessagesEditViewReturn>().HasChanged = true;

            Exit(true);
        }
    }
}
