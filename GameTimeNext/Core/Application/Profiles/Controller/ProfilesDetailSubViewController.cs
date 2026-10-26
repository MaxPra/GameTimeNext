
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
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.IO;
using System.Windows;
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
            GetView().btnLaunchGame.Content = "Running...";
            GetView().btnLaunchGame.IsEnabled = false;
            _gameRunning = true;
        }

        public void UpdateUIGameClosed()
        {
            GetView().btnLaunchGame.Content = "Launch game";
            GetView().btnLaunchGame.IsEnabled = true;
            ((ProfilesView)_profilesViewController!.View).LaunchingOverlay.Visibility = Visibility.Collapsed;
            _gameRunning = false;
        }

        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ProfilesSubViewDataWrapper>();
            _profilesViewController = _dataWrapper.SourceController as ProfilesViewController;

            if (_dataWrapper == null)
                return;

            AddIdentifier("T1PROFI", _dataWrapper.GetTypedTableObject());
        }

        protected override void BuildImpl()
        {

        }

        protected override void BuildFirstImpl()
        {
            ResetView();

            GetView().imgProfileCover.Source = FnImage.LoadImageWithoutLock(Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath, _dataWrapper!.GetTypedTableObject().PPFN), 300, 450);

            GetView().btnLaunchGame.IsEnabled = _dataWrapper!.GetTypedTableObject().SAID != 0 && GetView().btnLaunchGame.Content != "Running..." && TFPROFI.HasExecutables(_dataWrapper.GetTypedTableObject());

            // -- Progress bar section
            //bool isIGDBLinkedCorrectly = !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientID) &&
            //    !FnString.IsNullEmptyOrWhitespace(AppEnvironment.GetAppConfig().AppSettings.TwitchIGDBClientSecret);

            FnControls.SetVisible(GetView().pnlProgressSection, _dataWrapper!.GetTypedTableObject().ETTY != EstimatedTimeTypes.EST_TIME_NONE && TFPLTHR.HasCurrentPlaythrough(_dataWrapper!.GetTypedTableObject().PFID));

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

            double todaysGameTimeMinutes = TFPROFI.GetTodaysGameTimeInMinutes(_dataWrapper!.GetTypedTableObject().PFID);

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
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(_dataWrapper!.GetTypedTableObject().PFID);

            if (t1plthr == null)
                return;

            GetView().txPlaythrough.Text = t1plthr.PTDE;
        }

        private void FillProfileRating()
        {
            double rating = TFPLTHR.GetProfileRating(_dataWrapper!.GetTypedTableObject().PFID);

            FnControls.SetVisible(GetView().pnlRating, rating > 0);

            GetView().rtgProfileRating.Value = rating;
            GetView().txProfileRating.Text = CFPlaythroughsApp.FormatRatingAsStars(rating);
        }

        private void FillLastSessionGameTime()
        {

            double lastSessionTime = TFSESSI.GetLastSessionGameTime(_dataWrapper!.GetTypedTableObject().PFID);

            string text = FnTime.FormatMinutesAsHoursAndMinutes(lastSessionTime, FnTime.NoValue);

            GetView().txLastSession.Text = text;
        }

        private void FillProgressbarAndTimes()
        {
            T1PROFI t1profi = _dataWrapper!.GetTypedTableObject();

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
            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.GetTypedTableObject().PFID);

            return query;
        }

        public UIXQuery BuildCurrentPlaythroughTimeQuery()
        {

            long pfid = _dataWrapper!.GetTypedTableObject()!.PFID;

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
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.GetTypedTableObject().PFID);

            return query;
        }

        private void BuildCurrentPlaythroughPanel()
        {
            // Visibility of the playthrough box
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(_dataWrapper!.GetTypedTableObject().PFID);

            if (t1plthr != null && TFPLTHR.HasCurrentPlaythrough(_dataWrapper!.GetTypedTableObject().PFID) && t1plthr.PTTY != PlaythroughType.INITIAL_PLAYTHROUGH)
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
            T1PROFI t1profi = _dataWrapper!.GetTypedTableObject();

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

        public async void EV_btnLaunchGame()
        {
            FnSteam.StartSteamGame(_dataWrapper!.GetTypedTableObject().SAID.ToString(CultureInfo.InvariantCulture), _dataWrapper!.GetTypedTableObject().PFID, GetApp());
            GetView().btnLaunchGame.Content = "Launching...";
            GetView().btnLaunchGame.IsEnabled = false;

            ProfilesViewController? profilesViewController = _dataWrapper.SourceController as ProfilesViewController;
            ((ProfilesView)profilesViewController!.View).LaunchingOverlay.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                // Wait 30 seconds
                Thread.Sleep(30000);

                // If the game has not started within these 30 seconds --> reset the button
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!_gameRunning)
                    {
                        UpdateUIGameClosed();
                    }
                });
            });
        }

        protected void EV_btnStartMonitoring()
        {

            if (!CFProfilesApp.AskForNewPlaythroughCreationIfNotActive(_dataWrapper!.GetTypedTableObject().PFID, GetApp()))
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
