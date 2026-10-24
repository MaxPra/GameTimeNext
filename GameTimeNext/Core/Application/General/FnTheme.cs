using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI;
using System.Windows.Media;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.General
{
    public class FnTheme
    {
        public static string[] CalculateAccentStateColors(string accentColorStart)
        {
            if (FnString.IsNullEmptyOrWhitespace(accentColorStart))
                throw new ArgumentException("Accent color is empty.", nameof(accentColorStart));

            if (accentColorStart.StartsWith("#"))
                accentColorStart = accentColorStart.Substring(1);

            // Supports RRGGBB and AARRGGBB
            if (accentColorStart.Length == 8)
                accentColorStart = accentColorStart.Substring(2);

            if (accentColorStart.Length != 6)
                throw new ArgumentException("Accent color must be in #RRGGBB or #AARRGGBB format.", nameof(accentColorStart));

            int r = Convert.ToInt32(accentColorStart.Substring(0, 2), 16);
            int g = Convert.ToInt32(accentColorStart.Substring(2, 2), 16);
            int b = Convert.ToInt32(accentColorStart.Substring(4, 2), 16);

            int rHover = Math.Min(255, (int)Math.Round(r * 1.20));
            int gHover = Math.Min(255, (int)Math.Round(g * 1.20));
            int bHover = Math.Min(255, (int)Math.Round(b * 1.20));

            int rPressed = Math.Max(0, (int)Math.Round(r * 0.75));
            int gPressed = Math.Max(0, (int)Math.Round(g * 0.75));
            int bPressed = Math.Max(0, (int)Math.Round(b * 0.75));

            string accent = $"#{r:X2}{g:X2}{b:X2}";
            string hover = $"#{rHover:X2}{gHover:X2}{bHover:X2}";
            string pressed = $"#{rPressed:X2}{gPressed:X2}{bPressed:X2}";

            return new[] { accent, hover, pressed };
        }

        public static void ApplyThemeColors(Dictionary<string, string> dicAccentColors)
        {
            SetColorResource("Color.Accent", dicAccentColors["accent"]);
            SetColorResource("Color.AccentHover", dicAccentColors["hover"]);
            SetColorResource("Color.AccentPressed", dicAccentColors["pressed"]);

            SetBrushColor("Brush.Accent", dicAccentColors["accent"]);
            SetBrushColor("Brush.AccentHover", dicAccentColors["hover"]);
            SetBrushColor("Brush.AccentPressed", dicAccentColors["pressed"]);
        }

        public static void ApplyDefaultTheme()
        {
            SetColorResource("Color.Accent", "#3498e5");
            SetColorResource("Color.AccentHover", "#1a7fcb");
            SetColorResource("Color.AccentPressed", "#15629e");

            SetBrushColor("Brush.Accent", "#3498e5");
            SetBrushColor("Brush.AccentHover", "#1a7fcb");
            SetBrushColor("Brush.AccentPressed", "#15629e");
        }

        /// <summary>
        /// Switches the blurred background image as base of the main window background (setting BackgroundSource,
        /// codetable bG): the desktop wallpaper, the cover of the selected profile, or none for an empty source.
        /// Works at startup (before the main window exists) and at runtime.
        /// </summary>
        public static void ApplyWallpaperBackground(string backgroundSource)
        {
            var app = System.Windows.Application.Current;

            bool enabled = IsDynamicBackgroundEnabled(backgroundSource);
            bool useProfileCover = enabled && backgroundSource == BackgroundSource.PROFILE;
            bool useDesktopWallpaper = enabled && !useProfileCover;

            // Both share one resource: disable first, so the newly enabled source isn't removed afterwards
            if (!useDesktopWallpaper)
                DesktopWallpaper.SetEnabled(app, false);

            if (!useProfileCover)
                ProfileCoverBackground.SetEnabled(app, false);

            DesktopWallpaper.SetEnabled(app, useDesktopWallpaper);
            ProfileCoverBackground.SetEnabled(app, useProfileCover);

            // Without dynamic background the main window is translucent (acrylic)
            foreach (MainWindow mainWindow in app.Windows.OfType<MainWindow>())
            {
                mainWindow.UseWallpaperBackground = enabled;
                mainWindow.UseBackdropBlur = !enabled;
            }
        }

        /// <summary>True if a background source is set (an empty source disables the dynamic background).</summary>
        public static bool IsDynamicBackgroundEnabled(string backgroundSource)
        {
            return !FnString.IsNullEmptyOrWhitespace(backgroundSource);
        }

        /// <summary>
        /// Passes the cover of the selected profile (null for none) to the profile background
        /// (only visible with BackgroundSource PROFILE).
        /// </summary>
        public static void ApplyProfileBackground(T1PROFI? t1profi)
        {
            ProfileCoverBackground.SetCover(System.Windows.Application.Current, t1profi?.PPFN ?? string.Empty);
        }

        private static void SetBrushColor(string key, string colorHex)
        {
            Color color = (Color)ColorConverter.ConvertFromString(colorHex);
            var resources = System.Windows.Application.Current.Resources;

            if (resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
                return;
            }

            resources[key] = new SolidColorBrush(color);
        }

        private static void SetColorResource(string key, string colorHex)
        {
            Color color = (Color)ColorConverter.ConvertFromString(colorHex);
            System.Windows.Application.Current.Resources[key] = color;
        }
    }
}
