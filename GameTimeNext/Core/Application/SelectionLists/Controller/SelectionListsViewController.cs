using GameTimeNext.Core.Application.SelectionLists.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.SelectionLists.Controller
{
    public class SelectionListsViewController : UIXViewControllerBase<SelectionListsApp, SelectionListsView>
    {
        // Text type of the list whose entries are shown, to not reload them when the same list is selected again
        private string? _shownTxtyp;
        public SelectionListsViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Loading your lists...",
                                            "Sorting the choices...",
                                            "Looking up what you can pick from...",
                                            "Unpacking the dropdowns...",
                                            "Counting the options..."
                                            );
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            if (!await GetView().dgLists.LoadDataAsync())
                return;

            // The first list is opened, so the right side is never empty without a reason
            if (GetView().dgLists.Items.OfType<SelectionListDataGridRow>().FirstOrDefault() is SelectionListDataGridRow firstRow)
                GetView().dgLists.SelectedItem = firstRow;

            await ShowSelectedListAsync(true);
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
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
        #endregion

        /// <summary>
        /// The list chosen on the left, or null if there is none.
        /// </summary>
        public T1CTABH? GetSelectedList()
        {
            return (GetView().dgLists.SelectedItem as SelectionListDataGridRow)?.RowObject as T1CTABH;
        }

        /// <summary>
        /// Shows the title, the columns and the entries of the chosen list. Does nothing when this list is already shown,
        /// unless force is set (after entries were changed).
        /// </summary>
        public async Task ShowSelectedListAsync(bool force = false)
        {
            T1CTABH? list = GetSelectedList();

            if (!force && list?.TXTYP == _shownTxtyp)
                return;

            _shownTxtyp = list?.TXTYP;

            // The search belongs to one list
            GetView().txbSearch.Clear();

            GetView().txListTitle.Text = list?.DESCR ?? "No lists available";
            FnControls.SetVisible(GetView().btnAdd, list != null);

            // The parameters of the list become columns, named like the fields in the edit dialog
            for (int i = 1; i <= CFSelectionListsApp.MaxParameterCount; i++)
            {
                DataGridColumn column = GetView().dgEntries.Columns[i];
                bool isActive = list != null && CFSelectionListsApp.IsParameterSupported(list, i);

                column.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;

                if (isActive)
                    column.Header = CFSelectionListsApp.GetParameterLabel(list!, i);
            }

            await GetView().dgEntries.LoadDataAsync();
        }

        /// <summary>
        /// Enter in the search field (the engine only passes Enter on, not every typed character).
        /// </summary>
        protected async Task EV_txbSearch()
        {
            await GetView().dgEntries.LoadDataAsync();
        }

        protected async Task EV_btnRefresh()
        {
            await GetView().dgEntries.LoadDataAsync();
        }

        protected void EV_btnAdd()
        {
            if (GetSelectedList() is not T1CTABH list)
                return;

            SelectionListEntryEditApp app = GetApp().GetApplication<SelectionListEntryEditApp>(UIXApplicationStartTarget.Window);
            app.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await ShowSelectedListAsync(true);
            }, list);
        }
    }
}
