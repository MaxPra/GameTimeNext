using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Codetables.Selectors
{
    public class CodetablesEntrysView_CodetableEntrysSelectorAdapter : UIXSelectorAdapterBase<CodetablesEntrysApp, CodetablesEntrysView, UIXDataGrid, CodetableEntryDataGridRow>
    {
        public CodetablesEntrysView_CodetableEntrysSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<CodetableEntryDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            string txtyp = GetApp().T1CTABH!.TXTYP;

            return await Task.Run(() => BuildRows(txtyp), token);
        }

        private List<CodetableEntryDataGridRow> BuildRows(string txtyp)
        {
            UIXQuery query = BuildQuery(txtyp);

            List<CodetableEntryDataGridRow> rows = new List<CodetableEntryDataGridRow>();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    TXCTABD txctabd = new TXCTABD();

                    string readTxtyp = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXTYP, string.Empty);
                    string txnum = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXNUM, string.Empty);

                    T1CTABD t1ctabd = txctabd.Read(readTxtyp, txnum);

                    CodetableEntryDataGridRow row = GetSelector().CreateNewRow<CodetableEntryDataGridRow>();
                    row.COTXTYP = readTxtyp;
                    row.COTXNUM = txnum;
                    row.CODESCR = t1ctabd.DESCR;
                    row.RowObject = t1ctabd;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private static UIXQuery BuildQuery(string txtyp)
        {
            UIXQuery query = new UIXQuery(K1CTABD.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);

            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXTYP, QueryCompareType.EQUALS, txtyp);

            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.TXNUM, OrderDirection.ASC);

            return query;
        }
    }
}
