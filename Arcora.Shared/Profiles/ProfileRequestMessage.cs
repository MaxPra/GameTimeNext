using Arcora.Shared.Base;

namespace Arcora.Shared.Profiles
{
    public class ProfileRequestMessage : RequestMessageBase
    {
        public long PFID { get; }

        public ProfileRequestMessage(long pfid)
        {
            Request = AvailableRequests.Profile;
            PFID = pfid;
        }
    }
}
