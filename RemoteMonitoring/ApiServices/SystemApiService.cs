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
        public async Task<bool> SendLog(string message)
        {
            TryResult<bool> result = await TryRequestAsync<bool>(new LogRequestMessage(message));
            return result.Success ? result.Value : false;
        }
        #endregion
    }
}
