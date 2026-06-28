using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Batch
{
    public class ProfilesBatchApp : UIXApplication
    {
        public override void InitializeApplicationOutput()
        {

        }

        public void Start(UIXApplication hostApplication)
        {
            this.HostApplication = hostApplication;
            CallDispatcher = hostApplication.CallDispatcher;
        }

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
    }
}
