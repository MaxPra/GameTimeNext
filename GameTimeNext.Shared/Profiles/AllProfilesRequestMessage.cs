namespace GameTimeNext.Shared.Base
{
    public class AllProfilesRequestMessage : RequestMessageBase
    {
        public AllProfilesRequestMessage()
        {
            Request = AvailableRequests.AllProfiles;
        }
    }
}
