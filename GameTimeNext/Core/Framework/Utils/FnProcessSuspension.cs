using System.Runtime.InteropServices;

namespace GameTimeNext.Core.Framework.Utils
{
    /// <summary>
    /// Suspends and resumes whole processes (all their threads), like the "Suspend" of the Resource Monitor.
    /// </summary>
    public static class FnProcessSuspension
    {
        private const uint PROCESS_SUSPEND_RESUME = 0x0800;

        private static readonly HashSet<int> _suspendedProcessIds = new HashSet<int>();
        private static readonly object _suspendedProcessIdsLock = new object();

        /// <summary>
        /// Suspends the process. Returns false if it can't be opened for it (e.g. it runs elevated or is protected
        /// by an anti-cheat) or the call fails.
        /// </summary>
        public static bool Suspend(int processId)
        {
            if (!Call(processId, NtSuspendProcess))
                return false;

            lock (_suspendedProcessIdsLock)
                _suspendedProcessIds.Add(processId);

            return true;
        }

        /// <summary>
        /// Resumes a process suspended with Suspend. Returns false if it doesn't exist anymore or the call fails.
        /// </summary>
        public static bool Resume(int processId)
        {
            lock (_suspendedProcessIdsLock)
                _suspendedProcessIds.Remove(processId);

            return Call(processId, NtResumeProcess);
        }

        /// <summary>
        /// True if the process was suspended with Suspend and not resumed yet. Its threads handle no messages: nothing
        /// may wait for them (e.g. AttachThreadInput, SendMessage).
        /// </summary>
        public static bool IsSuspended(int processId)
        {
            lock (_suspendedProcessIdsLock)
                return _suspendedProcessIds.Contains(processId);
        }

        private static bool Call(int processId, Func<IntPtr, int> function)
        {
            IntPtr handle = OpenProcess(PROCESS_SUSPEND_RESUME, false, (uint)processId);

            if (handle == IntPtr.Zero)
                return false;

            try
            {
                // NTSTATUS: negative values are errors
                return function(handle) >= 0;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("ntdll.dll")]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll")]
        private static extern int NtResumeProcess(IntPtr processHandle);
    }
}
