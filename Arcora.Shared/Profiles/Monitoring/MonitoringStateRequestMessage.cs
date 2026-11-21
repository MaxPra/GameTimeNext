using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles.Monitoring
{
    public class MonitoringStateRequestMessage : RequestMessageBase
    {
        public MonitoringStateRequestMessage()
        {
            Request = AvailableRequests.MonitoringState;
        }
    }
}
