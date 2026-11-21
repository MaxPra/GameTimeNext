namespace Arcora.Shared.Base
{
    /// <summary>
    /// Asks arcora for the current pairing code of the remote monitoring; the answer is the code as JSON string.
    /// </summary>
    public class PairingCodeRequestMessage : RequestMessageBase
    {
        public PairingCodeRequestMessage()
        {
            Request = AvailableRequests.PairingCode;
        }
    }
}
