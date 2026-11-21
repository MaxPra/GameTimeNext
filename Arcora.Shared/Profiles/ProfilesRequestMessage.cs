using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles
{
    public class ProfilesRequestMessage : RequestMessageBase
    {
        public ProfilesRequestMessage()
        {
            Request = AvailableRequests.Profiles;
        }
    }
}
