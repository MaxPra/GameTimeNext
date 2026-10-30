using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Selectors
{
    public class CodetablesView_CodetablesSelectorAdapter : UIXSelectorAdapterBase<CodetablesApp, CodetablesView, UIXDataGrid, CodetableDataGridRow>
    {
        public CodetablesView_CodetablesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<CodetableDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Filters are read on the UI thread, the query runs in the background
            string txtyp = GetView().TxbTextType.Text;
            string descr = GetView().TxbDescription.Text;
            string permi = GetView().CmbPermission.SelectedValue?.ToString() ?? string.Empty;

            return await Task.Run(() => BuildRows(txtyp, descr, permi), token);
        }

        private List<CodetableDataGridRow> BuildRows(string txtyp, string descr, string permi)
        {
            List<CodetableDataGridRow> rows = new List<CodetableDataGridRow>();

            UIXQuery query = BuildQuery(txtyp, descr, permi);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    TXCTABH txctabd = new TXCTABH();

                    string readTxtyp = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.TXTYP, string.Empty);

                    T1CTABH t1ctabd = txctabd.Read(readTxtyp);

                    CodetableDataGridRow row = GetSelector().CreateNewRow<CodetableDataGridRow>();
                    row.COTXTYP = t1ctabd.TXTYP;
                    row.CODESCR = t1ctabd.DESCR;
                    row.COPERMI = TFCTABD.GetDescription("cP", t1ctabd.PERMI);
                    row.RowObject = t1ctabd;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private static UIXQuery BuildQuery(string txtyp, string descr, string permi)
        {
            UIXQuery query = new UIXQuery(K1CTABH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1CTABH.Name, K1CTABH.Fields.TXTYP);

            if (!FnString.IsNullEmptyOrWhitespace(txtyp))
                query.AddWhere(K1CTABH.Name, K1CTABH.Fields.TXTYP, QueryCompareType.EQUALS, txtyp);

            if (!FnString.IsNullEmptyOrWhitespace(descr))
                query.AddWhere(K1CTABH.Name, K1CTABH.Fields.DESCR, QueryCompareType.LIKE, descr);

            if (!FnString.IsNullEmptyOrWhitespace(permi))
                query.AddWhere(K1CTABH.Name, K1CTABH.Fields.PERMI, QueryCompareType.EQUALS, permi);

            query.SetTopX(1000);
            query.AddOrderBy(K1CTABH.Name, K1CTABH.Fields.TXTYP, OrderDirection.ASC);

            return query;
        }
    }
}
