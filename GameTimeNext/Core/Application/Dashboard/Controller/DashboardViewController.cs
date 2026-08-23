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
using System.Windows.Media.Imaging;
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
                COCOVIM = FnImage.LoadImageWithoutLock(Path.Combine(AppEnvironment.GetAppConfig().CoverFolderPath ?? string.Empty, prof.PPFN), 300, 450),
                COISPLA = FnSystem.IsExeFoundInPath(prof.EXGF),
                COCPLTI = CFDashboardApp.FormatTime(TFPROFI.GetGameTimeInMinutes(prof.PFID, timeSpanStart, timeSpanEnd))
            }).ToList();
        }

        private void FillSectionPlaytimeOverview()
        {
            GetView().txtTotalPlaytime.Text = CFDashboardApp.FormatTime(TFPROFI.GetPlaytime(timeSpanStart, timeSpanEnd));
            GetView().txtGamesPlayed.Text = TFPROFI.GetPlayedProfilesCount(timeSpanStart, timeSpanEnd).ToString();
            GetView().txtDaysPlayed.Text = TFPROFI.GetDaysPlayed(timeSpanStart, timeSpanEnd).ToString();
        }

        private void FillSectionOverallStatistics()
        {
            GetView().txtStatPlayedToday.Text = CFDashboardApp.FormatTime(TFPROFI.GetPlaytimeToday(timeSpanStart, timeSpanEnd));

            {
                // Longest Session
                (string gana, double plti, DateTime plto) = TFPROFI.GetLongestSession(timeSpanStart, timeSpanEnd);
                GetView().txtStatLongestSession.Text = CFDashboardApp.FormatTime(plti);
                GetView().txtStatLongestSessionGame.Text = gana;
                GetView().txtStatLongestSessionDate.Text = $"({CFProfilesApp.FormatFirstLastDate(plto)})";
            }
        }

        private void FillSectionLastPlayed()
        {
            (string gana, DateTime lapl, double plti, string ppfn) = TFPROFI.GetLastPlayed(timeSpanStart, timeSpanEnd);
            GetView().txtLastPlayedTitle.Text = gana;
            GetView().txtLastPlayedDate.Text = CFProfilesApp.FormatFirstLastDate(lapl);
            GetView().txtLastPlayedTime.Text = CFDashboardApp.FormatTime(plti);

            BitmapImage? coverImage = FnImage.LoadImageWithoutLock(ppfn, 300, 450);
            GetView().imgLastPlayedCover.Source = coverImage;
        }

        private void FillSectionMostPlayed()
        {
            (string gana, double plti, int days, string ppfn) = TFPROFI.GetMostPlayed(timeSpanStart, timeSpanEnd);
            GetView().txtMostPlayedTitle.Text = gana;
            GetView().txtMostPlayedPlaytime.Text = CFDashboardApp.FormatTime(plti);
            GetView().txtMostPlayedDays.Text = days.ToString();

            BitmapImage? coverImage = FnImage.LoadImageWithoutLock(ppfn, 300, 450);
            GetView().imgMostPlayedCover.Source = coverImage;
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
