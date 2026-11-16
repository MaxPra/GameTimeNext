namespace GameTimeNext.Shared.Profiles.Monitoring
{
    public enum MonitoringResponse
    {
        ProfilesAppNotReady,
        ProfileNotFound,
        NoActivePlaythrough,

        MonitoringAlreadyRunning,
        MonitoringNotRunningYet,

        MonitoringStarted,
        MonitoringStopped,
    }
}
