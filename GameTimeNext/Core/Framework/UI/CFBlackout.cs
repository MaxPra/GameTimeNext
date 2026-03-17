using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    internal static class CFBlackout
    {
        private static readonly List<Window> blackoutWindows = new();
        private static bool isActive = false;

        private static bool cursorClipped = false;
        private static bool mouseHidden = false;

        public static void ToggleBlackout(Window owner)
        {
            if (isActive)
            {
                Disable(owner.Dispatcher);
            }
            else
            {
                Enable(owner);
            }
        }

        public static void Enable(Window owner)
        {
            if (isActive)
            {
                return;
            }

            CreateWindowsForMonitors(owner, monitor => true);
            MoveMouseToVirtualBottomRight();
            HideMouseCursorGlobally();
            ClipMouseToVirtualBottomRightPixel();

            isActive = true;
        }

        public static void EnableOnSecondaryMonitors(Window owner, bool manageCursor = false)
        {
            if (isActive)
            {
                return;
            }

            CreateWindowsForMonitors(owner, monitor => !monitor.IsPrimary);

            if (manageCursor)
            {
                MoveMouseToVirtualBottomRight();
                HideMouseCursorGlobally();
                ClipMouseToVirtualBottomRightPixel();
            }

            isActive = true;
        }

        public static void Disable(Dispatcher dispatcher)
        {
            if (!isActive)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                CloseAllWindows();
            }
            else
            {
                dispatcher.Invoke(CloseAllWindows);
            }

            UnclipMouse();
            ShowMouseCursorGlobally();

            isActive = false;
        }

        public static void ToggleSecondaryBlackout(Window owner, bool manageCursor = false)
        {
            if (isActive)
            {
                Disable(owner.Dispatcher);
            }
            else
            {
                EnableOnSecondaryMonitors(owner, manageCursor);
            }
        }

        public static bool IsActive()
        {
            return isActive;
        }

        private static void CreateWindowsForMonitors(Window owner, Func<MonitorInfoData, bool> predicate)
        {
            foreach (var monitor in GetAllMonitors().Where(predicate))
            {
                var wnd = BuildBlackoutWindow(owner, monitor);
                blackoutWindows.Add(wnd);
                wnd.Show();
            }
        }

        private static void CloseAllWindows()
        {
            foreach (var window in blackoutWindows.ToList())
            {
                try
                {
                    window.Close();
                }
                catch
                {
                }
            }

            blackoutWindows.Clear();
        }

        private static Window BuildBlackoutWindow(Window owner, MonitorInfoData monitor)
        {
            var source = PresentationSource.FromVisual(owner);
            double leftDip = monitor.Bounds.Left;
            double topDip = monitor.Bounds.Top;
            double widthDip = monitor.Bounds.Width;
            double heightDip = monitor.Bounds.Height;

            if (source?.CompositionTarget != null)
            {
                var transform = source.CompositionTarget.TransformFromDevice;

                var topLeft = transform.Transform(new Point(monitor.Bounds.Left, monitor.Bounds.Top));
                var bottomRight = transform.Transform(new Point(monitor.Bounds.Right, monitor.Bounds.Bottom));

                leftDip = topLeft.X;
                topDip = topLeft.Y;
                widthDip = bottomRight.X - topLeft.X;
                heightDip = bottomRight.Y - topLeft.Y;
            }

            var wnd = new Window
            {
                Owner = owner,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                AllowsTransparency = false,
                Background = Brushes.Black,
                Left = leftDip,
                Top = topDip,
                Width = widthDip,
                Height = heightDip,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            wnd.Cursor = System.Windows.Input.Cursors.None;
            wnd.Focusable = false;

            wnd.PreviewKeyDown += (s, e) => e.Handled = true;
            wnd.PreviewMouseDown += (s, e) => e.Handled = true;
            wnd.PreviewMouseUp += (s, e) => e.Handled = true;
            wnd.PreviewMouseMove += (s, e) => e.Handled = true;

            return wnd;
        }

        private static List<MonitorInfoData> GetAllMonitors()
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

        private static void MoveMouseToVirtualBottomRight()
        {
            var virtualScreen = GetVirtualScreenBounds();
            int targetX = virtualScreen.right - 1;
            int targetY = virtualScreen.bottom - 1;
            SetCursorPos(targetX, targetY);
        }

        private static void ClipMouseToVirtualBottomRightPixel()
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

        private static void UnclipMouse()
        {
            if (!cursorClipped)
            {
                return;
            }

            ClipCursor(IntPtr.Zero);
            cursorClipped = false;
        }

        private static void HideMouseCursorGlobally()
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

        private static void ShowMouseCursorGlobally()
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

        private sealed class MonitorInfoData
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

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
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