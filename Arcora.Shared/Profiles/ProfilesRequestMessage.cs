using Arcora.Shared.Base;

namespace Arcora.Shared.Profiles
{
    public class ProfilesRequestMessage : RequestMessageBase
    {
        public ProfilesRequestMessage()
        {
            Request = AvailableRequests.Profiles;
        }
    }
}
