using Arcora.Shared.Profiles;
using Arcora.Shared.Profiles.Monitoring;
using RemoteMonitoring.Base;
using System.Collections.Concurrent;

namespace RemoteMonitoring.ApiServices
{
    public class ProfilesApiService : ApiServiceBase
    {
        private readonly ConcurrentDictionary<long, string> _coverPaths = new();

        public ProfilesApiService(SocketClient socket) : base(socket)
        {
        }

        #region Methods PUBLIC
        public async Task<List<ProfileDto>?> GetProfiles()
        {
            TryResult<List<ProfileDto>> result = await TryRequestAsync<List<ProfileDto>>(new ProfilesRequestMessage());
            if (result.Value is null)
                return null;

            foreach (ProfileDto profile in result.Value)
                RememberCover(profile);

            return result.Value;
        }

        public async Task<ProfileDetailDto?> GetProfile(long pfid)
        {
            TryResult<ProfileDetailDto> result = await TryRequestAsync<ProfileDetailDto>(new ProfileRequestMessage(pfid));

            if (result.Value is not null)
                RememberCover(result.Value);

            return result.Value;
        }

        public async Task<string?> GetCoverPath(long pfid)
        {
            if (_coverPaths.TryGetValue(pfid, out string? path))
                return path;

            return (await GetProfile(pfid))?.PPFN;
        }

        public async Task<StartMonitoringResponseMessage?> StartMonitoring(long pfid, bool createPlaythrough = false)
        {
            TryResult<StartMonitoringResponseMessage> result = await TryRequestAsync<StartMonitoringResponseMessage>(new StartMonitoringRequestMessage(pfid, createPlaythrough));
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

        #region Methods PRIVATE
        private void RememberCover(ProfileDto profile)
        {
            _coverPaths[profile.PFID] = profile.PPFN;
        }
        #endregion
    }
}
