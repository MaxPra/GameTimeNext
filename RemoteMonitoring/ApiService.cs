using GameTimeNext.Shared.SocketCom;
using GameTimeNext.Shared.SocketCom.ProfilesCom;
using System.Text.Json;

namespace RemoteMonitoring
{
    public class ApiService
    {
        private SocketClient _socket;

        public ApiService(SocketClient socket)
        {
            _socket = socket;
        }

        public async Task<List<OlisProfileDTO>> GetAllProfiles()
        {
            try
            {
                string? response = await _socket.SendAsync(AvailableRequests.AllProfiles);
                if (response is null)
                    throw new ApiRequestFailedException(AvailableRequests.AllProfiles);

                List<OlisProfileDTO>? serialized = JsonSerializer.Deserialize<List<OlisProfileDTO>>(response);
                if (serialized is null)
                    throw new ApiRequestFailedException(AvailableRequests.AllProfiles);

                return serialized;

            }
            catch(ApiRequestFailedException ex)
            {
                FnLog.AddError(this, string.Empty, ex);
                return [];
            }
            catch (Exception ex)
            {
                ApiRequestFailedException apiEx = new ApiRequestFailedException(AvailableRequests.AllProfiles, ex);
                FnLog.AddError(this, string.Empty, apiEx);
                return [];
            }
        }
    }

    public class ApiRequestFailedException : Exception
    {
        public ApiRequestFailedException(AvailableRequests request) : this(request, null) { }
        public ApiRequestFailedException(AvailableRequests request, Exception? innerException) : base($"ApiRequest \"{request.ToString()}\" failed.", innerException) { }
    }
}
