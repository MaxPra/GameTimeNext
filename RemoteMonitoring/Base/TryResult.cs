namespace RemoteMonitoring.Base
{
    public readonly record struct TryResult<TRes>(bool Success, TRes? Value, Exception? Exception)
    {
        public static TryResult<TRes> Ok(TRes value) => new(true, value, null);
        public static TryResult<TRes> Failed(Exception? exception = null) => new(false, default, exception);
    }
}
