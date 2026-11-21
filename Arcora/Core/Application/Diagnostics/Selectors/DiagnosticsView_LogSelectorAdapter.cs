using GameTimeNext.Core.Application.Diagnostics.Controller;
using GameTimeNext.Core.Application.Diagnostics.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Diagnostics.Selectors
{
    public class DiagnosticsView_LogSelectorAdapter : UIXSelectorAdapterBase<DiagnosticsApp, DiagnosticsView, UIXDataGrid, LogDataGridRow>
    {
        public DiagnosticsView_LogSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<LogDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Filters are read on the UI thread, the query runs in the background
            // Whole days: from the start of the From day up to the end of the To day
            DateTime? from = GetView().dtpFrom.SelectedDate?.Date;
            DateTime? to = GetView().dtpTo.SelectedDate?.Date.AddDays(1);
            string logType = GetView().cmbLogType.SelectedValue?.ToString() ?? string.Empty;
            string source = GetView().txbSource.Text;
            string message = GetView().txbMessage.Text;

            return await Task.Run(() => BuildRows(from, to, logType, source, message), token);
        }

        private List<LogDataGridRow> BuildRows(DateTime? from, DateTime? to, string logType, string source, string message)
        {
            List<LogDataGridRow> rows = new List<LogDataGridRow>();

            foreach (SystemLogRecord record in AppEnvironment.GetLogDataBaseManager().ReadT1sylog(from, to, logType, source, message, CFDiagnosticsApp.LogTopX))
            {
                LogDataGridRow row = GetSelector().CreateNewRow<LogDataGridRow>();

                row.Icon = GetIconForLogType(record.LGTY);

                row.COLGTS = CFDiagnosticsApp.FormatTimestamp(record.LGTS);
                row.COLGTY = record.LGTY;
                row.COLGSR = record.LGSR;
                // Single line in the list; the full text is shown below it
                row.COLGMS = record.LGMS.ReplaceLineEndings(" ");
                //row.COLGEX = FnString.IsNullEmptyOrWhitespace(record.LGEX) ? string.Empty : "Yes";
                row.RowObject = record;

                (row.RowBackground, row.RowForeground) = CFDiagnosticsApp.GetLogRowColors(record.LGTY);

                rows.Add(row);
            }

            return rows;
        }

        private string GetIconForLogType(string logType)
        {
            string icon = "";

            switch (logType)
            {
                case nameof(UIXLogType.Info):
                    icon = UIXMdlIcons.Info;
                    break;

                case nameof(UIXLogType.Warning):
                    icon = UIXMdlIcons.Warning;
                    break;

                case nameof(UIXLogType.Error):
                    icon = UIXMdlIcons.Error;
                    break;

                case nameof(GTNLogType.AppStart):
                    icon = UIXMdlIcons.Play;
                    break;

                case nameof(GTNLogType.LogCleared):
                    icon = UIXMdlIcons.Delete;
                    break;
            }

            return icon;
        }

    }
}
