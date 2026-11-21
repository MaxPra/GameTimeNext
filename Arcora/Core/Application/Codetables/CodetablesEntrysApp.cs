using Arcora.Core.Application.Codetables.Controller;
using Arcora.Core.Application.Codetables.Selectors;
using Arcora.Core.Application.Codetables.Views;
using Arcora.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace Arcora.Core.Application.Codetables
{
    public class CodetablesEntrysApp : UIXApplication
    {
        public CodetablesEntrysView? CodetablesEntrysView { get; set; }
        public CodetablesEntrysViewController? CodetablesEntrysEditViewController { get; set; }
        public T1CTABH? T1CTABH { get; set; }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(CodetablesEntrysApp));
        }

        public override void InitializeApplicationOutput()
        {
            CodetablesEntrysView = new CodetablesEntrysView();
            MainView = CodetablesEntrysView;
            CodetablesEntrysEditViewController = new CodetablesEntrysViewController(this);
            CodetablesEntrysView.ViewController = CodetablesEntrysEditViewController;
            CodetablesEntrysView.dgCodetableEntrys.SelectorAdapter = new CodetablesEntrysView_CodetableEntrysSelectorAdapter(this);
            CodetablesEntrysView.dgCodetableEntrys.SelectorController = new CodetablesEntrysView_CodetableEntrysSelectorController(this);
        }

        public void Edit(T1CTABH t1ctabh)
        {
            T1CTABH = t1ctabh;

            CodetablesEntrysView!.ViewIndicator.Add("ED");
            CodetablesEntrysView!.Title = "Codetable Entrys";

            CodetablesEntrysEditViewController!.Show();
        }

        public void View(T1CTABH t1ctabh)
        {
            T1CTABH = t1ctabh;
            CodetablesEntrysView!.ViewIndicator.Clear();
            CodetablesEntrysView!.Title = "Codetable Entrys";

            CodetablesEntrysEditViewController!.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
