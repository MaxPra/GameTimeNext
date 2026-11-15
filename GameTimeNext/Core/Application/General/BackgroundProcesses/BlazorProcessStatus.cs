namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    public enum BlazorProcessState
    {
        NotStarted,

        /// <summary>
        /// Debug: the web app is started by the developer, not by RemoteMonitoringProcess.
        /// </summary>
        StartedSeparately,

        Running,

        /// <summary>
        /// Ended by itself (crash or regular exit); RemoteMonitoringProcess starts it again in its next cycle.
        /// </summary>
        Exited,

        /// <summary>
        /// Ended by RemoteMonitoringProcess (remote monitoring switched off or app exit).
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// Snapshot of the Blazor process of the remote monitoring, e.g. for the module "Diagnostics".
    /// </summary>
    public sealed record BlazorProcessStatus(
        BlazorProcessState State,
        int? ProcessId = null,
        int? Port = null,
        DateTime? StartedAt = null,
        DateTime? EndedAt = null,
        int? ExitCode = null,
        int RestartCount = 0)
    {
        public static readonly BlazorProcessStatus NotStarted = new(BlazorProcessState.NotStarted);
    }
}
