using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.SelectionLists.Controller;
using GameTimeNext.Core.Application.SelectionLists.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.SelectionLists.Selectors
{
    /// <summary>
    /// The codetables a user may change, listed by their description.
    /// </summary>
    public class SelectionListsView_ListsSelectorAdapter : UIXSelectorAdapterBase<SelectionListsApp, SelectionListsView, UIXDataGrid, SelectionListDataGridRow>
    {
        public SelectionListsView_ListsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<SelectionListDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            return await Task.Run(BuildRows, token);
        }

        private List<SelectionListDataGridRow> BuildRows()
        {
            List<SelectionListDataGridRow> rows = new List<SelectionListDataGridRow>();

            UIXQuery query = new UIXQuery(K1CTABH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1CTABH.Name, K1CTABH.Fields.TXTYP);
            query.AddWhere(K1CTABH.Name, K1CTABH.Fields.PERMI, QueryCompareType.EQUALS, "U");
            query.AddOrderBy(K1CTABH.Name, K1CTABH.Fields.DESCR, OrderDirection.ASC);

            TXCTABH txctabh = new TXCTABH();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string txtyp = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.TXTYP, string.Empty);

                    T1CTABH t1ctabh = txctabh.Read(txtyp);

                    SelectionListDataGridRow row = GetSelector().CreateNewRow<SelectionListDataGridRow>();
                    row.CONAME = t1ctabh.DESCR;
                    row.RowObject = t1ctabh;

                    rows.Add(row);
                }
            }

            return rows;
        }
    }
}
