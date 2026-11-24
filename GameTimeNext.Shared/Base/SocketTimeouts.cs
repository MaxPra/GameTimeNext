namespace GameTimeNext.Shared.Base
{
    /// <summary>
    /// Timeouts of the socket connection between the web app and arcora, shared so both sides agree on them.
    /// </summary>
    public static class SocketTimeouts
    {
        /// <summary>
        /// Time the web app waits for the answer to a request before it gives up and connects again.
        /// </summary>
        public static readonly TimeSpan Request = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Age of a request after which arcora no longer carries out a change (e.g. starting the monitoring): its answer
        /// would arrive after the web app gave up, so the phone would not know about the change. Shorter than Request,
        /// so the answer that nothing was done still arrives in time.
        /// </summary>
        public static readonly TimeSpan ChangeDeadline = Request - TimeSpan.FromSeconds(2);

        /// <summary>
        /// Time the web app waits for the answer to a ping. Much shorter than Request: arcora answers it at once, without
        /// the UI thread, so a lost connection is noticed quickly. Waiting for an earlier request (one at a time) doesn't
        /// count.
        /// </summary>
        public static readonly TimeSpan Ping = TimeSpan.FromSeconds(5);
    }
}
