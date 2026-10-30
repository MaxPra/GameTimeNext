using GameTimeNext.Shared.Base;
using GameTimeNext.Shared.Profiles;
using RemoteMonitoring.Base;

namespace RemoteMonitoring.ApiServices
{
    public class ProfilesApiService : ApiServiceBase<ProfileDto>
    {
        public ProfilesApiService(SocketClient socket) : base(socket)
        {
        }

        #region Methods PUBLIC
        public async Task<List<ProfileDto>> GetAllProfiles()
        {
            TryResult<List<ProfileDto>> result = await TryRequestAsync<List<ProfileDto>>(new AllProfilesRequestMessage());
            return result .Value ?? [];
        }

        public async Task<ProfileDto?> GetProfile(long pfid)
        {
            TryResult<ProfileDto> result = await TryRequestAsync<ProfileDto>(new ProfileRequestMessage(pfid));
            return result.Value;
        }
        #endregion
    }
}
