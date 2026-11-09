using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.SelectionLists.Controller;
using GameTimeNext.Core.Application.SelectionLists.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.SelectionLists
{
    /// <summary>
    /// Dialog to add or change one entry of a selection list. The key of the entry is generated and never shown.
    /// </summary>
    public class SelectionListEntryEditApp : UIXApplication
    {
        public SelectionListEntryEditView? SelectionListEntryEditView { get; set; }
        public SelectionListEntryEditViewController? SelectionListEntryEditViewController { get; set; }

        /// <summary>The list the entry belongs to (its parameters decide which fields are shown).</summary>
        public T1CTABH? List { get; set; }

        public T1CTABD? T1CTABD { get; set; }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(SelectionListEntryEditApp));
        }

        public override void InitializeApplicationOutput()
        {
            SelectionListEntryEditView = new SelectionListEntryEditView();
            MainView = SelectionListEntryEditView;

            SelectionListEntryEditViewController = new SelectionListEntryEditViewController(this);
            SelectionListEntryEditView.ViewController = SelectionListEntryEditViewController;
        }

        public void CreateNew(Action<SelectionListEntryEditViewController.SelectionListEntryEditViewReturn> callback, T1CTABH list)
        {
            List = list;

            T1CTABD = new TXCTABD().CreateNew();
            T1CTABD.TXTYP = list.TXTYP;
            T1CTABD.TXNUM = CFSelectionListsApp.GenerateKey(list.TXTYP);

            SelectionListEntryEditView!.ViewIndicator.Clear();
            SelectionListEntryEditView.ViewIndicator.Add("CN");
            SelectionListEntryEditView.Title = $"New entry in \"{list.DESCR}\"";

            SelectionListEntryEditViewController!.SetResultCallback(callback);
            SelectionListEntryEditViewController.Show();
        }

        public void Edit(Action<SelectionListEntryEditViewController.SelectionListEntryEditViewReturn> callback, T1CTABD t1ctabd, T1CTABH list)
        {
            List = list;
            T1CTABD = t1ctabd;

            SelectionListEntryEditView!.ViewIndicator.Clear();
            SelectionListEntryEditView.ViewIndicator.Add("ED");
            SelectionListEntryEditView.Title = $"Edit entry of \"{list.DESCR}\"";

            SelectionListEntryEditViewController!.SetResultCallback(callback);
            SelectionListEntryEditViewController.Show();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
