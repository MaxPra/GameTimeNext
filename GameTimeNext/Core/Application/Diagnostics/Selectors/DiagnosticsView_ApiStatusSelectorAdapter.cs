using GameTimeNext.Core.Application.Diagnostics.Controller;
using GameTimeNext.Core.Application.Diagnostics.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Http;
using GameTimeNext.Core.Framework.Igdb;
using GameTimeNext.Core.Framework.SteamGridDB;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Diagnostics.Selectors
{
    /// <summary>
    /// Status of the external APIs: every configured API is checked with a small test request on each load (open, refresh),
    /// so the list shows the current state, together with the statistics of the requests since the app started.
    /// </summary>
    public class DiagnosticsView_ApiStatusSelectorAdapter : UIXSelectorAdapterBase<DiagnosticsApp, DiagnosticsView, UIXDataGrid, ApiStatusDataGridRow>
    {
        // An API that doesn't answer within this time counts as not reachable
        private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

        public DiagnosticsView_ApiStatusSelectorAdapter(UIXApplication app) : base(app)
        {
        }

        protected override async Task<IEnumerable<ApiStatusDataGridRow>> ExecuteAsync(CancellationToken token)
        {
            // Settings are read on the UI thread; checks and rows run in the background
            AppSettings settings = AppEnvironment.GetAppConfig().AppSettings;

            string steamGridDbKey = settings.SteamGridDbKey?.Trim() ?? string.Empty;
            bool igdbConfigured = settings.EnableTwitchIGDB
                && !FnString.IsNullEmptyOrWhitespace(settings.TwitchIGDBClientID)
                && !FnString.IsNullEmptyOrWhitespace(settings.TwitchIGDBClientSecret);
            string igdbClientId = settings.TwitchIGDBClientID?.Trim() ?? string.Empty;
            string igdbClientSecret = settings.TwitchIGDBClientSecret?.Trim() ?? string.Empty;

            Dictionary<string, string> configurations = new Dictionary<string, string>
            {
                [FnApiStatus.SteamGridDB] = FnString.IsNullEmptyOrWhitespace(steamGridDbKey) ? string.Empty : "API key set",
                [FnApiStatus.Igdb] = igdbConfigured ? "Client ID and secret set" : string.Empty
            };

            return await Task.Run(async () =>
            {
                List<Task> checks = new List<Task>();

                if (!FnString.IsNullEmptyOrWhitespace(steamGridDbKey))
                    checks.Add(CheckAsync(FnApiStatus.SteamGridDB, ct => new SteamGridDBClient(steamGridDbKey).CheckConnectionAsync(ct), token));

                if (igdbConfigured)
                    checks.Add(CheckAsync(FnApiStatus.Igdb, ct => IgdbService.CheckConnectionAsync(igdbClientId, igdbClientSecret, ct), token));

                await Task.WhenAll(checks);

                return FnApiStatus.GetAll().Select(status => BuildRow(status, configurations[status.Name])).ToList();
            }, token);
        }

        private static Task CheckAsync(string apiName, Func<CancellationToken, Task> check, CancellationToken token)
        {
            return FnApiStatus.CheckAsync(FnApiStatus.Get(apiName), check, CheckTimeout, token);
        }

        private ApiStatusDataGridRow BuildRow(ApiStatus status, string configuration)
        {
            bool configured = !FnString.IsNullEmptyOrWhitespace(configuration);
            bool checkFailed = !FnString.IsNullEmptyOrWhitespace(status.LastCheckError);

            ApiStatusDataGridRow row = GetSelector().CreateNewRow<ApiStatusDataGridRow>();
            row.COAPI = status.Name;
            row.COCONF = configured ? configuration : "Not configured";
            row.COCHCK = configured && status.LastCheckAt != null
                ? $"{CFDiagnosticsApp.FormatTimestamp(status.LastCheckAt)} · {CFDiagnosticsApp.FormatDuration(status.LastCheckDuration)}"
                : string.Empty;
            row.COLREQ = CFDiagnosticsApp.FormatTimestamp(status.LastRequestAt);
            row.COLSUC = CFDiagnosticsApp.FormatTimestamp(status.LastSuccessAt);
            row.COREQS = status.RequestCount.ToString("N0", CultureInfo.CurrentCulture);
            row.COERRS = status.ErrorCount.ToString("N0", CultureInfo.CurrentCulture);
            row.COAVGD = status.RequestCount == 0 ? string.Empty : CFDiagnosticsApp.FormatDuration(status.AverageDuration);
            row.RowObject = status;

            // The reason of a failed check comes first, otherwise the last failed request
            if (configured && checkFailed)
                row.COLERR = $"{CFDiagnosticsApp.FormatTimestamp(status.LastCheckAt)}: {status.LastCheckError}";
            else if (status.LastErrorAt != null)
                row.COLERR = $"{CFDiagnosticsApp.FormatTimestamp(status.LastErrorAt)}: {status.LastError}";

            // Status: the check of this load decides
            if (!configured)
            {
                row.COSTAT = "Not configured";
                row.COSTAT.Foreground = FnColor.TextDisabled();
                row.COCONF.Foreground = FnColor.Warning();
            }
            else if (checkFailed)
            {
                row.COSTAT = "Error";
                row.COSTAT.Foreground = FnColor.Danger();
                row.COLERR.Foreground = FnColor.Danger();
            }
            else
            {
                row.COSTAT = "OK";
                row.COSTAT.Foreground = FnColor.Success();
            }

            return row;
        }
    }
}
