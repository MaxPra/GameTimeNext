using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UIX.ViewController.Engine.Utils;
using UIXMonitorTweaks;

namespace GameTimeNext.Core.Application.Screensetups
{
    public class CFScreensetupsApp
    {
        // COM based: created lazily and only used on the UI thread
        private static UIXAudioManager? _audioManager;

        public static UIXAudioManager GetAudioManager()
        {
            _audioManager ??= new UIXAudioManager();
            return _audioManager;
        }

        /// <summary>
        /// Applies the monitors and the audio device of a screensetup. Returns an error message or null on success.
        /// </summary>
        public static async Task<string?> ApplyScreensetupAsync(T1SCRSH t1scrsh)
        {
            List<T1SCRSP> t1scrsps = TFSCRSP.GetMonitorsOfScreensetup(t1scrsh.SSID);

            // Monitors that are not connected right now cannot be switched
            HashSet<string> connectedMonitorIds = UIXMonitorManager.GetMonitors().Select(m => m.Id).ToHashSet();

            List<UIXMonitorTarget> targets = t1scrsps
                .Where(p => connectedMonitorIds.Contains(p.MOID))
                .Select(p => new UIXMonitorTarget(p.MOID, p.MOEN, p.MOEN && p.MOPR))
                .ToList();

            if (!targets.Any(t => t.Enabled))
                return "None of the monitors enabled in this screensetup is connected.";

            try
            {
                await Task.Run(() => UIXMonitorManager.Apply(targets));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                // The library message names the real cause (e.g. open display settings)
                return ex.Message;
            }

            // Audio after the monitors: an HDMI output only exists once its monitor is on
            if (!FnString.IsNullEmptyOrWhitespace(t1scrsh.ADID) && !GetAudioManager().SetDefaultPlaybackDevice(t1scrsh.ADID))
                return $"The monitors have been switched, but the audio device \"{t1scrsh.ADNA}\" could not be set as default.";

            AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup = t1scrsh.SSID;
            AppEnvironment.SaveAppConfig();

            return null;
        }

        public static string FormatMonitors(List<T1SCRSP> t1scrsps)
        {
            List<string> monitorNames = t1scrsps
                .Where(p => p.MOEN)
                .Select(p => p.MOPR ? $"{p.MONA} (Primary)" : p.MONA)
                .ToList();

            return monitorNames.Count == 0 ? "-" : string.Join(", ", monitorNames);
        }

        public static string FormatAudioDevice(T1SCRSH t1scrsh)
        {
            return FnString.IsNullEmptyOrWhitespace(t1scrsh.ADID) ? "(Unchanged)" : t1scrsh.ADNA;
        }

        /// <summary>
        /// Identify overlays with the glass look of the toast notifications: the library's default content
        /// on a glass panel with acrylic blur behind the overlay window. Must be called on the UI thread.
        /// </summary>
        public static UIXIdentifyOptions CreateIdentifyOptions()
        {
            // The overlays run on their own UI thread: only frozen brushes and plain values may be captured
            Brush glass = GetFrozenBrush("Toast.GlassBrush");
            Brush opaque = GetFrozenBrush("Toast.BackgroundBrush");
            Brush sheen = GetFrozenBrush("Toast.SheenBrush");
            Brush border = GetFrozenBrush("Brush.Popup.GlassBorder");
            Color accent = ((SolidColorBrush)GetFrozenBrush("Brush.Accent")).Color;
            Color textPrimary = ((SolidColorBrush)GetFrozenBrush("Brush.TextPrimary")).Color;

            // The blur is rounded with the system radius, so the panel uses the same radius as popups and toasts
            CornerRadius cornerRadius = (CornerRadius)System.Windows.Application.Current.FindResource("Popup.CornerRadius");

            return new UIXIdentifyOptions
            {
                // The library card stays as it is (layout, badge, name), only its own background and shadow give way to the glass
                CardBackground = Colors.Transparent,
                CornerRadius = cornerRadius.TopLeft,
                DropShadow = false,
                BadgeBackground = accent,
                Foreground = textPrimary,
                ContentFactory = context =>
                {
                    // Sheen on top like ToastMessage (ToastSheenStyle), the library content above it
                    Grid layers = new Grid();
                    layers.Children.Add(new Border { Background = sheen, CornerRadius = cornerRadius, IsHitTestVisible = false });
                    layers.Children.Add(UIXIdentifyOptions.CreateDefaultContent(context));

                    Border panel = new Border
                    {
                        Background = opaque,
                        BorderBrush = border,
                        BorderThickness = new Thickness(1),
                        CornerRadius = cornerRadius,
                        SnapsToDevicePixels = true,
                        Child = layers
                    };

                    // Same as ToastMessage: acrylic blur behind the (transparent) overlay window, translucent tint only if it is active
                    panel.Loaded += (_, _) =>
                    {
                        Window? overlayWindow = Window.GetWindow(panel);
                        if (overlayWindow is null)
                            return;

                        // The library puts a 30px margin around the content for its drop shadow (disabled here). The blur
                        // fills the whole window, so without this the panel would sit as a second box inside the blurred area.
                        panel.Margin = new Thickness(0);

                        AcrylicBlur.SetIsEnabled(overlayWindow, true);
                        if (AcrylicBlur.GetIsActive(overlayWindow))
                            panel.Background = glass;
                    };

                    return panel;
                }
            };
        }

        private static Brush GetFrozenBrush(string resourceKey)
        {
            // Frozen copy of the current value: usable from other threads (resources with DynamicResource colors are not frozen)
            Brush brush = ((Brush)System.Windows.Application.Current.FindResource(resourceKey)).CloneCurrentValue();
            brush.Freeze();
            return brush;
        }
    }
}
