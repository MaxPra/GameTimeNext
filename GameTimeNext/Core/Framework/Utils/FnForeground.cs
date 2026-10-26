using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GameTimeNext.Core.Framework.Utils
{
    public static class FnForeground
    {
        /// <summary>
        /// Brings the window to the foreground although another application (e.g. a game) has it.
        /// Windows only lets the process with the last input do that, and controller input doesn't count. First the input
        /// of the foreground thread is attached for the switch; if that isn't enough (e.g. the game runs elevated), an
        /// Alt key press makes this process the one with the last input. Returns true if the window is in the foreground.
        /// </summary>
        public static bool BringToFront(Window window)
        {
            bool isForeground = BringToFront(new WindowInteropHelper(window).Handle);

            window.Activate();

            return isForeground;
        }

        /// <summary>
        /// Same as BringToFront(Window) for any top level window, e.g. the window of a game.
        /// </summary>
        public static bool BringToFront(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return false;

            if (IsIconic(hwnd))
                ShowWindow(hwnd, SW_RESTORE);

            if (GetForegroundWindow() == hwnd)
                return true;

            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThread = GetWindowThreadProcessId(foregroundWindow, out _);
            uint ownThread = GetCurrentThreadId();

            bool attached = foregroundThread != 0 && foregroundThread != ownThread && AttachThreadInput(foregroundThread, ownThread, true);

            try
            {
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
            }
            finally
            {
                if (attached)
                    AttachThreadInput(foregroundThread, ownThread, false);
            }

            if (GetForegroundWindow() != hwnd)
            {
                keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);

                SetForegroundWindow(hwnd);
            }

            return GetForegroundWindow() == hwnd;
        }

        /// <summary>
        /// True if the window is visibly on top at its center, i.e. no other window (e.g. a game) covers it,
        /// even if it hasn't got the input focus.
        /// </summary>
        public static bool IsVisibleOnTop(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero || IsIconic(hwnd) || !GetWindowRect(hwnd, out RECT rect))
                return false;

            POINT center = new POINT
            {
                X = rect.Left + (rect.Right - rect.Left) / 2,
                Y = rect.Top + (rect.Bottom - rect.Top) / 2
            };

            IntPtr windowAtCenter = WindowFromPoint(center);

            return windowAtCenter != IntPtr.Zero && GetAncestor(windowAtCenter, GA_ROOT) == hwnd;
        }

        /// <summary>
        /// True if the window is the foreground window.
        /// </summary>
        public static bool IsForeground(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && GetForegroundWindow() == hwnd;
        }

        /// <summary>
        /// The main window of a group of processes (e.g. the executables of a game): the largest visible top level window
        /// without owner that isn't a tool window, also when it is minimized. Process.MainWindowHandle only looks at one
        /// process and takes the first visible window, which may be a splash or helper window.
        /// </summary>
        public static IntPtr FindMainWindow(IEnumerable<int> processIds)
        {
            HashSet<uint> ids = processIds.Select(id => (uint)id).ToHashSet();

            IntPtr mainWindow = IntPtr.Zero;
            long mainWindowArea = 0;

            if (ids.Count == 0)
                return mainWindow;

            EnumWindows((hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out uint processId);

                if (!ids.Contains(processId) || !IsWindowVisible(hwnd) || GetWindow(hwnd, GW_OWNER) != IntPtr.Zero)
                    return true;

                if ((GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64() & WS_EX_TOOLWINDOW) != 0)
                    return true;

                long area = GetRestoredArea(hwnd);

                if (area > mainWindowArea)
                {
                    mainWindow = hwnd;
                    mainWindowArea = area;
                }

                return true;
            }, IntPtr.Zero);

            return mainWindow;
        }

        /// <summary>
        /// Minimizes a window of another application without waiting for it (a hanging game doesn't block the caller).
        /// The window loses the focus with it.
        /// </summary>
        public static void Minimize(IntPtr hwnd)
        {
            if (hwnd != IntPtr.Zero && !IsIconic(hwnd))
                ShowWindowAsync(hwnd, SW_MINIMIZE);
        }

        /// <summary>
        /// Size of the window in its normal state: a minimized window only has the size of its caption.
        /// </summary>
        private static long GetRestoredArea(IntPtr hwnd)
        {
            RECT rect;

            if (IsIconic(hwnd))
            {
                WINDOWPLACEMENT placement = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };

                if (!GetWindowPlacement(hwnd, ref placement))
                    return 0;

                rect = placement.rcNormalPosition;
            }
            else if (!GetWindowRect(hwnd, out rect))
            {
                return 0;
            }

            return (long)Math.Max(0, rect.Right - rect.Left) * Math.Max(0, rect.Bottom - rect.Top);
        }

        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        private const uint GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOOLWINDOW = 0x00000080;
        private const uint GA_ROOT = 2;
        private const byte VK_MENU = 0x12;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
    }
}
