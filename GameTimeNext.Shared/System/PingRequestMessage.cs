namespace GameTimeNext.Shared.Base
{
    public class PingRequestMessage : RequestMessageBase
    {
        public PingRequestMessage()
        {
            Request = AvailableRequests.Ping;
        }
    }
}
