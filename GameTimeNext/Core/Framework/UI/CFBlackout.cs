using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    internal static class CFBlackout
    {
        private static readonly List<Window> blackoutWindows = new();
        private static readonly List<Action> movementStops = new();
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

        public static void Enable(Window owner, bool consoleMode = false)
        {
            if (isActive)
            {
                return;
            }

            bool showMovingLabel = AppEnvironment.GetAppConfig().AppSettings.EnableFullBlackoutText && !consoleMode;
            CreateWindowsForMonitors(owner, monitor => true, showMovingLabel: showMovingLabel);
            FnDisplay.MoveMouseToVirtualBottomRight();
            FnDisplay.HideMouseCursorGlobally();
            FnDisplay.ClipMouseToVirtualBottomRightPixel();

            isActive = true;
        }

        public static void EnableOnSecondaryMonitors(Window owner, bool manageCursor = false)
        {
            if (isActive)
            {
                return;
            }

            CreateWindowsForMonitors(owner, monitor => !monitor.IsPrimary, showMovingLabel: false);

            if (manageCursor)
            {
                FnDisplay.MoveMouseToVirtualBottomRight();
                FnDisplay.HideMouseCursorGlobally();
                FnDisplay.ClipMouseToVirtualBottomRightPixel();
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

            FnDisplay.UnclipMouse();
            FnDisplay.ShowMouseCursorGlobally();

            isActive = false;
        }

        public static bool AllowedToToggleBlackout(T1PROFI t1profi)
        {
            if (TFPROFI.BlackoutOverridenAndActive(t1profi))
                return true;

            if (TFPROFI.BlackoutOverridenAndInactive(t1profi))
                return false;

            if (AppEnvironment.GetAppConfig().AppSettings.BlackoutSideMonitors)
                return true;

            return false;
        }

        public static void ToggleSecondaryBlackout(Window owner, bool manageCursor = false, bool consoleMode = false)
        {
            if (isActive)
            {
                Disable(owner.Dispatcher);
            }
            else
            {
                // In console mode, black out all monitors
                if (consoleMode)
                    Enable(owner, true);
                else
                    EnableOnSecondaryMonitors(owner, manageCursor);
            }
        }

        public static bool IsActive()
        {
            return isActive;
        }

        private static void CreateWindowsForMonitors(Window owner, Func<FnDisplay.MonitorInfoData, bool> predicate, bool showMovingLabel)
        {
            foreach (var monitor in FnDisplay.GetAllMonitors().Where(predicate))
            {
                var wnd = BuildBlackoutWindow(owner, monitor, showMovingLabel);
                blackoutWindows.Add(wnd);
                wnd.Show();
            }
        }

        private static void CloseAllWindows()
        {
            StopMovementTimers();

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

        private static void StopMovementTimers()
        {
            foreach (var stop in movementStops.ToList())
            {
                try
                {
                    stop();
                }
                catch
                {
                }
            }

            movementStops.Clear();
        }

        private static Window BuildBlackoutWindow(Window owner, FnDisplay.MonitorInfoData monitor, bool showMovingLabel)
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

            if (showMovingLabel)
            {
                AddParticleClock(wnd);
            }

            wnd.Cursor = System.Windows.Input.Cursors.None;
            wnd.Focusable = false;

            wnd.PreviewKeyDown += (s, e) => e.Handled = true;
            wnd.PreviewMouseDown += (s, e) => e.Handled = true;
            wnd.PreviewMouseUp += (s, e) => e.Handled = true;
            wnd.PreviewMouseMove += (s, e) => e.Handled = true;

            return wnd;
        }

        private static void AddParticleClock(Window wnd)
        {
            var clock = new CFBlackoutParticleClock();
            wnd.Content = clock;

            void StopAnimation()
            {
                clock.Stop();
            }

            wnd.Loaded += (_, _) =>
            {
                clock.Start();
                movementStops.Add(StopAnimation);
            };

            wnd.Closed += (_, _) =>
            {
                StopAnimation();
                movementStops.Remove(StopAnimation);
            };
        }
    }
}
