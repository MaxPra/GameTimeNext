using GameTimeNext.Core.Application.Dashboard.ViewModels;
using GameTimeNext.Core.Application.Dashboard.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Dashboard.Controller
{
    public class DashboardViewController : UIXViewControllerBase
    {
        DashboardViewModel? viewModel = null;
        short timeSpanOffset = 0;
        DateTime? timeSpanStart = null;
        DateTime timeSpanEnd = DateTime.Today.AddDays(1).AddTicks(-1);

        public DashboardViewController(UIXApplication app) : base(app)
        {
        }

        protected override void Init()
        {
            viewModel = new DashboardViewModel();

            GetApp().Loader.SetRandomTexts(
                    "Preparing your dashboard...",
                    "Collecting your latest gaming stats...",
                    "Checking your most played adventures...",
                    "Calculating your total playtime...",
                    "Looking up your last played game...",
                    "Summarizing today’s progress...",
                    "Scanning session history...",
                    "Loading your gaming highlights...",
                    "Building your personal gaming overview...",
                    "Tracking your longest sessions...",
                    "Syncing your recent activity...",
                    "Preparing your next gaming insight...",
                    "Reviewing your playtime trends...",
                    "Updating your dashboard cards...",
                    "Gathering your gaming milestones...",
                    "Compiling your playtime universe..."
                    );

            // Pipeline unterdrücken
            using (SuppressRunEventPipeline())
            {
                GetView().CmbTimeRange.SelectedIndex = AppEnvironment.GetAppConfig().UserSettings.SelectedDashboardMode;
                timeSpanOffset = AppEnvironment.GetAppConfig().UserSettings.SelectedDashboardOffset;
                ChangeTimeSpanDays();
            }
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildPlayedProfilesListBoxAsync();
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildImplAsync()
        {
        }

        protected override void BuildImpl()
        {
        }

        protected override void Check()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
            GetView().txtTimeSpanOffset.Text = timeSpanOffset.ToString();

            FillSectionPlaytimeOverview();
            FillSectionOverallStatistics();
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override async Task TriggeredEventAsync(FrameworkElement source, string eventName)
        {
            if (source is ComboBox cmb && eventName == UIXEventNames.Selector.SelectionChanged)
            {
                if (cmb.Name == GetView().CmbTimeRange.Name)
                {
                    ChangeTimeSpanDays();

                    AppEnvironment.GetAppConfig().UserSettings.SelectedDashboardMode = (short)GetView().CmbTimeRange.SelectedIndex;
                    AppEnvironment.SaveAppConfig();

                    await BuildPlayedProfilesListBoxAsync();
                    FillView();
                }

            }
        }

        private DashboardApp GetApp()
        {
            return (DashboardApp)App;
        }

        private DashboardView GetView()
        {
            return (DashboardView)View;
        }

        private void ChangeTimeSpanDays()
        {
            int selectedIndex = GetView().CmbTimeRange.SelectedIndex;

            switch (selectedIndex)
            {
                case 0:
                    // Last 7 Days
                    timeSpanOffset = 0;
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEndDays(7);
                    break;
                case 1:
                    // Last 30 Days
                    timeSpanOffset = 0;
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEndDays(30);
                    break;
                case 2:
                    // Last 365 Days
                    timeSpanOffset = 0;
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEndDays(365);
                    break;
                case 3:
                    // Week
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Week, offset: timeSpanOffset);
                    break;
                case 4:
                    // Month
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Month, offset: timeSpanOffset);
                    break;
                default:
                    // All time
                    timeSpanOffset = 0;
                    timeSpanStart = null;
                    timeSpanEnd = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
            }
        }

        private async Task BuildPlayedProfilesListBoxAsync()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<T1PROFI> t1profis = GetPlayedProfiles();
                List<ProfilesListBoxItem> profiles = BuildProfileListBoxItems(t1profis);

                View.Dispatcher.Invoke(() =>
                {
                    viewModel = new DashboardViewModel();
                    viewModel!.T1Profis = new System.Collections.ObjectModel.ObservableCollection<ProfilesListBoxItem>(profiles);
                    GetView().DataContext = viewModel;

                    GetApp().Loader.Stop();
                });
            });
        }

        private List<ProfilesListBoxItem> BuildProfileListBoxItems(List<T1PROFI> t1profiles)
        {
            return t1profiles.Select(prof => new ProfilesListBoxItem
            {
                ItemObject = prof,
                COCOVIM = FnImage.LoadImageWithoutLock(Path.Combine(AppEnvironment.GetAppConfig().CoverFolderPath ?? string.Empty, prof.PPFN), 300, 450),
                COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF)
            }).ToList();
        }

        private void FillSectionPlaytimeOverview()
        {
            GetView().txtTotalPlaytime.Text = CFDashboardApp.FormatTime(GetPlaytime());
            GetView().txtGamesPlayed.Text = GetPlayedProfilesCount().ToString();
            GetView().txtDaysPlayed.Text = GetDaysPlayed().ToString();
        }

        private void FillSectionOverallStatistics()
        {
            // OFDOI
        }

        private List<T1PROFI> GetPlayedProfiles()
        {
            UIXQuery query = BuildQueryPlayedProfiles();

            List<T1PROFI> t1profis = new List<T1PROFI>();

            TXPROFI txprofi = new TXPROFI();
            using (var reader = query.Execute())
                while (reader.Read())
                    t1profis.Add(txprofi.Read(UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.PFID))!);

            return t1profis;
        }

        private int GetPlayedProfilesCount()
        {
            UIXQuery query = BuildQueryPlayedProfilesCount();

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetInt32(reader, "TotalGames");

            return 0;
        }

        private double GetPlaytime()
        {
            UIXQuery query = BuildQueryPlaytime();
            string sql = query.PreviewQuery();

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "TotalPlaytime");

            return 0;
        }

        private int GetDaysPlayed()
        {
            string query = BuildQueryDaysPlayed();

            using (var reader = UIXQuery.ExecuteCustom(query, AppEnvironment.GetDataBaseManager().GetConnection()))
                if (reader.Read())
                    return UIXQuery.GetInt32(reader, "DAYS");

            return 0;
        }

        private UIXQuery BuildQueryPlayedProfiles()
        {
            UIXQuery query = BuildQueryPlayedProfilesBase();
            query.SetDistinct();

            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);

            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLFR, OrderDirection.DESC);

            return query;
        }

        private UIXQuery BuildQueryPlayedProfilesCount()
        {
            UIXQuery query = BuildQueryPlayedProfilesBase();

            query.AddCount(K1SESSI.Name, K1SESSI.Fields.PFID, true, "TotalGames");

            return query;
        }

        private UIXQuery BuildQueryPlaytime()
        {
            UIXQuery query = BuildQueryPlayedProfilesBase();

            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            return query;
        }

        private UIXQuery BuildQueryPlayedProfilesBase()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            if (timeSpanStart is not null)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLTO, QueryCompareType.GREATER_OR_EQUAL, timeSpanStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.LESS_THAN, timeSpanEnd.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            return query;
        }

        private string BuildQueryDaysPlayed()
        {
            string sqlPlfr = "";
            string sqlPlto = "";

            {
                // PLFR
                UIXQuery query = BuildQueryPlayedProfilesBase();
                query.AddFieldRaw($"DATE({K1SESSI.Name}.{K1SESSI.Fields.PLFR})", "PLAYDAY");

                sqlPlfr = query.PreviewQuery();
            }

            {
                // PLTO
                UIXQuery query = BuildQueryPlayedProfilesBase();
                query.AddFieldRaw($"DATE({K1SESSI.Name}.{K1SESSI.Fields.PLTO})", "PLAYDAY");

                sqlPlto = query.PreviewQuery();
            }

            return $"SELECT COUNT(DISTINCT PLAYDAY) AS DAYS FROM ({sqlPlfr} UNION {sqlPlto})";
        }

        protected void EV_BtnRefresh()
        {
            Open(true);
        }

        protected void EV_BtnDecreaseTimeSpan()
        {
            timeSpanOffset--;

            AppEnvironment.GetAppConfig().UserSettings.SelectedDashboardOffset = timeSpanOffset;
            AppEnvironment.SaveAppConfig();

            Open(true);
        }

        protected void EV_BtnIncreaseTimeSpan()
        {
            timeSpanOffset++;

            AppEnvironment.GetAppConfig().UserSettings.SelectedDashboardOffset = timeSpanOffset;
            AppEnvironment.SaveAppConfig();

            Open(true);
        }
    }
}
