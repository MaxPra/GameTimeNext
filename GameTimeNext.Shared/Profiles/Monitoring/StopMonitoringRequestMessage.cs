using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles.Monitoring
{
    public class StopMonitoringRequestMessage : RequestMessageBase
    {
        public StopMonitoringRequestMessage()
        {
            Request = AvailableRequests.StopMonitoring;
        }
    }
}
