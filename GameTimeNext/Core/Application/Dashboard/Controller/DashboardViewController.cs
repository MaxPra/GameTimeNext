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
        bool showOffsetSelection = false;

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
            FnControls.SetVisible(GetView().grdTimeSpanOffset, showOffsetSelection);
            FnControls.SetEnabled(GetView().BtnIncreaseTimeSpan, timeSpanOffset < 0);
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
            FillSectionLastPlayed();
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
                    Open(true);
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

            showOffsetSelection = false;
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
                    showOffsetSelection = true;
                    break;
                case 4:
                    // Month
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Month, offset: timeSpanOffset);
                    showOffsetSelection = true;
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
            GetView().txtStatPlayedToday.Text = CFDashboardApp.FormatTime(GetPlaytimeToday());

            {
                // Longest Session
                (string gana, double plti, DateTime plto) = GetLongestSession();
                GetView().txtStatLongestSession.Text = CFDashboardApp.FormatTime(plti);
                GetView().txtStatLongestSessionGame.Text = gana;
                GetView().txtStatLongestSessionDate.Text = $"({CFProfilesApp.FormatFirstLastDate(plto)})";
            }
        }

        private void FillSectionLastPlayed()
        {
            (string gana, DateTime lapl, double plti, string ppfn) = GetLastPlayed();
            GetView().txtLastPlayedTitle.Text = gana;
            GetView().txtLastPlayedDate.Text = CFProfilesApp.FormatFirstLastDate(lapl);
            GetView().txtLastPlayedTime.Text = CFDashboardApp.FormatTime(plti);

            var coverImage = FnImage.LoadImageWithoutLock(ppfn, 300, 450);
            GetView().imgLastPlayedCover.Source = coverImage;
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

        private double GetPlaytimeToday()
        {
            UIXQuery query = BuildQueryPlaytime(today: true);

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "TotalPlaytime");

            return 0;
        }

        private (string gana, double plti, DateTime plto) GetLongestSession()
        {
            UIXQuery query = BuildQueryLongestSession();

            using (var reader = query.Execute())
                if (reader.Read())
                    return (
                        UIXQuery.GetString(reader, "GANA"),
                        UIXQuery.GetDouble(reader, "PLTI"),
                        UIXQuery.GetDateTime(reader, "PLTO")
                    );

            return ("n.A.", 0, DateTime.MinValue);
        }

        private (string gana, DateTime lapl, double plti, string ppfn) GetLastPlayed()
        {
            UIXQuery query = BuildQueryLastPlayed();

            using (var reader = query.Execute())
                if (reader.Read())
                    return (
                        UIXQuery.GetString(reader, "GANA"),
                        UIXQuery.GetDateTime(reader, "PLTO"),
                        TFPROFI.GetGameTimeInMinutes(UIXQuery.GetInt64(reader, "PFID"), timeSpanStart, timeSpanEnd),
                        Path.Combine(AppEnvironment.GetAppConfig().CoverFolderPath ?? string.Empty, UIXQuery.GetString(reader, "PPFN"))
                    );

            return ("n.A.", DateTime.MinValue, 0, string.Empty);
        }

        private UIXQuery BuildQueryPlayedProfiles()
        {
            UIXQuery query = BuildQuerySessionsInTimeSpanBase();
            query.SetDistinct();

            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);

            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLFR, OrderDirection.DESC);

            return query;
        }

        private UIXQuery BuildQueryPlayedProfilesCount()
        {
            UIXQuery query = BuildQuerySessionsInTimeSpanBase();

            query.AddCount(K1SESSI.Name, K1SESSI.Fields.PFID, true, "TotalGames");

            return query;
        }

        private UIXQuery BuildQueryPlaytime(bool today = false)
        {
            UIXQuery query = BuildQuerySessionsInTimeSpanBase(today);

            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            return query;
        }

        private string BuildQueryDaysPlayed()
        {
            string sqlPlfr = "";
            string sqlPlto = "";

            {
                // PLFR
                UIXQuery query = BuildQuerySessionsInTimeSpanBase();
                query.AddFieldRaw($"DATE({K1SESSI.Name}.{K1SESSI.Fields.PLFR})", "PLAYDAY");

                sqlPlfr = query.PreviewQuery();
            }

            {
                // PLTO
                UIXQuery query = BuildQuerySessionsInTimeSpanBase();
                query.AddFieldRaw($"DATE({K1SESSI.Name}.{K1SESSI.Fields.PLTO})", "PLAYDAY");

                sqlPlto = query.PreviewQuery();
            }

            return $"SELECT COUNT(DISTINCT PLAYDAY) AS DAYS FROM ({sqlPlfr} UNION {sqlPlto})";
        }

        private UIXQuery BuildQueryLongestSession()
        {
            UIXQuery query = BuildQuerySessionsInTimeSpanBase();
            query.SetTopX(1);

            UIXQueryTable t1profi = query.AddJoinTable(K1PROFI.Name, JoinType.LEFT);
            t1profi.AddJoinCondition(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA, "GANA");
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI, "PLTI");
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO, "PLTO");

            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLTI, OrderDirection.DESC);

            return query;
        }

        private UIXQuery BuildQuerySessionsInTimeSpanBase(bool today = false)
        {
            DateTime? start = timeSpanStart;
            DateTime end = timeSpanEnd;

            if (today)
            {
                (start, end) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Day);
            }

            return TFSESSI.BuildQuerySessionsInTimeSpanBase(start, end);
        }

        private UIXQuery BuildQueryLastPlayed()
        {
            UIXQuery query = TFSESSI.BuildQuerySessionsInTimeSpanBase(timeSpanStart, timeSpanEnd);
            query.SetTopX(1);

            UIXQueryTable t1profi = query.AddJoinTable(K1PROFI.Name, JoinType.LEFT);
            t1profi.AddJoinCondition(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID, "PFID");
            query.AddField(K1PROFI.Name, K1PROFI.Fields.GANA, "GANA");
            query.AddField(K1PROFI.Name, K1PROFI.Fields.PPFN, "PPFN");
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO, "PLTO");

            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLTO, OrderDirection.DESC);

            return query;
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
            if (timeSpanOffset > 0) timeSpanOffset = 0;

            AppEnvironment.GetAppConfig().UserSettings.SelectedDashboardOffset = timeSpanOffset;
            AppEnvironment.SaveAppConfig();

            Open(true);
        }
    }
}
