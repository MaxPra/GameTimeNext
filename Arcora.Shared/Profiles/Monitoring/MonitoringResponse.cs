namespace Arcora.Shared.Profiles.Monitoring
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

        // The profile or one of its playthroughs is open for editing on the PC (record lock)
        ProfileLocked,

        // arcora was busy and got to the request only after SocketTimeouts.ChangeDeadline: nothing was changed
        RequestExpired,
    }
}
