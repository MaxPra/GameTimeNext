using GameTimeNext.Core.Application.Metadata.Controller;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Selectors
{
    public class MetadataEditView_FieldsSelectorAdapter : UIXSelectorAdapterBase<MetadataEditApp, MetadataEditView, UIXDataGrid, MetadataPosDataGridRow>
    {
        public MetadataEditView_FieldsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<MetadataPosDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Filter and table are read on the UI thread, the query runs in the background
            string search = GetView().TxbFieldSearch.Text;
            string menam = GetApp().T1METAH!.MENAM;

            return await Task.Run(() => BuildRows(search, menam), token);
        }

        private List<MetadataPosDataGridRow> BuildRows(string search, string menam)
        {
            List<MetadataPosDataGridRow> rows = new List<MetadataPosDataGridRow>();

            TXMETAP txmetap = new TXMETAP();

            UIXQuery query = BuildQuery(search, menam);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string readMenam = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.MENAM, string.Empty);
                    string ponam = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.PONAM, string.Empty);

                    T1METAP? t1metap = txmetap.Read(readMenam, ponam);
                    if (t1metap is null)
                        continue;

                    MetadataPosDataGridRow row = GetSelector().CreateNewRow<MetadataPosDataGridRow>();
                    row.COPONAM = t1metap.PONAM;
                    row.COPORDE = t1metap.PORDE;
                    row.COMENAM = t1metap.MENAM;
                    row.CODATYP = UIXSqliteDataType.GetByKey(t1metap.DATYP).Name;
                    row.CODESCR = t1metap.DESCR;
                    row.COPRIMK = t1metap.PRIMK;
                    row.CODALEN = t1metap.DALEN;
                    row.RowObject = t1metap;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private static UIXQuery BuildQuery(string search, string menam)
        {
            UIXQuery query = new UIXQuery(K1METAP.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1METAP.Name, K1METAP.Fields.MENAM);
            query.AddField(K1METAP.Name, K1METAP.Fields.PONAM);

            if (!FnString.IsNullEmptyOrWhitespace(search))
            {
                string like = $"%{search.Trim()}%";

                query.AddWhereRaw(
                    $"({K1METAP.Name}.{K1METAP.Fields.PONAM} LIKE ? OR {K1METAP.Name}.{K1METAP.Fields.DESCR} LIKE ?)",
                    like, like);
            }

            query.AddWhere(K1METAP.Name, K1METAP.Fields.MENAM, QueryCompareType.EQUALS, menam);

            query.AddOrderBy(K1METAP.Name, K1METAP.Fields.PORDE, OrderDirection.ASC);

            return query;
        }
    }
}
