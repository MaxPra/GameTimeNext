using Arcora.Shared.Base;

namespace Arcora.Shared.Profiles.Monitoring
{
    public class MonitoringStateRequestMessage : RequestMessageBase
    {
        public MonitoringStateRequestMessage()
        {
            Request = AvailableRequests.MonitoringState;
        }
    }
}
