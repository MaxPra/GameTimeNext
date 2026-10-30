using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    internal static class CFBlackout
    {
        private static readonly List<Window> blackoutWindows = new();
        private static readonly List<CFBlackoutTransitionWindow> transitionWindows = new();
        private static readonly List<Window> closingWindows = new();
        private static bool isActive = false;

        // All monitors are blacked out (not only the secondary ones)
        private static bool isFullBlackout = false;

        // Animation shown on the full blackout (will become a user setting later)
        private static readonly BlackoutAnimationType blackoutAnimation = BlackoutAnimationType.ParticleProfileCover;

        // Particle transition when the blackout is turned on/off
        private static readonly bool useTransitions = true;

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

            CancelTransitions();

            bool showMovingLabel = AppEnvironment.GetAppConfig().AppSettings.EnableFullBlackoutText && !consoleMode;
            CreateWindowsForMonitors(owner, monitor => true, showMovingLabel: showMovingLabel);
            FnDisplay.MoveMouseToVirtualBottomRight();
            FnDisplay.HideMouseCursorGlobally();
            FnDisplay.ClipMouseToVirtualBottomRightPixel();

            isActive = true;
            isFullBlackout = true;
        }

        public static void EnableOnSecondaryMonitors(Window owner, bool manageCursor = false)
        {
            if (isActive)
            {
                return;
            }

            CancelTransitions();
            CreateWindowsForMonitors(owner, monitor => !monitor.IsPrimary, showMovingLabel: false);

            if (manageCursor)
            {
                FnDisplay.MoveMouseToVirtualBottomRight();
                FnDisplay.HideMouseCursorGlobally();
                FnDisplay.ClipMouseToVirtualBottomRightPixel();
            }

            isActive = true;
            isFullBlackout = false;
        }

        public static void Disable(Dispatcher dispatcher)
        {
            if (!isActive)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                CloseAllWindowsAnimated();
            }
            else
            {
                dispatcher.Invoke(CloseAllWindowsAnimated);
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
                // Black out all screens in console mode
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

        /// <summary>
        /// True while all monitors are blacked out (not only the secondary ones while monitoring).
        /// </summary>
        public static bool IsFullBlackout()
        {
            return isActive && isFullBlackout;
        }

        private static void CreateWindowsForMonitors(Window owner, Func<FnDisplay.MonitorInfoData, bool> predicate, bool showMovingLabel)
        {
            foreach (var monitor in FnDisplay.GetAllMonitors().Where(predicate))
            {
                var wnd = BuildBlackoutWindow(owner, monitor, showMovingLabel);
                blackoutWindows.Add(wnd);

                if (useTransitions)
                {
                    ShowWithTransition(owner, wnd);
                }
                else
                {
                    wnd.Show();
                }
            }
        }

        /// <summary>
        /// Plays the "burn in" transition over the live desktop. Once the screen is completely black,
        /// the running transition surface (black + embers) moves into the opaque blackout window as an overlay
        /// and the expensive transparent transition window is closed. The overlay then fades out its black
        /// over the starting blackout content, so both never render as two full screen windows at the same time.
        /// </summary>
        private static void ShowWithTransition(Window owner, Window wnd)
        {
            var transition = new CFBlackoutTransitionWindow(owner, GetBounds(wnd), BlackoutTransitionMode.In);
            transitionWindows.Add(transition);

            transition.Covered += () =>
            {
                if (!blackoutWindows.Contains(wnd))
                {
                    CloseTransition(transition);
                    return;
                }

                var surface = transition.DetachSurface();
                var root = (Grid)wnd.Content;
                root.Children.Add(surface);

                surface.Finished += () =>
                {
                    surface.Stop();
                    root.Children.Remove(surface);
                };
                wnd.Closed += (_, _) => surface.Stop();

                bool handedOver = false;
                void HandOver()
                {
                    if (handedOver)
                    {
                        return;
                    }

                    handedOver = true;
                    CloseTransition(transition);
                    surface.Release();
                }

                wnd.ContentRendered += (_, _) => HandOver();
                wnd.Show();

                // Fallback in case ContentRendered is not raised
                wnd.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(HandOver));
            };
            transition.Completed += () => CloseTransition(transition);

            transition.Show();
        }

        /// <summary>
        /// Covers the blackout window with a fully black transition window, closes the blackout window
        /// and then burns a hole into the black until the desktop is completely visible again.
        /// </summary>
        private static void HideWithTransition(Window wnd)
        {
            // The hole must not start burning before the blackout window is gone,
            // otherwise the blackout window would be visible through it
            var transition = new CFBlackoutTransitionWindow(wnd.Owner, GetBounds(wnd), BlackoutTransitionMode.Out, startImmediately: false);
            transitionWindows.Add(transition);
            closingWindows.Add(wnd);

            bool handedOver = false;
            void HandOver()
            {
                if (handedOver || !transitionWindows.Contains(transition))
                {
                    return;
                }

                handedOver = true;
                closingWindows.Remove(wnd);
                CloseQuietly(wnd);
                transition.Begin();
            }

            transition.ContentRendered += (_, _) => HandOver();
            transition.Completed += () => CloseTransition(transition);

            transition.Show();

            // Fallback in case ContentRendered is not raised
            transition.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(HandOver));
        }

        private static Rect GetBounds(Window wnd)
        {
            return new Rect(wnd.Left, wnd.Top, wnd.Width, wnd.Height);
        }

        private static void CloseTransition(CFBlackoutTransitionWindow transition)
        {
            transitionWindows.Remove(transition);
            CloseQuietly(transition);
        }

        /// <summary>
        /// Immediately ends all running transitions (e.g. the blackout is toggled again while one is still playing).
        /// </summary>
        private static void CancelTransitions()
        {
            foreach (var transition in transitionWindows.ToList())
            {
                CloseQuietly(transition);
            }

            transitionWindows.Clear();

            foreach (var window in closingWindows.ToList())
            {
                CloseQuietly(window);
            }

            closingWindows.Clear();
        }

        private static void CloseQuietly(Window window)
        {
            try
            {
                window.Close();
            }
            catch
            {
            }
        }

        private static void CloseAllWindowsAnimated()
        {
            // A blackout that is still burning in gets cancelled without an outro
            CancelTransitions();

            foreach (var window in blackoutWindows.ToList())
            {
                if (useTransitions && window.IsVisible)
                {
                    HideWithTransition(window);
                }
                else
                {
                    CloseQuietly(window);
                }
            }

            blackoutWindows.Clear();
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

                // Covering the screens must not take the focus from the window in front (a game, the Big Picture mode)
                ShowActivated = false,
                AllowsTransparency = false,
                Background = Brushes.Black,
                Left = leftDip,
                Top = topDip,
                Width = widthDip,
                Height = heightDip,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

            // Root grid: blackout content + (temporarily) the transition overlay on top
            var root = new Grid();
            wnd.Content = root;

            if (showMovingLabel)
            {
                AddBlackoutAnimation(wnd, root);
            }

            wnd.Cursor = System.Windows.Input.Cursors.None;
            wnd.Focusable = false;

            wnd.PreviewKeyDown += (s, e) => e.Handled = true;
            wnd.PreviewMouseDown += (s, e) => e.Handled = true;
            wnd.PreviewMouseUp += (s, e) => e.Handled = true;
            wnd.PreviewMouseMove += (s, e) => e.Handled = true;

            return wnd;
        }

        private static CFBlackoutAnimationBase CreateBlackoutAnimation()
        {
            if (blackoutAnimation == BlackoutAnimationType.ParticleProfileCover)
            {
                // Falls back to the clock if no profile is selected or it has no cover
                var cover = CFBlackoutParticleCover.TryCreate();
                if (cover != null)
                {
                    return cover;
                }
            }

            return new CFBlackoutParticleClock();
        }

        private static void AddBlackoutAnimation(Window wnd, Grid root)
        {
            var animation = CreateBlackoutAnimation();
            root.Children.Add(animation);

            wnd.Loaded += (_, _) => animation.Start();
            wnd.Closed += (_, _) => animation.Stop();
        }
    }
}
