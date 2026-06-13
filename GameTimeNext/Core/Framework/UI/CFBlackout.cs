using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    internal static class CFBlackout
    {
        private static readonly List<Window> blackoutWindows = new();
        private static bool isActive = false;

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
            CFDisplay.MoveMouseToVirtualBottomRight();
            CFDisplay.HideMouseCursorGlobally();
            CFDisplay.ClipMouseToVirtualBottomRightPixel();

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
                CFDisplay.MoveMouseToVirtualBottomRight();
                CFDisplay.HideMouseCursorGlobally();
                CFDisplay.ClipMouseToVirtualBottomRightPixel();
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

            CFDisplay.UnclipMouse();
            CFDisplay.ShowMouseCursorGlobally();

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

        private static void CreateWindowsForMonitors(Window owner, Func<CFDisplay.MonitorInfoData, bool> predicate)
        {
            foreach (var monitor in CFDisplay.GetAllMonitors().Where(predicate))
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

        private static Window BuildBlackoutWindow(Window owner, CFDisplay.MonitorInfoData monitor)
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
    }
}