using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.ErrorMessages.Controller;
using GameTimeNext.Core.Application.ErrorMessages.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.ErrorMessages
{
    public class ErrorMessagesEditApp : UIXApplication
    {

        public ErrorMessagesEditView? ErrorMessagesEditView { get; set; }
        public ErrorMessagesEditViewController? ErrorMessagesEditViewController { get; set; }
        public T1ERMSG? T1ERMSG { get; set; } = new T1ERMSG();

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ErrorMessagesEditApp));
        }

        public override void InitializeApplicationOutput()
        {
            ErrorMessagesEditView = new ErrorMessagesEditView();
            MainView = ErrorMessagesEditView;

            ErrorMessagesEditViewController = new ErrorMessagesEditViewController(this);
            ErrorMessagesEditView.ViewController = ErrorMessagesEditViewController;
        }

        public void CreateNew(Action<ErrorMessagesEditViewController.ErrorMessagesEditViewReturn> callback)
        {
            T1ERMSG = new TXERMSG().CreateNew();
            ErrorMessagesEditView!.ViewIndicator.Clear();
            ErrorMessagesEditView!.ViewIndicator.Add("CN");
            ErrorMessagesEditView!.Title = "Create New Error Message";
            ErrorMessagesEditViewController!.SetResultCallback(callback);

            ErrorMessagesEditViewController.Show();
        }

        public void Copy(Action<ErrorMessagesEditViewController.ErrorMessagesEditViewReturn> callback, T1ERMSG t1ermsgSource)
        {
            // The number is the primary key, so the copy starts without one and the user has to enter a new number
            T1ERMSG = new TXERMSG().Copy(t1ermsgSource);

            ErrorMessagesEditView!.ViewIndicator.Clear();
            ErrorMessagesEditView!.ViewIndicator.Add("CO");
            ErrorMessagesEditView!.Title = "Copy Error Message";
            ErrorMessagesEditViewController!.SetResultCallback(callback);

            ErrorMessagesEditViewController.Show();
        }

        public void Properties(Action<ErrorMessagesEditViewController.ErrorMessagesEditViewReturn> callback, T1ERMSG t1ermsg, bool edit)
        {
            ErrorMessagesEditView!.ViewIndicator.Clear();

            if (edit)
                ErrorMessagesEditView!.ViewIndicator.Add("ED");

            T1ERMSG = t1ermsg;

            ErrorMessagesEditView!.Title = edit ? "Edit Error Message" : "View Error Message";
            ErrorMessagesEditViewController!.SetResultCallback(callback);

            ErrorMessagesEditViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
