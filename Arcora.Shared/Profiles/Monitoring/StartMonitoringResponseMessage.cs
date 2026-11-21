using Arcora.Shared.Base;

namespace Arcora.Shared.Profiles.Monitoring
{
    public class StartMonitoringResponseMessage : ResponseMessageBase
    {
        public long PFID { get; }
        public MonitoringResponse Status { get; }

        public StartMonitoringResponseMessage(long pfid, MonitoringResponse status)
        {
            Request = AvailableRequests.StartMonitoring;
            PFID = pfid;
            Status = status;
        }
    }
}
