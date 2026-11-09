namespace GameTimeNext.Shared.Base
{
    public class LogRequestMessage : RequestMessageBase
    {
        public string Message { get; }

        public LogRequestMessage(string message)
        {
            Request = AvailableRequests.Log;
            Message = message;
        }
    }
}
