using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Codetables
{
    public class CodetablesEntrysApp : UIXApplication
    {
        public CodetablesEntrysView? CodetablesEntrysView { get; set; }
        public CodetablesEntrysViewController? CodetablesEntrysEditViewController { get; set; }
        public T1CTABH? T1CTABH { get; set; }

        public override void InitializeApplicationOutput()
        {
            CodetablesEntrysView = new CodetablesEntrysView();
            MainView = CodetablesEntrysView;
            CodetablesEntrysEditViewController = new CodetablesEntrysViewController(this);
            CodetablesEntrysView.WndController = CodetablesEntrysEditViewController;
        }

        public void Edit(Action<CodetablesEntrysViewController.CodetablesEntrysViewReturn> callback, T1CTABH t1ctabh)
        {
            T1CTABH = t1ctabh;

            CodetablesEntrysView!.ViewIndicator.Add("ED");
            CodetablesEntrysView!.Title = "Codetable Entrys";
            CodetablesEntrysEditViewController!.SetResultCallback(callback);
            CodetablesEntrysEditViewController!.Show(true);
        }

        public void View(T1CTABH t1ctabh)
        {
            T1CTABH = t1ctabh;
            CodetablesEntrysView!.ViewIndicator.Clear();
            CodetablesEntrysView!.Title = "Codetable Entrys";
            CodetablesEntrysEditViewController!.Show(true);
        }
    }
}
