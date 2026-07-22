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
                ChangeTimeSpanDays();
            }
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildProfilesListBoxAsync();
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
            FillMostPlayedSection();

            FillLastPlayedSection();

            FillPlaytimeOverviewSection();

            FillOverallStatisticsSection();
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

                    await BuildProfilesListBoxAsync();
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

        private async Task BuildProfilesListBoxAsync()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<T1PROFI> t1profis = GetRecentlyPlayedProfiles();
                List<ProfilesListBoxItem> profileItems = BuildProfileListItems(t1profis);

                View.Dispatcher.Invoke(() =>
                {
                    viewModel = new DashboardViewModel();
                    viewModel!.T1Profis = new System.Collections.ObjectModel.ObservableCollection<ProfilesListBoxItem>(profileItems);
                    GetView().DataContext = viewModel;

                    GetApp().Loader.Stop();
                });
            });
        }

        private void FillMostPlayedSection()
        {
            T1PROFI t1profiMostPlayed = DetermineMostPlayedProfile();

            if (t1profiMostPlayed == null)
                return;

            string coverPath = Path.Combine(AppEnvironment.GetAppConfig().CoverFolderPath ?? string.Empty, t1profiMostPlayed.PPFN);
            var coverImage = FnImage.LoadImageWithoutLock(coverPath, 300, 450);

            GetView().txtMostPlayedTitle.Text = t1profiMostPlayed.GANA;
            GetView().imgMostPlayedCover.Source = coverImage;
            GetView().txtMostPlayedPlaytime.Text = CFDashboardApp.FormatTime(TFPROFI.GetTotalGameTimeInMinutes(t1profiMostPlayed.PFID));

            int daysPlayed = TFSESSI.GetPlayedDays(t1profiMostPlayed.PFID, timeSpanStart, timeSpanEnd);
            GetView().txtMostPlayedDays.Text = daysPlayed.ToString();
        }

        private void FillLastPlayedSection()
        {
            T1PROFI t1profiLastPlayed = DetermineLastPlayedProfile();

            if (t1profiLastPlayed == null)
                return;

            GetView().txtLastPlayedTitle.Text = t1profiLastPlayed.GANA;
            GetView().txtLastPlayedDate.Text = CFProfilesApp.FormatFirstLastDate(t1profiLastPlayed.LAPL);
            GetView().txtLastPlayedTime.Text = CFDashboardApp.FormatTime(TFPROFI.GetTotalGameTimeInMinutes(t1profiLastPlayed.PFID));

            string coverPath = Path.Combine(AppEnvironment.GetAppConfig().CoverFolderPath ?? string.Empty, t1profiLastPlayed.PPFN);
            var coverImage = FnImage.LoadImageWithoutLock(coverPath, 300, 450);

            GetView().imgLastPlayedCover.Source = coverImage;
        }

        private void FillPlaytimeOverviewSection()
        {
            double overallPlayTimeMinutes = GetTotalPlaytimeMinutes();

            GetView().txtTotalPlaytime.Text = CFDashboardApp.FormatTime(overallPlayTimeMinutes);

            int daysPlayed = TFSESSI.GetPlayedDays(0, timeSpanStart, timeSpanEnd);
            GetView().txtDaysPlayed.Text = daysPlayed.ToString();

            GetView().txtGamesPlayed.Text = GetTotalPlayedGames().ToString();
        }

        private void FillOverallStatisticsSection()
        {
            GetView().txtStatPlayedToday.Text = CFDashboardApp.FormatTime(GetTodaysPlaytime());

            T1SESSI t1sessi = GetLargestSession();

            if (t1sessi == null)
                return;

            GetView().txtStatLongestSession.Text = CFDashboardApp.FormatTime(t1sessi.PLTI);
            GetView().txtStatLongestSessionDate.Text = "(" + CFProfilesApp.FormatFirstLastDate(t1sessi.PLTO) + ")";
        }

        private List<T1PROFI> GetRecentlyPlayedProfiles()
        {
            UIXQuery query = BuildQueryRecentlyPlayedProfiles();

            TXPROFI txprofi = new TXPROFI();
            List<T1PROFI> profiles = new List<T1PROFI>();

            string s = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    T1PROFI t1profi = txprofi.Read(UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID));

                    profiles.Add(t1profi);
                }
            }

            return profiles;
        }

        private double GetTotalPlaytimeMinutes()
        {
            UIXQuery query = BuildQueryTotalPlaytimeMinutes();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    return UIXQuery.GetDouble(reader, "TotalPlaytime");
                }
            }

            return 0;
        }



        private int GetTotalPlayedGames()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddCount(K1PROFI.Name, K1PROFI.Fields.PFID, true, "TotalGames");

            UIXQueryTable t1sessi_table = query.AddJoinTable(K1SESSI.Name, JoinType.INNER);
            t1sessi_table.AddJoinCondition(
                K1PROFI.Name, K1PROFI.Fields.PFID,
                QueryCompareType.EQUALS,
                K1SESSI.Name, K1SESSI.Fields.PFID);

            if (timeSpanStart is not null)
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.LAPL, QueryCompareType.GREATER_OR_EQUAL, timeSpanStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.LAPL, QueryCompareType.LESS_OR_EQUAL, timeSpanEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.SetDistinct(true);

            string sqlQ = query.PreviewQuery();
            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    return UIXQuery.GetInt32(reader, "TotalGames");
                }
            }

            return 0;
        }

        private double GetTodaysPlaytime()
        {
            UIXQuery query = BuildQueryTodaysPlaytime();

            double playedMinutesToday = 0;

            string sql = query.PreviewQuery();
            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    DateTime plfr = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.PLFR);
                    DateTime plto = UIXQuery.GetDateTime(reader, K1SESSI.Name, K1SESSI.Fields.PLTO);
                    double plti = UIXQuery.GetDouble(reader, K1SESSI.Name, K1SESSI.Fields.PLTI);

                    // Wenn Von gleich gestern
                    // dann muss die differenz vom letzten Ende der Session zu 00:00 Uhr heute berechnet werden
                    if (plfr.Date == DateTime.Today.AddDays(-1))
                    {
                        playedMinutesToday += ((double)(plto - DateTime.Today).TotalSeconds / 60);
                    }
                    else
                    {
                        playedMinutesToday += plti;
                    }
                }
            }

            return playedMinutesToday;
        }

        private T1SESSI GetLargestSession()
        {
            UIXQuery query = BuildQueryLargestSessionTime();

            string s = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    TXSESSI txsessi = new TXSESSI();

                    long seid = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.SEID);

                    return txsessi.Read(seid);
                }
            }

            return null!;
        }

        private UIXQuery BuildQueryLargestSessionTime()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);

            if (timeSpanStart is not null)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.GREATER_OR_EQUAL, timeSpanStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.LESS_OR_EQUAL, timeSpanEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLTI, OrderDirection.DESC);

            query.SetTopX(1);

            return query;

        }

        private UIXQuery BuildQueryRecentlyPlayedProfiles()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);

            if (timeSpanStart is not null)
                query.AddWhere(K1PROFI.Name, K1PROFI.Fields.LAPL, QueryCompareType.GREATER_OR_EQUAL, timeSpanStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.LAPL, QueryCompareType.LESS_OR_EQUAL, timeSpanEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.LAPL, OrderDirection.DESC);

            return query;
        }

        private UIXQuery BuildQueryTodaysPlaytime()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLFR);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTO);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PLTI);

            query.AddWhereDateOnDay(K1SESSI.Name, K1SESSI.Fields.PLTO, DateTime.Today);

            return query;
        }

        private UIXQuery BuildQueryTotalPlaytimeMinutes()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            if (timeSpanStart is not null)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.GREATER_OR_EQUAL, timeSpanStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.LESS_OR_EQUAL, timeSpanEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            return query;
        }

        private UIXQuery BuildQueryMostPlayedGame()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            UIXQueryTable t1sessi = query.AddJoinTable(K1SESSI.Name, JoinType.LEFT);
            t1sessi.AddJoinCondition(
                K1PROFI.Name, K1PROFI.Fields.PFID,
                QueryCompareType.EQUALS,
                K1SESSI.Name, K1SESSI.Fields.PFID);

            if (timeSpanStart is not null)
                query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.GREATER_OR_EQUAL, timeSpanStart.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PLFR, QueryCompareType.LESS_OR_EQUAL, timeSpanEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            query.AddGroupBy(K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddOrderByAlias("TotalPlaytime", OrderDirection.DESC);

            query.SetTopX(1);

            return query;
        }

        private UIXQuery BuildQueryLastPlayedGame()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);

            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PLTO, OrderDirection.DESC);

            query.SetTopX(1);

            return query;
        }

        private List<ProfilesListBoxItem> BuildProfileListItems(List<T1PROFI> t1profiles)
        {
            return t1profiles.Select(prof => new ProfilesListBoxItem
            {
                ItemObject = prof,
                COCOVIM = FnImage.LoadImageWithoutLock(Path.Combine(AppEnvironment.GetAppConfig().CoverFolderPath ?? string.Empty, prof.PPFN), 300, 450),
                COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF)
            }).ToList();
        }

        private void ChangeTimeSpanDays()
        {
            int selectedIndex = GetView().CmbTimeRange.SelectedIndex;

            switch (selectedIndex)
            {
                case 0:
                    // Last 7 Days
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEndDays(7);
                    break;
                case 1:
                    // Last 30 Days
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEndDays(30);
                    break;
                case 2:
                    // Last 365 Days
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEndDays(365);
                    break;
                case 3:
                    // Week
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Week, offset: -1);
                    break;
                case 4:
                    // Month
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Month, offset: -1);
                    break;
                default:
                    // All time
                    timeSpanStart = null;
                    timeSpanEnd = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
            }
        }

        private T1PROFI DetermineMostPlayedProfile()
        {
            UIXQuery query = BuildQueryMostPlayedGame();
            TXPROFI txprofi = new TXPROFI();

            string s = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                if (!reader.Read())
                    return null!;

                long pfid = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID);
                return txprofi.Read(pfid);
            }
        }

        private T1PROFI DetermineLastPlayedProfile()
        {
            UIXQuery query = BuildQueryLastPlayedGame();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    long pfid = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.PFID);
                    TXPROFI txprofi = new TXPROFI();
                    return txprofi.Read(pfid);
                }
            }

            return null!;
        }

        protected void EV_BtnRefresh()
        {
            Open(true);
        }
    }
}
