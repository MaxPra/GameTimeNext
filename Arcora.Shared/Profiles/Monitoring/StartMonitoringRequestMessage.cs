using Arcora.Shared.Base;

namespace Arcora.Shared.Profiles.Monitoring
{
    public class StartMonitoringRequestMessage : RequestMessageBase
    {
        public long PFID { get; }
        public bool CreatePlaythrough { get; }

        public StartMonitoringRequestMessage(long pfid, bool createPlaythrough)
        {
            Request = AvailableRequests.StartMonitoring;
            PFID = pfid;
            CreatePlaythrough = createPlaythrough;
        }
    }
}
