using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Toast messages at the top right of the work area (window ToastMessage). A toast shown while others are open is
    /// placed below them; when one closes, the ones below it move up.
    /// </summary>
    internal static class CFToast
    {
        private const double ScreenMargin = 16;
        private const double Gap = 8;
        private const double IconSize = 24;

        private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);
        private static readonly Uri IconUri = new Uri("pack://application:,,,/UI/Ressources/GTN_APP_ICON.ico", UriKind.Absolute);

        // Shown and not closing, from top to bottom
        private static readonly List<ToastEntry> openToasts = new();
        private static IReadOnlyList<BitmapFrame>? iconFrames;

        private sealed class ToastEntry
        {
            public required ToastMessage Window { get; init; }
            public required DispatcherTimer CloseTimer { get; init; }
            public double TargetTop { get; set; }
            public bool IsGlass { get; set; }
            public bool IsClosing { get; set; }
        }

        public static void Show(string title, string message) => Show(title, message, DefaultDuration);

        public static void Show(string title, string message, TimeSpan duration)
        {
            Dispatcher dispatcher = System.Windows.Application.Current.Dispatcher;

            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => Show(title, message, duration));
                return;
            }

            ToastMessage window = new ToastMessage();
            window.txTitle.Text = title;
            window.txMessage.Text = message;
            window.txMessage.Visibility = FnString.IsNullEmptyOrWhitespace(message) ? Visibility.Collapsed : Visibility.Visible;

            // Final position is set on Loaded (needs the height); until then the content is transparent
            Rect workArea = SystemParameters.WorkArea;
            window.Left = workArea.Right - window.Width - ScreenMargin;
            window.Top = workArea.Top + ScreenMargin;

            ToastEntry entry = new ToastEntry
            {
                Window = window,
                CloseTimer = new DispatcherTimer { Interval = duration }
            };

            entry.CloseTimer.Tick += (_, _) => BeginClose(entry);
            window.SourceInitialized += (_, _) => SetNoActivate(window);
            window.Loaded += (_, _) => Open(entry);
            window.Closed += (_, _) =>
            {
                // Also when the window is closed from outside (app exit)
                entry.CloseTimer.Stop();
                if (openToasts.Remove(entry))
                    ArrangeStack(null);
            };

            window.Show();
            ShowWindow(new WindowInteropHelper(window).Handle, SW_SHOWNOACTIVATE);
        }

        private static void Open(ToastEntry entry)
        {
            entry.Window.imgIcon.Source = GetIcon(entry.Window);

            // Not enough room below the others: the oldest give way
            double available = SystemParameters.WorkArea.Height - 2 * ScreenMargin;
            while (openToasts.Count > 0 && GetStackHeight() + Gap + entry.Window.ActualHeight > available)
                BeginClose(openToasts[0]);

            openToasts.Add(entry);
            ArrangeStack(entry);
            AnimatePopIn(entry);
            entry.CloseTimer.Start();
        }

        private static double GetStackHeight()
            => openToasts.Sum(entry => entry.Window.ActualHeight) + Gap * (openToasts.Count - 1);

        /// <summary>Moves the toasts to their place in the stack; the new one is placed directly, the others slide.</summary>
        private static void ArrangeStack(ToastEntry? newEntry)
        {
            double top = SystemParameters.WorkArea.Top + ScreenMargin;

            foreach (ToastEntry entry in openToasts)
            {
                if (entry == newEntry)
                {
                    entry.TargetTop = top;
                    entry.Window.Top = top;
                }
                else if (Math.Abs(entry.TargetTop - top) > 0.5)
                {
                    entry.TargetTop = top;
                    entry.Window.BeginAnimation(Window.TopProperty, new DoubleAnimation
                    {
                        To = top,
                        Duration = TimeSpan.FromMilliseconds(260),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
                }

                top += entry.Window.ActualHeight + Gap;
            }
        }

        /// <summary>
        /// Scales the content in with a slight overshoot. The acrylic blur always covers the whole window, so it is only
        /// switched on afterwards, then the opaque layer fades out to the glass.
        /// </summary>
        private static void AnimatePopIn(ToastEntry entry)
        {
            ToastMessage window = entry.Window;
            TimeSpan scaleDuration = TimeSpan.FromMilliseconds(340);

            DoubleAnimation scaleX = new DoubleAnimation(0.92, 1, scaleDuration)
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
            };
            DoubleAnimation scaleY = scaleX.Clone();

            scaleX.Completed += (_, _) => FadeToGlass(entry);

            window.sctRoot.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            window.sctRoot.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            window.grdRoot.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private static void FadeToGlass(ToastEntry entry)
        {
            if (entry.IsClosing || !AcrylicBlur.TryEnable(new WindowInteropHelper(entry.Window).Handle))
                return;

            entry.IsGlass = true;
            entry.Window.bdSolid.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });
        }

        private static void BeginClose(ToastEntry entry)
        {
            if (entry.IsClosing)
                return;

            entry.IsClosing = true;
            entry.CloseTimer.Stop();

            if (openToasts.Remove(entry))
                ArrangeStack(null);

            if (!entry.IsGlass)
            {
                AnimatePopOut(entry);
                return;
            }

            // Back to the opaque surface first, the blur can't scale with the content
            DoubleAnimation solid = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            solid.Completed += (_, _) =>
            {
                AcrylicBlur.TryDisable(new WindowInteropHelper(entry.Window).Handle);
                entry.IsGlass = false;
                AnimatePopOut(entry);
            };

            entry.Window.bdSolid.BeginAnimation(UIElement.OpacityProperty, solid);
        }

        private static void AnimatePopOut(ToastEntry entry)
        {
            ToastMessage window = entry.Window;
            TimeSpan duration = TimeSpan.FromMilliseconds(160);
            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseIn };

            DoubleAnimation opacity = new DoubleAnimation { To = 0, Duration = duration, EasingFunction = ease };
            opacity.Completed += (_, _) => window.Close();

            window.sctRoot.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation { To = 0.96, Duration = duration, EasingFunction = ease });
            window.sctRoot.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation { To = 0.96, Duration = duration, EasingFunction = ease });
            window.grdRoot.BeginAnimation(UIElement.OpacityProperty, opacity);
        }

        /// <summary>App icon frame that fits the icon size at the window's DPI (the .ico holds one design per size range).</summary>
        private static ImageSource? GetIcon(Visual visual)
        {
            try
            {
                iconFrames ??= BitmapDecoder.Create(IconUri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames;

                double pixels = IconSize * VisualTreeHelper.GetDpi(visual).DpiScaleX;

                return iconFrames.Where(frame => frame.PixelWidth >= pixels).OrderBy(frame => frame.PixelWidth).FirstOrDefault()
                    ?? iconFrames.OrderByDescending(frame => frame.PixelWidth).FirstOrDefault();
            }
            catch (Exception ex)
            {
                FnLog.AddWarning(nameof(CFToast), "The app icon for the toast could not be loaded.", ex);
                return null;
            }
        }

        // --- Native ---

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int SW_SHOWNOACTIVATE = 4;

        private static void SetNoActivate(Window window)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            SetWindowLong(handle, GWL_EXSTYLE, GetWindowLong(handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
