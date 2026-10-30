using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.ErrorMessages.Controller;
using GameTimeNext.Core.Application.ErrorMessages.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Globalization;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ErrorMessages.Selectors
{
    public class ErrorMessagesView_ErrorMessagesSelectorAdapter : UIXSelectorAdapterBase<ErrorMessagesApp, ErrorMessagesView, UIXDataGrid, ErrorMessagesDataGridRow>
    {
        public ErrorMessagesView_ErrorMessagesSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<ErrorMessagesDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Filters are read on the UI thread, the query runs in the background
            string ermnr = GetView().txbNumber.Text;
            string ermsg = GetView().txbMessage.Text;

            return await Task.Run(() => BuildRows(ermnr, ermsg), token);
        }

        private List<ErrorMessagesDataGridRow> BuildRows(string ermnr, string ermsg)
        {
            List<ErrorMessagesDataGridRow> rows = new List<ErrorMessagesDataGridRow>();

            UIXQuery query = BuildQuery(ermnr, ermsg);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string readErmnr = UIXQuery.GetString(reader, K1ERMSG.Name, K1ERMSG.Fields.ERMNR, string.Empty);

                    T1ERMSG? t1ermsg = new TXERMSG().Read(readErmnr);

                    if (t1ermsg == null)
                        continue;

                    ErrorMessagesDataGridRow row = GetSelector().CreateNewRow<ErrorMessagesDataGridRow>();
                    row.ERERMNR = t1ermsg.ERMNR;
                    row.ERERMSG = t1ermsg.ERMSG;
                    row.ERERHIN = t1ermsg.ERHIN;
                    row.ERCHAT = t1ermsg.CHAT == DateTime.MinValue ? string.Empty : t1ermsg.CHAT.ToString("g", CultureInfo.CurrentCulture);
                    row.ERCHUS = t1ermsg.CHUS;
                    row.RowObject = t1ermsg;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private static UIXQuery BuildQuery(string ermnr, string ermsg)
        {
            UIXQuery query = new UIXQuery(K1ERMSG.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERMNR);

            if (!FnString.IsNullEmptyOrWhitespace(ermnr))
                query.AddWhere(K1ERMSG.Name, K1ERMSG.Fields.ERMNR, QueryCompareType.LIKE, ermnr);

            if (!FnString.IsNullEmptyOrWhitespace(ermsg))
                query.AddWhere(K1ERMSG.Name, K1ERMSG.Fields.ERMSG, QueryCompareType.LIKE, ermsg);

            query.SetTopX(1000);
            query.AddOrderBy(K1ERMSG.Name, K1ERMSG.Fields.ERMNR, OrderDirection.ASC);

            return query;
        }
    }
}
