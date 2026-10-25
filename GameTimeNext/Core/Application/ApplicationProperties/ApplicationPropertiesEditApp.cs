using GameTimeNext.Core.Application.ApplicationProperties.Controller;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.ApplicationProperties
{
    public class ApplicationPropertiesEditApp : UIXApplication
    {

        public ApplicationPropertiesEditView? ApplicationPropertiesEditView { get; set; }
        public ApplicationPropertiesEditViewController? ApplicationPropertiesEditViewController { get; set; }
        public T1APRDH? T1APRDH { get; set; } = new T1APRDH();

        public override void InitializeApplicationOutput()
        {
            ApplicationPropertiesEditView = new ApplicationPropertiesEditView();
            MainView = ApplicationPropertiesEditView;

            ApplicationPropertiesEditViewController = new ApplicationPropertiesEditViewController(this);
            ApplicationPropertiesEditView.ViewController = ApplicationPropertiesEditViewController;
        }

        public void CreateNew(Action<ApplicationPropertiesEditViewController.ApplicationPropertiesEditViewReturn> callback)
        {
            T1APRDH = new TXAPRDH().CreateNew();
            ApplicationPropertiesEditView!.ViewIndicator.Clear();
            ApplicationPropertiesEditView!.ViewIndicator.Add("CN");
            ApplicationPropertiesEditView!.Title = "Create New Application Properties Definition";
            ApplicationPropertiesEditViewController!.SetResultCallback(callback);

            ApplicationPropertiesEditViewController.Show();
        }

        public void Copy(Action<ApplicationPropertiesEditViewController.ApplicationPropertiesEditViewReturn> callback, T1APRDH t1aprdhSource)
        {
            // The ID is the primary key, so the copy starts without one and the user has to enter a new ID
            T1APRDH = new TXAPRDH().CreateNew();
            T1APRDH.APDES = t1aprdhSource.APDES;
            T1APRDH.APHPT = t1aprdhSource.APHPT;
            T1APRDH.APMCP = t1aprdhSource.APMCP;

            ApplicationPropertiesEditView!.ViewIndicator.Clear();
            ApplicationPropertiesEditView!.ViewIndicator.Add("CO");
            ApplicationPropertiesEditView!.Title = "Copy Application Properties Definition";
            ApplicationPropertiesEditViewController!.SetResultCallback(callback);

            ApplicationPropertiesEditViewController.Show();
        }

        public void Properties(Action<ApplicationPropertiesEditViewController.ApplicationPropertiesEditViewReturn> callback, T1APRDH t1aprdh, bool edit)
        {
            ApplicationPropertiesEditView!.ViewIndicator.Clear();

            if (edit)
                ApplicationPropertiesEditView!.ViewIndicator.Add("ED");

            T1APRDH = t1aprdh;

            ApplicationPropertiesEditView!.Title = edit ? "Edit Application Properties Definition" : "View Application Properties Definition";
            ApplicationPropertiesEditViewController!.SetResultCallback(callback);

            ApplicationPropertiesEditViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
