using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles.Monitoring
{
    public class StopMonitoringResponseMessage : ResponseMessageBase
    {
        public MonitoringResponse Status { get; }

        public StopMonitoringResponseMessage(MonitoringResponse status)
        {
            Request = AvailableRequests.StopMonitoring;
            Status = status;
        }
    }
}
