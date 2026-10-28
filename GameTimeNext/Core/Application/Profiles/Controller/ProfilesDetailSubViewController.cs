
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    internal class ProfilesDetailSubViewController : UIXViewControllerBase
    {

        private ProfilesSubViewDataWrapper? _dataWrapper;
        private ProfilesViewController? _profilesViewController;

        private bool _gameRunning = false;
        private bool _gameLaunching = false;

        public ProfilesDetailSubViewController(UIXApplication app) : base(app)
        {
        }

        public void UpdateUIMonitoringStarted()
        {
            GetView().btnStartMonitoring.Content = "Stop monitoring";
        }

        public void UpdateUIMonitoringStopped()
        {
            GetView().btnStartMonitoring.Content = "Start monitoring";
        }

        public void UpdateUIGameRunning()
        {
            GetView().btnLaunchGame.Content = "Quit game";
            GetView().btnLaunchGame.IsEnabled = true;

            // Overlay stays visible (set by ProfilesViewController), but without the launch card
            GetLaunchingOverlay().Content = null;
            GetLaunchingOverlay().Tag = null;

            _gameRunning = true;
            _gameLaunching = false;
        }

        public void UpdateUIGameClosed()
        {
            UpdateUIIdle();
            GetLaunchingOverlay().Content = null;
            GetLaunchingOverlay().Tag = null;
            GetLaunchingOverlay().Visibility = Visibility.Collapsed;
            _gameRunning = false;
            _gameLaunching = false;
        }

        public void UpdateUIUnwrapped()
        {
            if (!_gameRunning && !_gameLaunching)
                UpdateUIIdle();
        }

        /// <summary>
        /// Neither launching nor running: a sealed game has to be unwrapped first (always possible), otherwise it can be launched
        /// </summary>
        private void UpdateUIIdle()
        {
            T1PROFI t1profi = _dataWrapper!.Get();
            bool isSealed = TFPROFI.IsSealed(t1profi);

            GetView().btnLaunchGame.Content = isSealed ? "Unwrap game" : "Launch game";
            GetView().btnLaunchGame.IsEnabled = isSealed || FnLauncher.CanLaunch(t1profi);
        }

        private void UpdateUIGameLaunching(T1PROFI t1profi)
        {
            GetView().btnLaunchGame.Content = "Cancel launch";
            GetView().btnLaunchGame.IsEnabled = true;

            // The game name shows the launch card with its animation
            GetLaunchingOverlay().Content = t1profi.GANA;
            GetLaunchingOverlay().Tag = null;
            GetLaunchingOverlay().Visibility = Visibility.Visible;

            _gameLaunching = true;
        }

        private void UpdateUIGameQuitting(T1PROFI t1profi)
        {
            GetView().btnLaunchGame.Content = "Quitting...";
            GetView().btnLaunchGame.IsEnabled = false;

            // Same card as for launching, in its quitting variant; the running game detection closes it (UpdateUIGameClosed)
            GetLaunchingOverlay().Content = t1profi.GANA;
            GetLaunchingOverlay().Tag = "Quitting";
            GetLaunchingOverlay().Visibility = Visibility.Visible;
        }

        private ContentControl GetLaunchingOverlay()
        {
            return ((ProfilesView)_profilesViewController!.View).LaunchingOverlay;
        }

        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();
            _profilesViewController = _dataWrapper.SourceController as ProfilesViewController;

            if (_dataWrapper == null)
                return;

            AddIdentifier("T1PROFI", _dataWrapper.Get());
        }

        protected override void BuildImpl()
        {

        }

        protected override void BuildFirstImpl()
        {
            ResetView();

            GetView().imgProfileCover.Source = CFProfilesApp.LoadProfileCover(_dataWrapper!.Get().PPFN);

            if (_gameRunning || _gameLaunching)
                GetView().btnLaunchGame.IsEnabled = true;
            else
                UpdateUIIdle();

            // -- Progress bar section
            //bool isIGDBLinkedCorrectly = !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientID) &&
            //    !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientSecret);

            FnControls.SetVisible(GetView().pnlProgressSection, _dataWrapper!.Get().ETTY != EstimatedTimeTypes.EST_TIME_NONE && TFPLTHR.HasCurrentPlaythrough(_dataWrapper!.Get().PFID));

            BuildProgressBarPanel();

            BuildCurrentPlaythroughPanel();
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
            // Total game time
            FillTotalGameTime();

            // Today's playtime
            FillTodaysGameTime();

            // Last session
            FillLastSessionGameTime();

            // First and last played
            FillFirstLastDatePlayed();

            // Playthrough
            FillCurrentPlaythroughTime();
            FillCurrentPlaythroughName();

            // Progressbar
            FillProgressbarAndTimes();

            // Rating
            FillProfileRating();
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }
        private void FillTotalGameTime()
        {
            UIXQuery query = BuildGameTimeQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    GetView().txTotalGameTime.Text = FnTime.FormatMinutesAsHoursAndMinutes(UIXQuery.GetDouble(reader, "TotalPlaytime"), FnTime.NoValue);
                }
            }
        }

        private void FillTodaysGameTime()
        {

            double todaysGameTimeMinutes = TFPROFI.GetTodaysGameTimeInMinutes(_dataWrapper!.Get().PFID);

            GetView().txPlayedToday.Text = FnTime.FormatMinutesAsHoursAndMinutes(todaysGameTimeMinutes);
        }

        private void FillFirstLastDatePlayed()
        {
            UIXQuery query = BuildFirstLastDatePlayedQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    GetView().txFirstTimePlayed.Text = CFProfilesApp.FormatFirstLastDate(UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.FIPL));
                    GetView().txLastTimePlayed.Text = CFProfilesApp.FormatFirstLastDate(UIXQuery.GetDateTime(reader, K1PROFI.Name, K1PROFI.Fields.LAPL));
                }
            }
        }

        private void FillCurrentPlaythroughTime()
        {
            UIXQuery query = BuildCurrentPlaythroughTimeQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    GetView().txPlaythroughTime.Text = FnTime.FormatMinutesAsHoursAndMinutes(UIXQuery.GetDouble(reader, "TotalPlaytimePlaythrough"), FnTime.NoValue);
                }
            }
        }

        private void FillCurrentPlaythroughName()
        {
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(_dataWrapper!.Get().PFID);

            if (t1plthr == null)
                return;

            GetView().txPlaythrough.Text = t1plthr.PTDE;
        }

        private void FillProfileRating()
        {
            double rating = TFPLTHR.GetProfileRating(_dataWrapper!.Get().PFID);

            FnControls.SetVisible(GetView().pnlRating, rating > 0);

            GetView().rtgProfileRating.Value = rating;
            GetView().txProfileRating.Text = CFPlaythroughsApp.FormatRatingAsStars(rating);
        }

        private void FillLastSessionGameTime()
        {

            double lastSessionTime = TFSESSI.GetLastSessionGameTime(_dataWrapper!.Get().PFID);

            string text = FnTime.FormatMinutesAsHoursAndMinutes(lastSessionTime, FnTime.NoValue);

            GetView().txLastSession.Text = text;
        }

        private void FillProgressbarAndTimes()
        {
            T1PROFI t1profi = _dataWrapper!.Get();

            switch (t1profi.ETTY)
            {
                case EstimatedTimeTypes.EST_TIME_MAIN:
                    FillProgressbarAndTimesMain(t1profi);
                    break;

                case EstimatedTimeTypes.EST_TIME_MAIN_EXTRA:
                    FillProgressbarAndTimesMainExtra(t1profi);
                    break;

                case EstimatedTimeTypes.EST_TIME_COMPLETIONIST:
                    FillProgressbarAndTimesCompletionist(t1profi);
                    break;
            }

            GetView().txEstMain.Text = FnTime.FormatMinutesAsHoursAndMinutes(t1profi.ETMA, FnTime.NoValue);
            GetView().txEstExtra.Text = FnTime.FormatMinutesAsHoursAndMinutes(t1profi.ETME, FnTime.NoValue);
            GetView().txEstCompletionist.Text = FnTime.FormatMinutesAsHoursAndMinutes(t1profi.ETCO, FnTime.NoValue);
        }

        private void FillProgressbarAndTimesCompletionist(T1PROFI t1profi)
        {
            double totalGameTimeMinutes = 0;

            UIXQuery query = BuildCurrentPlaythroughTimeQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    totalGameTimeMinutes = UIXQuery.GetDouble(reader, "TotalPlaytimePlaythrough");
                }
            }

            if (totalGameTimeMinutes == 0)
                return;

            double percent = (totalGameTimeMinutes / t1profi.ETCO) * 100.0;

            SetProgress(percent);
        }

        private void FillProgressbarAndTimesMainExtra(T1PROFI t1profi)
        {
            double totalGameTimeMinutes = 0;

            UIXQuery query = BuildCurrentPlaythroughTimeQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    totalGameTimeMinutes = UIXQuery.GetDouble(reader, "TotalPlaytimePlaythrough");
                }
            }

            if (totalGameTimeMinutes == 0)
                return;

            double percent = (totalGameTimeMinutes / t1profi.ETME) * 100.0;

            SetProgress(percent);
        }

        private void FillProgressbarAndTimesMain(T1PROFI t1profi)
        {
            double totalGameTimeMinutes = 0;

            UIXQuery query = BuildCurrentPlaythroughTimeQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    totalGameTimeMinutes = UIXQuery.GetDouble(reader, "TotalPlaytimePlaythrough");
                }
            }

            if (totalGameTimeMinutes == 0)
                return;

            double percent = (totalGameTimeMinutes / t1profi.ETMA) * 100.0;

            SetProgress(percent);
        }

        private UIXQuery BuildFirstLastDatePlayedQuery()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Fields
            query.AddField(K1PROFI.Name, K1PROFI.Fields.FIPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LAPL);

            // Where restrictions
            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.Get().PFID);

            return query;
        }

        public UIXQuery BuildCurrentPlaythroughTimeQuery()
        {

            long pfid = _dataWrapper!.Get().PFID;

            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Fields
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytimePlaythrough");

            // Join
            UIXQueryTable t1sessi_table = query.AddJoinTable(K1SESSI.Name, JoinType.INNER);
            t1sessi_table.AddJoinCondition(K1PLTHR.Name, K1PLTHR.Fields.PTID, QueryCompareType.EQUALS, K1SESSI.Name, K1SESSI.Fields.PTID);

            // Where
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, pfid);
            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTID, QueryCompareType.EQUALS, TFPLTHR.GetCurrentPlaythroughPtid(pfid));

            return query;
        }

        private UIXQuery BuildGameTimeQuery()
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Add fields
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            // Where restrictions
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.Get().PFID);

            return query;
        }

        private void BuildCurrentPlaythroughPanel()
        {
            // Visibility of the playthrough box
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(_dataWrapper!.Get().PFID);

            if (t1plthr != null && TFPLTHR.HasCurrentPlaythrough(_dataWrapper!.Get().PFID) && t1plthr.PTTY != PlaythroughType.INITIAL_PLAYTHROUGH)
            {
                FnControls.SetVisible(GetView().pnlPlaythrough, true);
            }
            else
            {
                FnControls.SetVisible(GetView().pnlPlaythrough, false);
            }
        }

        private void BuildProgressBarPanel()
        {
            T1PROFI t1profi = _dataWrapper!.Get();

            switch (t1profi.ETTY)
            {
                case EstimatedTimeTypes.EST_TIME_MAIN:
                    SetEstimateHighlighted(GetView().lblEstMain, GetView().txEstMain, true);
                    break;

                case EstimatedTimeTypes.EST_TIME_MAIN_EXTRA:
                    SetEstimateHighlighted(GetView().lblEstExtra, GetView().txEstExtra, true);
                    break;

                case EstimatedTimeTypes.EST_TIME_COMPLETIONIST:
                    SetEstimateHighlighted(GetView().lblEstCompletionist, GetView().txEstCompletionist, true);
                    break;
            }
        }

        /// <summary>
        /// Marks the estimate the progress is based on (labels are muted by default).
        /// </summary>
        private static void SetEstimateHighlighted(System.Windows.Controls.TextBlock label, System.Windows.Controls.TextBlock value, bool isHighlighted)
        {
            FontWeight fontWeight = isHighlighted ? FontWeights.Bold : FontWeights.Normal;

            label.FontWeight = fontWeight;
            value.FontWeight = fontWeight;
            label.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, isHighlighted ? "Brush.TextPrimary" : "Brush.TextSecondary");
        }

        public void SetProgress(double percent)
        {
            percent = Math.Clamp(percent, 0, 100);

            GetView().txtProgress.Text = $"~ {percent:0}%";

            // Relative scale instead of a fixed width, so the fill follows the card when the detail view resizes
            GetView().pnlProgress.RenderTransform = new System.Windows.Media.ScaleTransform(percent / 100.0, 1);
        }

        private void ResetView()
        {
            // Reset styles first
            SetEstimateHighlighted(GetView().lblEstMain, GetView().txEstMain, false);
            SetEstimateHighlighted(GetView().lblEstExtra, GetView().txEstExtra, false);
            SetEstimateHighlighted(GetView().lblEstCompletionist, GetView().txEstCompletionist, false);

            SetProgress(0);
        }

        private ProfilesDetailView GetView()
        {
            return (ProfilesDetailView)this.View;
        }

        private ProfilesApp GetApp()
        {
            return (ProfilesApp)this.App;
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {

        }

        /// <summary>
        /// Unwrap game / Launch game / Cancel launch / Quit game
        /// </summary>
        public async void EV_btnLaunchGame()
        {
            T1PROFI t1profi = _dataWrapper!.Get();

            if (!_gameRunning && !_gameLaunching && TFPROFI.IsSealed(t1profi))
            {
                _profilesViewController!.UnwrapSelectedProfile();
                return;
            }

            if (_gameRunning)
            {
                QuitGame(t1profi);
                return;
            }

            if (_gameLaunching)
            {
                FnLauncher.CancelLaunch(t1profi);
                UpdateUIGameClosed();
                return;
            }

            UpdateUIGameLaunching(t1profi);

            // Not installed or cancelled meanwhile; otherwise the running game detection switches to "Quit game"
            if (!await FnLauncher.StartGameAsync(t1profi, GetApp()) && _gameLaunching)
                UpdateUIGameClosed();
        }

        private void QuitGame(T1PROFI t1profi)
        {
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
            CFMBOXResult result = cfmbox.Show(GetErrorMessage("PRO0008", t1profi.GANA), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result != CFMBOXResult.Yes)
                return;

            UpdateUIGameQuitting(t1profi);

            // The game keeps running (e.g. elevated): back to "Quit game"
            if (!FnLauncher.QuitGame(t1profi))
                UpdateUIGameRunning();
        }

        protected void EV_btnStartMonitoring()
        {

            if (!CFProfilesApp.AskForNewPlaythroughCreationIfNotActive(_dataWrapper!.Get().PFID, GetApp()))
                return;

            if (CFGameTimeMonitoring.IsMonitoring)
            {
                CFGameTimeMonitoring.StopMonitoring();
                GetApp().CallDispatcher.Trigger("EXEV_GameTimeMonitoringStopped");
            }
            else
            {
                CFGameTimeMonitoring.StartMonitoring(AppEnvironment.CurrentPfid);
                GetApp().CallDispatcher.Trigger("EXEV_GameTimeMonitoringStarted");
            }
        }
    }
}
