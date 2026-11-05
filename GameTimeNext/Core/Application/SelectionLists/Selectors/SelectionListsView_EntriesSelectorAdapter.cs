using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.SelectionLists.Controller;
using GameTimeNext.Core.Application.SelectionLists.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.SelectionLists.Selectors
{
    /// <summary>
    /// The entries of the list chosen on the left, sorted by name. Entries delivered with the app are marked with a lock.
    /// </summary>
    public class SelectionListsView_EntriesSelectorAdapter : UIXSelectorAdapterBase<SelectionListsApp, SelectionListsView, UIXDataGrid, SelectionListEntryDataGridRow>
    {
        public SelectionListsView_EntriesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<SelectionListEntryDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            T1CTABH? list = GetViewController<SelectionListsViewController>().GetSelectedList();

            if (list == null)
            {
                ShowEmptyState(null, 0, false);
                return new List<SelectionListEntryDataGridRow>();
            }

            // Read on the UI thread, the rows are built in the background
            string search = GetView().txbSearch.Text.Trim();

            List<SelectionListEntryDataGridRow> rows = await Task.Run(() => BuildRows(list, search), token);

            // A newer load cancels this one: then the state of the view is left to it
            if (!token.IsCancellationRequested)
                ShowEmptyState(list, rows.Count, !FnString.IsNullEmptyOrWhitespace(search));

            return rows;
        }

        private void ShowEmptyState(T1CTABH? list, int count, bool isSearching)
        {
            GetView().txNoEntries.Text = list == null ? "No lists available" : isSearching ? "No matching entries" : "Nothing here yet";
            GetView().txNoEntriesHint.Text = list == null
                ? string.Empty
                : isSearching ? "Try another name." : "Click \"Add\" to create the first entry.";
            FnControls.SetVisible(GetView().pnlNoEntries, count == 0);
        }

        private List<SelectionListEntryDataGridRow> BuildRows(T1CTABH list, string search)
        {
            List<SelectionListEntryDataGridRow> rows = new List<SelectionListEntryDataGridRow>();

            UIXQuery query = new UIXQuery(K1CTABD.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);
            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXTYP, QueryCompareType.EQUALS, list.TXTYP);
            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.DESCR, OrderDirection.ASC);

            TXCTABD txctabd = new TXCTABD();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string txtyp = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXTYP, string.Empty);
                    string txnum = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXNUM, string.Empty);

                    T1CTABD t1ctabd = txctabd.Read(txtyp, txnum);

                    if (!FnString.IsNullEmptyOrWhitespace(search) && !t1ctabd.DESCR.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    SelectionListEntryDataGridRow row = GetSelector().CreateNewRow<SelectionListEntryDataGridRow>();
                    row.CONAME = t1ctabd.DESCR;
                    row.COPARM1 = CFSelectionListsApp.GetParameterText(list, 1, t1ctabd);
                    row.COPARM2 = CFSelectionListsApp.GetParameterText(list, 2, t1ctabd);
                    row.RowObject = t1ctabd;

                    if (!CFSelectionListsApp.IsEntryEditable(list, t1ctabd))
                    {
                        row.Icon = UIXMdlIcons.Permissions;
                        row.RowForeground = FnColor.TextSecondary();
                    }

                    rows.Add(row);
                }
            }

            return rows;
        }
    }
}
