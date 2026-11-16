using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles.Monitoring
{
    public class StopMonitoringResponseMessage : ResponseMessageBase
    {
        public MonitoringResponse Status { get; }
        public double? MonitoredMinutes { get; }
        public bool? SessionSaved { get; }

        public StopMonitoringResponseMessage(MonitoringResponse status, double? monitoredMinutes = null, bool? sessionSaved = null)
        {
            Request = AvailableRequests.StopMonitoring;
            Status = status;
            MonitoredMinutes = monitoredMinutes;
            SessionSaved = sessionSaved;
        }
    }
}
