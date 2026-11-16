namespace RemoteMonitoring.Base
{
    public enum ConnectionState
    {
        NeverConnected = 0,
        Connected = 1,
        Reconnecting = 2,
        Disconnected = 9,
    }
}
