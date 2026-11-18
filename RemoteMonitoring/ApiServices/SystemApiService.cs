using GameTimeNext.Shared.Base;
using RemoteMonitoring.Base;

namespace RemoteMonitoring.ApiServices
{
    public class SystemApiService : ApiServiceBase
    {
        public SystemApiService(SocketClient socket) : base(socket)
        {
        }

        #region Methods PUBLIC
        public async Task<bool> SendLog(LogRequestMessage requestMessage)
        {
            TryResult<bool> result = await TryRequestAsync<bool>(requestMessage);
            return result.Success ? result.Value : false;
        }

        /// <summary>
        /// Current pairing code of arcora, null if it did not answer.
        /// </summary>
        public async Task<string?> GetPairingCode()
        {
            TryResult<string> result = await TryRequestAsync<string>(new PairingCodeRequestMessage());
            return result.Value;
        }
        #endregion
    }
}
