using GameTimeNext.Shared.Base;
using RemoteMonitoring.Base.Exceptions;
using System.Text.Json;

namespace RemoteMonitoring.Base
{
    public abstract class ApiServiceBase<TDto> where TDto : DtoBase
    {
        // OFDO: Add Auto-Retry

        private SocketClient _socket;

        public ApiServiceBase(SocketClient socket)
        {
            _socket = socket;
        }

        #region Methods PROTECTED
        protected Task<TryResult<TRes>> TryRequestAsync<TRes>(RequestMessageBase requestMessage)
        {
            return TryExecuteAsync(requestMessage, () => RequestAsync<TRes>(requestMessage));
        }
        #endregion

        #region Methods PRIVATE
        private async Task<TryResult<TRes>> TryExecuteAsync<TRes>(RequestMessageBase requestMessage, Func<Task<TRes>> action)
        {
            try
            {
                return TryResult<TRes>.Ok(await action());
            }
            catch (ApiRequestFailedException ex)
            {
                FnLog.AddError(this, string.Empty, ex);
                return TryResult<TRes>.Failed();
            }
            catch (Exception ex)
            {
                ApiRequestFailedException apiEx = new ApiRequestFailedException(requestMessage.Request, ex);
                FnLog.AddError(this, string.Empty, apiEx);
                return TryResult<TRes>.Failed(ex);
            }
        }

        private async Task<TRes> RequestAsync<TRes>(RequestMessageBase requestMessage)
        {
            string? response = await _socket.SendAsync(requestMessage);
            if (response is null)
                throw new ApiRequestFailedException(requestMessage.Request);

            TRes? result = JsonSerializer.Deserialize<TRes>(response);
            if (result is null)
                throw new ApiRequestFailedException(requestMessage.Request);

            return result;
        }
        #endregion
    }
}
