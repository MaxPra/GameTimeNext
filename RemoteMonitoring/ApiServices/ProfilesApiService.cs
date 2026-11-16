using GameTimeNext.Shared.Profiles;
using GameTimeNext.Shared.Profiles.Monitoring;
using RemoteMonitoring.Base;

namespace RemoteMonitoring.ApiServices
{
    public class ProfilesApiService : ApiServiceBase
    {
        public ProfilesApiService(SocketClient socket) : base(socket)
        {
        }

        #region Methods PUBLIC
        public async Task<List<ProfileDto>> GetProfiles()
        {
            TryResult<List<ProfileDto>> result = await TryRequestAsync<List<ProfileDto>>(new ProfilesRequestMessage());
            return result.Value ?? [];
        }

        public async Task<ProfileDto?> GetProfile(long pfid)
        {
            TryResult<ProfileDto> result = await TryRequestAsync<ProfileDto>(new ProfileRequestMessage(pfid));
            return result.Value;
        }

        public async Task<StartMonitoringResponseMessage?> StartMonitoring(long pfid)
        {
            TryResult<StartMonitoringResponseMessage> result = await TryRequestAsync<StartMonitoringResponseMessage>(new StartMonitoringRequestMessage(pfid, false));
            return result.Value;
        }

        public async Task<StopMonitoringResponseMessage?> StopMonitoring()
        {
            TryResult<StopMonitoringResponseMessage> result = await TryRequestAsync<StopMonitoringResponseMessage>(new StopMonitoringRequestMessage());
            return result.Value;
        }

        public async Task<MonitoringStateResponseMessage?> GetMonitoringState()
        {
            TryResult<MonitoringStateResponseMessage> result = await TryRequestAsync<MonitoringStateResponseMessage>(new MonitoringStateRequestMessage());
            return result.Value;
        }
        #endregion
    }
}
