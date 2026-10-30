using System.Runtime.InteropServices;

namespace GameTimeNext.Core.Framework.Utils
{
    internal static class FnDisplay
    {
        private static bool cursorClipped = false;
        private static bool mouseHidden = false;

        public static List<MonitorInfoData> GetAllMonitors()
        {
            var monitors = new List<MonitorInfoData>();

            EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                (IntPtr monitorHandle, IntPtr hdc, ref RECT monitorRect, IntPtr lParam) =>
                {
                    var monitorInfo = new MONITORINFOEX();
                    monitorInfo.cbSize = Marshal.SizeOf<MONITORINFOEX>();

                    if (GetMonitorInfo(monitorHandle, ref monitorInfo))
                    {
                        monitors.Add(new MonitorInfoData
                        {
                            Handle = monitorHandle,
                            Bounds = monitorInfo.rcMonitor,
                            WorkArea = monitorInfo.rcWork,
                            IsPrimary = (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0,
                            DeviceName = monitorInfo.szDevice
                        });
                    }

                    return true;
                },
                IntPtr.Zero);

            return monitors;
        }

        /// <summary>
        /// Actual bounds of the window in device independent units. Unlike Window.Left/Top this is also correct for a
        /// maximized window (Left/Top then return the restore position, which may be on another or a disconnected monitor).
        /// Null if the window has no handle yet.
        /// </summary>
        public static System.Windows.Rect? GetWindowBounds(System.Windows.Window window)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out RECT rect))
                return null;

            return ToDeviceIndependent(window, rect);
        }

        /// <summary>
        /// Work area (without taskbar) of the monitor the window is on, or the nearest one, in device independent units.
        /// Null if the window has no handle yet.
        /// </summary>
        public static System.Windows.Rect? GetWorkArea(System.Windows.Window window)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero)
                return null;

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            var monitorInfo = new MONITORINFOEX();
            monitorInfo.cbSize = Marshal.SizeOf<MONITORINFOEX>();

            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
                return null;

            return ToDeviceIndependent(window, monitorInfo.rcWork);
        }

        private static System.Windows.Rect ToDeviceIndependent(System.Windows.Window window, RECT rect)
        {
            var transform = System.Windows.PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice
                ?? System.Windows.Media.Matrix.Identity;

            var topLeft = transform.Transform(new System.Windows.Point(rect.left, rect.top));
            var bottomRight = transform.Transform(new System.Windows.Point(rect.right, rect.bottom));

            return new System.Windows.Rect(topLeft, bottomRight);
        }

        private static RECT GetVirtualScreenBounds()
        {
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            return new RECT
            {
                left = left,
                top = top,
                right = left + width,
                bottom = top + height
            };
        }

        public static void MoveMouseToVirtualBottomRight()
        {
            var virtualScreen = GetVirtualScreenBounds();
            int targetX = virtualScreen.right - 1;
            int targetY = virtualScreen.bottom - 1;
            SetCursorPos(targetX, targetY);
        }

        public static void ClipMouseToVirtualBottomRightPixel()
        {
            if (cursorClipped)
            {
                return;
            }

            var virtualScreen = GetVirtualScreenBounds();

            var rect = new RECT
            {
                left = virtualScreen.right - 1,
                top = virtualScreen.bottom - 1,
                right = virtualScreen.right,
                bottom = virtualScreen.bottom
            };

            ClipCursor(ref rect);
            cursorClipped = true;
        }

        public static void UnclipMouse()
        {
            if (!cursorClipped)
            {
                return;
            }

            ClipCursor(IntPtr.Zero);
            cursorClipped = false;
        }

        public static void HideMouseCursorGlobally()
        {
            if (mouseHidden)
            {
                return;
            }

            while (ShowCursor(false) >= 0)
            {
            }

            mouseHidden = true;
        }

        public static void ShowMouseCursorGlobally()
        {
            if (!mouseHidden)
            {
                return;
            }

            while (ShowCursor(true) < 0)
            {
            }

            mouseHidden = false;
        }

        public sealed class MonitorInfoData
        {
            public IntPtr Handle { get; set; }

            public RECT Bounds { get; set; }

            public RECT WorkArea { get; set; }

            public bool IsPrimary { get; set; }

            public string DeviceName { get; set; } = string.Empty;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        private const int MONITORINFOF_PRIMARY = 0x00000001;

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(IntPtr lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(
            IntPtr hdc,
            IntPtr lprcClip,
            MonitorEnumProc lpfnEnum,
            IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public int Left => left;
            public int Top => top;
            public int Right => right;
            public int Bottom => bottom;
            public int Width => right - left;
            public int Height => bottom - top;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }
    }
}
