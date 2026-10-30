using GameTimeNext.Core.Application.ApplicationProperties.Controller;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ApplicationProperties.Selectors
{
    public class ApplicationPropertiesView_ApplicationPropertiesSelectorAdapter : UIXSelectorAdapterBase<ApplicationPropertiesApp, ApplicationPropertiesView, UIXDataGrid, ApplicationPropertiesDataGridRow>
    {
        public ApplicationPropertiesView_ApplicationPropertiesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<ApplicationPropertiesDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Filters are read on the UI thread, the query runs in the background
            string apdid = GetView().txbId.Text;
            string apdes = GetView().txbDescription.Text;

            return await Task.Run(() => BuildRows(apdid, apdes), token);
        }

        private List<ApplicationPropertiesDataGridRow> BuildRows(string apdid, string apdes)
        {
            List<ApplicationPropertiesDataGridRow> rows = new List<ApplicationPropertiesDataGridRow>();

            UIXQuery query = BuildQuery(apdid, apdes);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string readApdid = UIXQuery.GetString(reader, K1APRDH.Name, K1APRDH.Fields.APDID, string.Empty);

                    T1APRDH? t1aprdh = new TXAPRDH().Read(readApdid);

                    if (t1aprdh == null)
                        continue;

                    ApplicationPropertiesDataGridRow row = GetSelector().CreateNewRow<ApplicationPropertiesDataGridRow>();
                    row.APAPDID = t1aprdh.APDID;
                    row.APAPDES = t1aprdh.APDES;
                    row.APAPMCP = t1aprdh.APMCP;
                    row.RowObject = t1aprdh;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private static UIXQuery BuildQuery(string apdid, string apdes)
        {
            UIXQuery query = new UIXQuery(K1APRDH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1APRDH.Name, K1APRDH.Fields.APDID);

            if (!FnString.IsNullEmptyOrWhitespace(apdid))
                query.AddWhere(K1APRDH.Name, K1APRDH.Fields.APDID, QueryCompareType.LIKE, apdid);

            if (!FnString.IsNullEmptyOrWhitespace(apdes))
                query.AddWhere(K1APRDH.Name, K1APRDH.Fields.APDES, QueryCompareType.LIKE, apdes);

            query.SetTopX(1000);
            query.AddOrderBy(K1APRDH.Name, K1APRDH.Fields.APDID, OrderDirection.ASC);

            return query;
        }
    }
}
