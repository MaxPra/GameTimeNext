using Arcora.Shared.Base;

namespace Arcora.Shared.Profiles.Monitoring
{
    public class StopMonitoringRequestMessage : RequestMessageBase
    {
        public StopMonitoringRequestMessage()
        {
            Request = AvailableRequests.StopMonitoring;
        }
    }
}
