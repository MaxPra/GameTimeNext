using System.Globalization;
using GameTimeNext.Core.Application.Dashboard.ViewModels;
using GameTimeNext.Core.Application.Dashboard.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Dashboard.Controller
{
    public class DashboardViewController : UIXViewControllerBase<DashboardApp, DashboardView>
    {
        DashboardViewModel? viewModel = null;
        short timeSpanOffset = 0;
        DateTime? timeSpanStart = null;
        DateTime timeSpanEnd = DateTime.Today.AddDays(1).AddTicks(-1);
        bool showOffsetSelection = false;
        // Playtime of the whole period in minutes (playtime overview), the base of the most played share
        double totalPlaytime = 0;

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

            // Suppress pipeline
            using (SuppressRunEventPipeline())
            {
                FillComboboxTimeRange();

                GetView().CmbTimeRange.SelectedIndex =AppEnvironment.GetAppConfig().UserSettings.SelectedDashboardMode;
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
            FnControls.SetVisible(GetView().pnlTimeSpanOffset, showOffsetSelection);
            FnControls.SetEnabled(GetView().BtnIncreaseTimeSpan, timeSpanOffset < 0);
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
            GetView().txtTimeSpanRange.Text = FormatTimeSpan();

            FillSectionPlaytimeOverview();
            FillSectionDayBars();
            FillSectionOverallStatistics();
            FillSectionLastPlayed();
            FillSectionMostPlayed();
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

        /// <summary>
        /// Periods of the dashboard; the selection is evaluated and stored by index, so the order must not change.
        /// </summary>
        private void FillComboboxTimeRange()
        {
            UIXManualCodetable codetable = new UIXManualCodetable();
            codetable.AddEntry("LAST7", "Last 7 Days");
            codetable.AddEntry("LAST30", "Last 30 Days");
            codetable.AddEntry("LAST365", "Last 365 Days");
            codetable.AddEntry("DAY", "Day");
            codetable.AddEntry("WEEK", "Week");
            codetable.AddEntry("MONTH", "Month");
            codetable.AddEntry("ALL", "All Time");
            codetable.ApplyTo(GetView().CmbTimeRange);
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
                    // Day
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Day, offset: timeSpanOffset);
                    showOffsetSelection = true;
                    break;
                case 4:
                    // Week
                    (timeSpanStart, timeSpanEnd) = FnTimeSpan.GetBeginningAndEnd(FnTimeSpan.TimeSpanType.Week, offset: timeSpanOffset);
                    showOffsetSelection = true;
                    break;
                case 5:
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

        /// <summary>
        /// The shown period in words: the day, the month, a date range or "All time".
        /// </summary>
        private string FormatTimeSpan()
        {
            if (timeSpanStart is not DateTime start)
                return "All time";

            DateTime end = timeSpanEnd.Date;

            switch (GetView().CmbTimeRange.SelectedIndex)
            {
                case 3:
                    // Day
                    return start.ToString("D", CultureInfo.CurrentCulture);
                case 5:
                    // Month
                    return start.ToString("Y", CultureInfo.CurrentCulture);
            }

            if (start.Date == end)
                return start.ToString("d", CultureInfo.CurrentCulture);

            return $"{start.ToString("d", CultureInfo.CurrentCulture)} – {end.ToString("d", CultureInfo.CurrentCulture)}";
        }

        private async Task BuildPlayedProfilesListBoxAsync()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<T1PROFI> t1profis = TFPROFI.GetPlayedProfiles(timeSpanStart, timeSpanEnd);
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
                COCOVIM = CFProfilesApp.LoadProfileCover(prof.PPFN),
                COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF),
                COCPLTI = FnTime.FormatMinutesAsHoursAndMinutes(TFPROFI.GetGameTimeInMinutes(prof.PFID, timeSpanStart, timeSpanEnd))
            }).ToList();
        }

        private void FillSectionPlaytimeOverview()
        {
            totalPlaytime = TFPROFI.GetPlaytime(timeSpanStart, timeSpanEnd);
            int games = TFPROFI.GetPlayedProfilesCount(timeSpanStart, timeSpanEnd);
            int days = TFPROFI.GetDaysPlayed(timeSpanStart, timeSpanEnd);

            GetView().txtTotalPlaytime.Text = FnTime.FormatMinutesAsHoursAndMinutes(totalPlaytime);
            GetView().txtDaysPlayed.Text = FormatCount(days, "day played", "days played");
            GetView().txtGamesPlayed.Text = FormatCount(games, "game", "games");
            GetView().txtAveragePerDay.Text = $"Ø {FnTime.FormatMinutesAsHoursAndMinutes(days > 0 ? totalPlaytime / days : 0, FnTime.NoValue)} per day";
        }

        /// <summary>
        /// Playtime of the last 7 days up to the end of the period, as bars relative to the peak day.
        /// </summary>
        private void FillSectionDayBars()
        {
            DateTime lastDay = timeSpanEnd.Date;
            List<DashboardDayBar> bars = new List<DashboardDayBar>(7);

            double[] minutes = new double[7];
            for (int i = 0; i < 7; i++)
            {
                DateTime day = lastDay.AddDays(i - 6);
                minutes[i] = TFPROFI.GetPlaytime(day, day.AddDays(1).AddTicks(-1));
            }

            double peak = minutes.Max();
            int peakIndex = peak > 0 ? Array.IndexOf(minutes, peak) : -1;

            for (int i = 0; i < 7; i++)
            {
                DateTime day = lastDay.AddDays(i - 6);
                double ratio = peak > 0 ? minutes[i] / peak : 0;
                string time = FnTime.FormatMinutesAsHoursAndMinutes(minutes[i]);

                bars.Add(new DashboardDayBar
                {
                    Label = day.ToString("ddd", CultureInfo.CurrentCulture),
                    ValueText = time,
                    ToolTipText = $"{day.ToString("ddd", CultureInfo.CurrentCulture)}, {day.ToString("d", CultureInfo.CurrentCulture)}: {time}",
                    BarHeight = new GridLength(ratio, GridUnitType.Star),
                    RestHeight = new GridLength(1 - ratio, GridUnitType.Star),
                    IsPeak = i == peakIndex,
                    IsToday = day == DateTime.Today
                });
            }

            GetView().icDayBars.ItemsSource = bars;
            GetView().txtDayBarsCaption.Text = lastDay == DateTime.Today
                ? "Last 7 days"
                : $"7 days up to {lastDay.ToString("d", CultureInfo.CurrentCulture)}";
        }

        private static string FormatCount(int count, string singular, string plural)
        {
            return $"{count.ToString(CultureInfo.CurrentCulture)} {(count == 1 ? singular : plural)}";
        }

        private void FillSectionOverallStatistics()
        {
            GetView().txtStatPlayedToday.Text = FnTime.FormatMinutesAsHoursAndMinutes(TFPROFI.GetPlaytimeToday(timeSpanStart, timeSpanEnd));

            // Fixed periods, independent of the selected time span
            FillPeriodTile(FnTimeSpan.TimeSpanType.Week, GetView().txtStatThisWeek, GetView().txtStatThisWeekDays);
            FillPeriodTile(FnTimeSpan.TimeSpanType.Month, GetView().txtStatThisMonth, GetView().txtStatThisMonthDays);
            FillPeriodTile(FnTimeSpan.TimeSpanType.Year, GetView().txtStatThisYear, GetView().txtStatThisYearDays);

            {
                // Longest Session
                (string gana, double plti, DateTime plto) = TFPROFI.GetLongestSession(timeSpanStart, timeSpanEnd);
                GetView().txtStatLongestSession.Text = FnTime.FormatMinutesAsHoursAndMinutes(plti, FnTime.NoValue);
                GetView().txtStatLongestSessionGame.Text = gana;
                GetView().txtStatLongestSessionDate.Text = $"({CFProfilesApp.FormatFirstLastDate(plto)})";
            }
        }

        private static void FillPeriodTile(FnTimeSpan.TimeSpanType type, TextBlock txtPlaytime, TextBlock txtDays)
        {
            (DateTime start, DateTime end) = FnTimeSpan.GetBeginningAndEnd(type);
            txtPlaytime.Text = FnTime.FormatMinutesAsHoursAndMinutes(TFPROFI.GetPlaytime(start, end));
            txtDays.Text = FormatCount(TFPROFI.GetDaysPlayed(start, end), "day played", "days played");
        }

        private void FillSectionLastPlayed()
        {
            (string gana, DateTime lapl, double plti, string ppfn) = TFPROFI.GetLastPlayed(timeSpanStart, timeSpanEnd);
            GetView().txtLastPlayedTitle.Text = gana;
            GetView().txtLastPlayedDate.Text = CFProfilesApp.FormatFirstLastDate(lapl);
            GetView().txtLastPlayedTime.Text = FnTime.FormatMinutesAsHoursAndMinutes(plti, FnTime.NoValue);

            // No cover without a played game in the time span
            GetView().ccLastPlayedCase.Content = FnString.IsNullEmptyOrWhitespace(ppfn) ? null : CFProfilesApp.LoadProfileCover(ppfn);
        }

        private void FillSectionMostPlayed()
        {
            (string gana, double plti, int days, string ppfn) = TFPROFI.GetMostPlayed(timeSpanStart, timeSpanEnd);
            GetView().txtMostPlayedTitle.Text = gana;
            GetView().txtMostPlayedPlaytime.Text = FnTime.FormatMinutesAsHoursAndMinutes(plti, FnTime.NoValue);
            GetView().txtMostPlayedDays.Text = days.ToString(CultureInfo.CurrentCulture);
            GetView().txtMostPlayedAverage.Text = FnTime.FormatMinutesAsHoursAndMinutes(days > 0 ? plti / days : 0, FnTime.NoValue);

            // Share of the period's playtime (FillSectionPlaytimeOverview runs before and sets totalPlaytime)
            double share = totalPlaytime > 0 ? Math.Clamp(plti / totalPlaytime, 0, 1) : 0;
            GetView().txtMostPlayedShare.Text = totalPlaytime > 0 ? share.ToString("P0", CultureInfo.CurrentCulture) : FnTime.NoValue;
            GetView().scMostPlayedShare.ScaleX = share;

            // No cover without a played game in the time span
            GetView().ccMostPlayedCase.Content = FnString.IsNullEmptyOrWhitespace(ppfn) ? null : CFProfilesApp.LoadProfileCover(ppfn);
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
