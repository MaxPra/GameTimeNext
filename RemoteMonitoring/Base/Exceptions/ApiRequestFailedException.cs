using GameTimeNext.Shared;

namespace RemoteMonitoring.Base.Exceptions
{
    public class ApiRequestFailedException : Exception
    {
        public ApiRequestFailedException(AvailableRequests request) : this(request, null) { }
        public ApiRequestFailedException(AvailableRequests request, Exception? innerException) : base($"ApiRequest \"{request.ToString()}\" failed.", innerException) { }
    }
}
