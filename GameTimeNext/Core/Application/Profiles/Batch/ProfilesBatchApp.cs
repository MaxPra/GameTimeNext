using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Shared.Profiles;
using GameTimeNext.Shared.Profiles.Monitoring;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Batch
{
    public class ProfilesBatchApp : UIXApplication, IUIXBatchAppStarter
    {
        private TXPROFI _txprofi = new TXPROFI();

        public void Start(UIXApplication hostApplication)
        {
            HostApplication = hostApplication;
            CallDispatcher = hostApplication.CallDispatcher;
        }

        public override void InitializeApplicationOutput()
        {
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ProfilesBatchApp));
        }

        #region Methods PUBLIC
        public List<ProfileDto> GetProfiles()
        {
            List<ProfileDto> filteredT1profis = new List<ProfileDto>();

            UIXQuery query = new UIXQuery(K1PROFI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddWhere(K1PROFI.Name, K1PROFI.Fields.ARCH, QueryCompareType.NOT_EQUALS, true);

            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.LAPL, OrderDirection.DESC);
            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.PFID, OrderDirection.ASC);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long pfid = UIXQuery.GetInt64(reader, K1PROFI.Name, K1PROFI.Fields.PFID);
                    T1PROFI? t1profi = _txprofi.Read(pfid);
                    if (t1profi is null || !TFPROFI.IsExternalGame(t1profi)) continue;

                    filteredT1profis.Add(_txprofi.ToDto(t1profi));
                }
            }

            return filteredT1profis;
        }

        public ProfileDetailDto? GetProfile(long pfid)
        {
            T1PROFI? t1profi = _txprofi.Read(pfid);
            if (t1profi is null)
                return null;

            return _txprofi.ToDetailDto(t1profi);
        }

        public StartMonitoringResponseMessage StartMonitoring(long pfid, bool createPlaythrough)
        {
            return CallDispatcher.Invoke(() => ExecuteStartMonitoring(pfid, createPlaythrough));
        }

        public StopMonitoringResponseMessage StopMonitoring()
        {
            return CallDispatcher.Invoke(() => ExecuteStopMonitoring());
        }

        public MonitoringStateResponseMessage GetMonitoringState()
        {
            var state = CFGameTimeMonitoring.GetMonitoringState();

            if (!state.IsMonitoring)
                return new MonitoringStateResponseMessage(false, null, null);

            T1PROFI t1profi = _txprofi.Read(state.PFID)!;
            return new MonitoringStateResponseMessage(true, _txprofi.ToDto(t1profi), state.StartedAt);
        }
        #endregion

        #region Methods PRIVATE
        private StartMonitoringResponseMessage ExecuteStartMonitoring(long pfid, bool createPlaythrough)
        {
            if (!IsProfilesViewReady())
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.ProfilesAppNotReady);

            if (CFGameTimeMonitoring.IsMonitoring)
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.MonitoringAlreadyRunning);

            T1PROFI? t1profi = _txprofi.Read(pfid);
            if (t1profi is null)
                return new StartMonitoringResponseMessage(pfid, MonitoringResponse.ProfileNotFound);

            if (!TFPLTHR.HasCurrentPlaythrough(pfid))
            {
                if (!createPlaythrough)
                    return new StartMonitoringResponseMessage(pfid, MonitoringResponse.NoActivePlaythrough);

                long nextNumber = TFPLTHR.GetCurrentPlaythroughCount(pfid, PlaythroughType.NEW_PLAYTHROUGH) + 1;
                TFPLTHR.CreateNewPlaythrough(pfid, "Playthrough #" + nextNumber, PlaythroughType.NEW_PLAYTHROUGH);
            }

            CallDispatcher.Trigger("EXEV_SwitchToApplication", typeof(ProfilesApp).FullName!);

            if (!AppEnvironment.CurrentPfid.Equals(pfid))
            {
                AppEnvironment.CurrentPfid = pfid;
                CallDispatcher.Trigger("EXEV_SwitchProfile", pfid);
            }

            CFGameTimeMonitoring.StartMonitoring(pfid);
            CallDispatcher.Trigger("EXEV_GameTimeMonitoringStarted");

            return new StartMonitoringResponseMessage(pfid, MonitoringResponse.MonitoringStarted);
        }

        private StopMonitoringResponseMessage ExecuteStopMonitoring()
        {
            if (!IsProfilesViewReady())
                return new StopMonitoringResponseMessage(MonitoringResponse.ProfilesAppNotReady);

            if (!CFGameTimeMonitoring.IsMonitoring)
                return new StopMonitoringResponseMessage(MonitoringResponse.MonitoringNotRunningYet);

            CFGameTimeMonitoring.StopMonitoring();
            CallDispatcher.Trigger("EXEV_GameTimeMonitoringStopped");

            double minutes = CFGameTimeMonitoring.GetMonitoredTimeInMinutes();
            return new StopMonitoringResponseMessage(MonitoringResponse.MonitoringStopped, minutes, CFGameTimeMonitoring.IsSessionStored(minutes));
        }

        private bool IsProfilesViewReady()
        {
            string profilesApp = typeof(ProfilesApp).FullName!;

            return AppEnvironment.IsApplicationRunning(profilesApp)
                && AppEnvironment.IsApplicationInitialized(profilesApp)
                && CallDispatcher.IsRegistered("EXEV_SwitchToApplication")
                && CallDispatcher.IsRegistered("EXEV_SwitchProfile")
                && CallDispatcher.IsRegistered("EXEV_GameTimeMonitoringStarted")
                && CallDispatcher.IsRegistered("EXEV_GameTimeMonitoringStopped");
        }
        #endregion
    }
}
