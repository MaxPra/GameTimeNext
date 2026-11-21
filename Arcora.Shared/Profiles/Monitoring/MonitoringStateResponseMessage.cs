using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles.Monitoring
{
    public class MonitoringStateResponseMessage : ResponseMessageBase
    {
        public bool IsMonitoring { get; }
        public ProfileDto? Profile { get; }
        public DateTime? StartedAt { get; }

        public MonitoringStateResponseMessage(bool isMonitoring, ProfileDto? profile, DateTime? startedAt)
        {
            Request = AvailableRequests.MonitoringState;
            IsMonitoring = isMonitoring;
            Profile = profile;
            StartedAt = startedAt;
        }
    }
}
