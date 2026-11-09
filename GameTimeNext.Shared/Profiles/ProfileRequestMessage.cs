namespace GameTimeNext.Shared.Base
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
