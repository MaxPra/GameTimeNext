using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UIX.ViewController.Engine.Runnables;
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

        // Like the Windows display settings
        private static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Applies the monitors and the audio device of a screensetup on probation and asks to keep them; without a confirmation
        /// within 10 seconds the library restores the display settings (and the audio device) from before. Shows the loader of
        /// the application while switching. Returns an error message or null on success.
        /// </summary>
        public static async Task<string?> ApplyScreensetupAsync(UIXApplication app, T1SCRSH t1scrsh)
        {
            List<UIXMonitorTarget> targets = CreateTargets(t1scrsh);

            if (!targets.Any(t => t.Enabled))
                return app.GetErrorMessage("SCS0004");

            string? audioDeviceId = FnString.IsNullEmptyOrWhitespace(t1scrsh.ADID) ? null : t1scrsh.ADID;

            UIXTentativeChange change;

            app.Loader.Begin($"Switching to {t1scrsh.SSNA}...");

            try
            {
                change = await Task.Run(() => UIXMonitorManager.ApplyTentatively(targets, ConfirmationTimeout, audioDeviceId));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                // The library message names the real cause (e.g. open display settings); it reverted already
                return ex.Message;
            }
            finally
            {
                app.Loader.Stop();
            }

            // Dispose reverts a change that is still pending (e.g. after an exception here)
            using (change)
            {
                // Nothing changed on the monitors (e.g. only the audio device): nothing to confirm
                if (!change.HasChanges || (ConfirmDisplaySettings(app, change) && change.Keep()))
                {
                    if (!change.IsAudioDeviceSet)
                        return app.GetErrorMessage("SCS0005", t1scrsh.ADNA);

                    AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup = t1scrsh.SSID;
                    AppEnvironment.SaveAppConfig();

                    // The screensetup is active, only some display modes were skipped (e.g. no longer supported)
                    return change.Result.IsComplete ? null : app.GetErrorMessage("SCS0006", string.Join(Environment.NewLine, change.Result.Warnings));
                }

                UIXApplyResult restoreResult;

                app.Loader.Begin("Restoring the previous display settings...");

                try
                {
                    // Waits for the revert the library may have started already (timeout)
                    restoreResult = await Task.Run(change.Revert);
                }
                finally
                {
                    app.Loader.Stop();
                }

                return restoreResult.IsComplete
                    ? app.GetErrorMessage("SCS0009")
                    : app.GetErrorMessage("SCS0010", string.Join(Environment.NewLine, restoreResult.Warnings));
            }
        }

        private static bool ConfirmDisplaySettings(UIXApplication app, UIXTentativeChange change)
        {
            // Ends together with the timeout of the library
            CFMBOXTimeout timeout = new CFMBOXTimeout(change.RemainingTime ?? ConfirmationTimeout, CFMBOXResult.No,
                remainingSeconds => app.GetErrorMessage("SCS0008", remainingSeconds));

            // Only "Yes" keeps them: No, Esc, closing the message and the timeout revert
            return app.GetApplication<CFMBOX>(UIXApplicationStartTarget.Window)
                .Show(app.GetErrorMessage("SCS0007"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question, timeout) == CFMBOXResult.Yes;
        }

        /// <summary>
        /// Desired state of the connected monitors of a screensetup; monitors that are not connected right now cannot be switched.
        /// </summary>
        private static List<UIXMonitorTarget> CreateTargets(T1SCRSH t1scrsh)
        {
            HashSet<string> connectedMonitorIds = UIXMonitorManager.GetMonitors().Select(m => m.Id).ToHashSet();

            // A display mode of 0 lets Windows choose
            return TFSCRSP.GetMonitorsOfScreensetup(t1scrsh.SSID)
                .Where(p => connectedMonitorIds.Contains(p.MOID))
                .Select(p => new UIXMonitorTarget(p.MOID, p.MOEN, p.MOEN && p.MOPR)
                {
                    Width = p.MOWI > 0 && p.MOHE > 0 ? (int)p.MOWI : null,
                    Height = p.MOWI > 0 && p.MOHE > 0 ? (int)p.MOHE : null,
                    RefreshRate = p.MORF > 0 ? (int)p.MORF : null,
                    Orientation = ToOrientation(p.MOOR),
                    Placement = ToPlacement(p.MOPL),
                    ScalePercent = p.MOSC > 0 ? (int)p.MOSC : null
                })
                .ToList();
        }

        public static string FormatMonitors(List<T1SCRSP> t1scrsps)
        {
            List<string> monitorNames = t1scrsps
                .Where(p => p.MOEN)
                .Select(FormatMonitor)
                .ToList();

            return monitorNames.Count == 0 ? "-" : string.Join(", ", monitorNames);
        }

        private static string FormatMonitor(T1SCRSP t1scrsp)
        {
            List<string> details = new List<string>();

            if (t1scrsp.MOPR)
                details.Add("Primary");

            if (t1scrsp.MOWI > 0 || t1scrsp.MORF > 0 || !FnString.IsNullEmptyOrWhitespace(t1scrsp.MOOR) || t1scrsp.MOSC > 0)
                details.Add(FormatMode(t1scrsp.MOWI, t1scrsp.MOHE, t1scrsp.MORF, t1scrsp.MOOR, t1scrsp.MOSC));

            if (!t1scrsp.MOPR && !FnString.IsNullEmptyOrWhitespace(t1scrsp.MOPL))
                details.Add(FormatPlacement(t1scrsp.MOPL));

            return details.Count == 0 ? t1scrsp.MONA : $"{t1scrsp.MONA} ({string.Join(", ", details)})";
        }

        /// <summary>
        /// Display mode of a screensetup monitor; 0 / an empty orientation (codetable mO) stand for "Windows chooses" / "unchanged".
        /// </summary>
        public static string FormatMode(long width, long height, long refreshRate, string orientationKey = "", long scalePercent = 0)
        {
            string resolution = width > 0 && height > 0 ? FormatResolution(width, height) : "";
            string refresh = refreshRate > 0 ? FormatRefreshRate(refreshRate) : "";

            List<string> parts = new List<string>();

            string mode = resolution.Length > 0 && refresh.Length > 0 ? $"{resolution} @ {refresh}" : resolution + refresh;
            if (mode.Length > 0)
                parts.Add(mode);

            if (!FnString.IsNullEmptyOrWhitespace(orientationKey))
                parts.Add(TFCTABD.GetDescription("mO", orientationKey));

            if (scalePercent > 0)
                parts.Add(FormatScale(scalePercent));

            return parts.Count > 0 ? string.Join(", ", parts) : "Automatic";
        }

        public static string FormatScale(long scalePercent)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{scalePercent} %");
        }

        /// <summary>
        /// Placement of a screensetup monitor (codetable mP); empty = unchanged.
        /// </summary>
        public static string FormatPlacement(string placementKey)
        {
            return FnString.IsNullEmptyOrWhitespace(placementKey) ? "Unchanged" : TFCTABD.GetDescription("mP", placementKey);
        }

        public static UIXDisplayOrientation? ToOrientation(string orientationKey)
        {
            return orientationKey switch
            {
                "LAND" => UIXDisplayOrientation.Landscape,
                "PORT" => UIXDisplayOrientation.Portrait,
                "LANDF" => UIXDisplayOrientation.LandscapeFlipped,
                "PORTF" => UIXDisplayOrientation.PortraitFlipped,
                _ => null
            };
        }

        public static string GetOrientationKey(UIXDisplayOrientation orientation)
        {
            return orientation switch
            {
                UIXDisplayOrientation.Portrait => "PORT",
                UIXDisplayOrientation.LandscapeFlipped => "LANDF",
                UIXDisplayOrientation.PortraitFlipped => "PORTF",
                _ => "LAND"
            };
        }

        public static UIXMonitorPlacement? ToPlacement(string placementKey)
        {
            return placementKey switch
            {
                "LEFT" => UIXMonitorPlacement.LeftOfPrimary,
                "RIGHT" => UIXMonitorPlacement.RightOfPrimary,
                "ABOVE" => UIXMonitorPlacement.AbovePrimary,
                "BELOW" => UIXMonitorPlacement.BelowPrimary,
                _ => null
            };
        }

        /// <summary>
        /// Codetable key (mP) of a placement reported by the library; empty for the primary monitor and monitors that are off.
        /// </summary>
        public static string GetPlacementKey(UIXMonitorPlacement? placement)
        {
            return placement switch
            {
                UIXMonitorPlacement.LeftOfPrimary => "LEFT",
                UIXMonitorPlacement.RightOfPrimary => "RIGHT",
                UIXMonitorPlacement.AbovePrimary => "ABOVE",
                UIXMonitorPlacement.BelowPrimary => "BELOW",
                _ => string.Empty
            };
        }

        public static string FormatResolution(long width, long height)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{width} x {height}");
        }

        public static string FormatRefreshRate(long refreshRate)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{refreshRate} Hz");
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
