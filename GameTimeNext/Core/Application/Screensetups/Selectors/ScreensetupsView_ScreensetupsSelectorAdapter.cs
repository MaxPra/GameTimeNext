using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Screensetups.Controller;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Screensetups.Selectors
{
    public class ScreensetupsView_ScreensetupsSelectorAdapter : UIXSelectorAdapterBase<ScreensetupsApp, ScreensetupsView, UIXDataGrid, ScreensetupDataGridRow>
    {
        public ScreensetupsView_ScreensetupsSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<ScreensetupDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Filter is read on the UI thread, the query runs in the background
            string ssna = GetView().TxbName.Text;

            return await Task.Run(() => BuildRows(ssna), token);
        }

        private List<ScreensetupDataGridRow> BuildRows(string ssna)
        {
            List<ScreensetupDataGridRow> rows = new List<ScreensetupDataGridRow>();

            long selectedScreensetup = AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup;

            UIXQuery query = BuildQuery(ssna);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long ssid = UIXQuery.GetInt64(reader, K1SCRSH.Name, K1SCRSH.Fields.SSID);

                    T1SCRSH? t1scrsh = new TXSCRSH().Read(ssid);

                    if (t1scrsh == null)
                        continue;

                    List<T1SCRSP> t1scrsps = TFSCRSP.GetMonitorsOfScreensetup(ssid);

                    ScreensetupDataGridRow row = GetSelector().CreateNewRow<ScreensetupDataGridRow>();
                    row.COSSNA = t1scrsh.SSNA;
                    row.COSSDE = t1scrsh.SSDE;
                    row.COMONI = CFScreensetupsApp.FormatMonitors(t1scrsps);
                    row.COAUDI = CFScreensetupsApp.FormatAudioDevice(t1scrsh);
                    row.COISACT = t1scrsh.SSID == selectedScreensetup;
                    row.RowObject = t1scrsh;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private static UIXQuery BuildQuery(string ssna)
        {
            UIXQuery query = new UIXQuery(K1SCRSH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSID);

            if (!FnString.IsNullEmptyOrWhitespace(ssna))
                query.AddWhere(K1SCRSH.Name, K1SCRSH.Fields.SSNA, QueryCompareType.LIKE, ssna);

            query.SetTopX(1000);
            query.AddOrderBy(K1SCRSH.Name, K1SCRSH.Fields.SSNA, OrderDirection.ASC);

            return query;
        }
    }
}
