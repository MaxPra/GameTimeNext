
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.DataWrapper;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.LauncherIntegration;
using System.Text;
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

        protected override void Build()
        {

        }

        protected override void BuildFirst()
        {
            GetView().btnLaunchGame.IsEnabled = _dataWrapper!.GetTypedTableObject().SAID != 0 && GetView().btnLaunchGame.Content != "Running..." && TFPROFI.HasExecutables(_dataWrapper.GetTypedTableObject());


            BuildCurrentPlaythroughPanel();
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void Check()
        {
        }

        protected override void FillViewImpl()
        {
            // Total game time
            FillTotalGameTime();

            // Heutige Spielzeit
            FillTodaysGameTime();

            // First und Lastplayed
            FillFirstLastDatePlayed();

            // Playthrough
            FillCurrentPlaythroughTime();
            FillCurrentPlaythroughName();
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
                    GetView().txTotalGameTime.Text = CFProfilesApp.FormatGameTimeHours(UIXQuery.GetDouble(reader, "TotalPlaytime"));
                }
            }
        }

        private void FillTodaysGameTime()
        {

            double todaysGameTimeMinutes = TFPROFI.GetTodaysGameTimeInMinutes(_dataWrapper!.GetTypedTableObject().PFID);

            StringBuilder sb = new StringBuilder();

            sb.Append(CFProfilesApp.FormatGameTimeHours(todaysGameTimeMinutes));
            sb.Append(" ");
            sb.Append("(").Append(CFProfilesApp.FormatGameTimeMinutes(todaysGameTimeMinutes)).Append(")");


            GetView().txPlayedToday.Text = sb.ToString();
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

            string sql = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    GetView().txPlaythroughTime.Text = CFProfilesApp.FormatGameTimeHours(UIXQuery.GetDouble(reader, "TotalPlaytimePlaythrough"));
                }
            }
        }

        private void FillCurrentPlaythroughName()
        {
            T1PLTHR t1plthr = TFPLTHR.GetCurrentPlaythrough(_dataWrapper!.GetTypedTableObject().PFID);

            if (t1plthr == null)
                return;

            GetView().txPlaythrough.Text = t1plthr.PTDE + ":";
        }

        private UIXQuery BuildFirstLastDatePlayedQuery()
        {
            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Felder
            query.AddField(K1PROFI.Name, K1PROFI.Fields.FIPL);
            query.AddField(K1PROFI.Name, K1PROFI.Fields.LAPL);

            // Where Restriktionen
            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.GetTypedTableObject().PFID);

            return query;
        }

        public UIXQuery BuildCurrentPlaythroughTimeQuery()
        {

            long pfid = _dataWrapper!.GetTypedTableObject()!.PFID;

            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Felder
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

            // Felder hinzufügen
            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            // Where Restriktionen
            query.AddWhere(K1SESSI.Name, K1SESSI.Fields.PFID, QueryCompareType.EQUALS, _dataWrapper!.GetTypedTableObject().PFID);

            return query;
        }

        private void BuildCurrentPlaythroughPanel()
        {
            // Sichtbarkeitssteuerung Playthrough-Box
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
            CFSteamGameStarter.StartSteamGame(_dataWrapper!.GetTypedTableObject().SAID.ToString(), _dataWrapper!.GetTypedTableObject().PFID, GetApp());
            GetView().btnLaunchGame.Content = "Launching...";
            GetView().btnLaunchGame.IsEnabled = false;

            ProfilesViewController? profilesViewController = _dataWrapper.SourceController as ProfilesViewController;
            ((ProfilesView)profilesViewController!.View).LaunchingOverlay.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                // 15 Sekunden warten
                Thread.Sleep(15000);

                // Sollte das Spiel in diesen 15 Sekunden nicht gestartet worden sein --> zurücksetzen des Buttons
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
