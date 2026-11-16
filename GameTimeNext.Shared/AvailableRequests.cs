namespace GameTimeNext.Shared
{
    public enum AvailableRequests
    {
        // System
        Ping = 0,
        Log = 1,

        // Profiles
        Profiles = 101,
        Profile = 102,
        StartMonitoring = 103,
        StopMonitoring = 104,
        MonitoringState = 105,
    }
}
