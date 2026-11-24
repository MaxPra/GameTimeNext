namespace RemoteMonitoring.Base
{
    /// <summary>
    /// Result of checking a pairing code against the one of arcora (PairingService.CheckAsync).
    /// </summary>
    public enum PairingCheckResult
    {
        // The code matches the current pairing code
        Paired = 0,

        // No code, or one that doesn't match: the phone has to scan the QR code
        NotPaired = 1,

        // The current code is unknown (arcora not reachable, or the web app has just started): nothing can be said about
        // the code, the phone must not be sent to pair again
        Unreachable = 2,
    }
}
