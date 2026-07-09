using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Codetables
{
    public class CodetablesEditApp : UIXApplication
    {

        public CodetablesEditView? CodetablesEditView { get; set; }
        public CodetablesEditViewController? CodetablesEditViewController { get; set; }
        public T1CTABH? T1CTABH { get; set; } = new T1CTABH();

        public override void InitializeApplicationOutput()
        {
            CodetablesEditView = new CodetablesEditView();
            MainView = CodetablesEditView;

            CodetablesEditViewController = new CodetablesEditViewController(this);
            CodetablesEditView.ViewController = CodetablesEditViewController;
        }

        public void CreateNew(Action<CodetablesEditViewController.CodetablesEditViewReturn> callback)
        {
            T1CTABH = new TXCTABH().CreateNew();
            CodetablesEditView!.ViewIndicator.Add("CN");
            CodetablesEditView!.Title = "Create New Codetable";
            CodetablesEditViewController!.SetResultCallback(callback);

            CodetablesEditViewController.Show();
        }

        public void Properties(Action<CodetablesEditViewController.CodetablesEditViewReturn> callback, T1CTABH t1ctabh, bool edit)
        {
            CodetablesEditView!.ViewIndicator.Clear();

            if (edit)
                CodetablesEditView!.ViewIndicator.Add("ED");

            T1CTABH = t1ctabh;

            CodetablesEditView!.Title = edit ? "Edit Codetable" : "View Codetable";
            CodetablesEditViewController!.SetResultCallback(callback);

            CodetablesEditViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
