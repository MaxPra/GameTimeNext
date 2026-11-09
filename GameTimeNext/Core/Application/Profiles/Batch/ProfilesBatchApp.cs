using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Shared.Profiles;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Batch
{
    public class ProfilesBatchApp : UIXApplication, IUIXBatchAppStarter
    {
        private TXPROFI _txprofi = new TXPROFI();

        public void Start(UIXApplication hostApplication)
        {
            HostApplication = hostApplication;
            // CallDispatcher = hostApplication.CallDispatcher; // OFDOI: CallDispatcher
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

        public List<ProfileDto> GetAllProfiles()
        {
            return _txprofi.ReadAll().Select(t => _txprofi.ToDto(t)).ToList();
        }

        public ProfileDto? GetProfile(long pfid)
        {
            T1PROFI? t1profi = _txprofi.Read(pfid);
            if (t1profi is null)
                return null;

            return _txprofi.ToDto(t1profi);
        }
    }

    // OFDOI: Remove old BatchApp
    [Obsolete]
    public class PrrofilesBatchApp : UIXApplication
    {

        public void StartMonitoringBatch(long pfid)
        {
            if (!AppEnvironment.IsApplicationRunning(typeof(ProfilesApp).FullName!) || !AppEnvironment.IsApplicationInitialized(typeof(ProfilesApp).FullName!))
                return;

            if (!SwitchToProfilesAppBatch())
                return;

            SwitchProfileBatch(pfid);

            // Neuen Playthrough erstellen, wenn noch keiner existiert
            if (TFPLTHR.GetCurrentPlaythrough(pfid) is null)
            {
                long nextNumber = TFPLTHR.GetCurrentPlaythroughCount(pfid, PlaythroughType.NEW_PLAYTHROUGH) + 1;
                TFPLTHR.CreateNewPlaythrough(pfid, "Playthrough #" + nextNumber, PlaythroughType.NEW_PLAYTHROUGH);
            }

            CFGameTimeMonitoring.StartMonitoring(AppEnvironment.CurrentPfid);
            CallDispatcher!.Trigger("EXEV_GameTimeMonitoringStarted");
        }

        public void StopMonitoringBatch()
        {
            CFGameTimeMonitoring.StopMonitoring();
            CallDispatcher!.Trigger("EXEV_GameTimeMonitoringStopped");
        }

        public bool SwitchToProfilesAppBatch()
        {
            if (!AppEnvironment.IsApplicationRunning(typeof(ProfilesApp).FullName!) || !AppEnvironment.IsApplicationInitialized(typeof(ProfilesApp).FullName!))
                return false;

            CallDispatcher!.Trigger("EXEV_SwitchToApplication", typeof(ProfilesApp).FullName!);
            return true;
        }

        public void SwitchProfileBatch(long pfid)
        {
            AppEnvironment.CurrentPfid = pfid;
            CallDispatcher!.Trigger("EXEV_SwitchProfile", pfid);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {

        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ProfilesBatchApp));
        }

        public override void InitializeApplicationOutput()
        {

        }
    }
}
