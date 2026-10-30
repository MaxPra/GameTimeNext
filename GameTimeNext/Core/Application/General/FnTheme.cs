using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.Utils;
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

        private const string ColorAccent = "Color.Accent";
        private const string ColorAccentHover = "Color.AccentHover";
        private const string ColorAccentPressed = "Color.AccentPressed";

        /// <summary>
        /// Shared resources derived from the accent colors, with the accent color key each one follows.
        /// A DynamicResource inside a shared Freezable resource (brush in a ResourceDictionary) is resolved only once,
        /// so these keep the default accent unless they are replaced here. They must be referenced with DynamicResource:
        /// a StaticResource keeps the old instance (styles freeze the brushes of their setters, so they cannot be
        /// changed in place either). Every new accent-derived brush in a ResourceDictionary has to be added to this list.
        /// For gradient brushes the first gradient stop is the accent.
        /// </summary>
        private static readonly (string ResourceKey, string ColorKey)[] AccentResources =
        {
            ("Brush.Accent", ColorAccent),
            ("Brush.AccentHover", ColorAccentHover),
            ("Brush.AccentPressed", ColorAccentPressed),
            ("Brush.Accent.Subtle", ColorAccent),
            ("Brush.Accent.Muted", ColorAccent),
            ("Brush.Accent.Outline", ColorAccentHover),
            ("Brush.LoaderAccent", ColorAccent),
            ("Brush.LoaderAccentHover", ColorAccentHover),
            ("Brush.LoaderAccentPressed", ColorAccentPressed),
            ("GradientBrush.ProfileDetailView.AccentGlowPrimary", ColorAccent),
            ("GradientBrush.ProfileDetailView.AccentGlowSecondary", ColorAccentHover),
            ("GradientBrush.Window.AccentGlowPrimary", ColorAccent),
            ("GradientBrush.Window.AccentGlowSecondary", ColorAccentHover),
        };

        // Default accent colors from Colors.xaml, captured before they are overridden for the first time
        private static Dictionary<string, Color>? _defaultAccentColors;

        public static void ApplyThemeColors(Dictionary<string, string> dicAccentColors)
        {
            ApplyAccentColors(
                ToColor(dicAccentColors["accent"]),
                ToColor(dicAccentColors["hover"]),
                ToColor(dicAccentColors["pressed"]));
        }

        /// <summary>Resets the accent colors to the defaults defined in Colors.xaml.</summary>
        public static void ApplyDefaultTheme()
        {
            Dictionary<string, Color> defaults = GetDefaultAccentColors();
            ApplyAccentColors(defaults[ColorAccent], defaults[ColorAccentHover], defaults[ColorAccentPressed]);
        }

        /// <summary>
        /// Applies the accent colors of the profile if it uses its own accent colors and profile specific styles
        /// are allowed in the settings, otherwise the default accent colors (null for no profile).
        /// </summary>
        public static void ApplyProfileAccentColors(T1PROFI? t1profi)
        {
            Dictionary<string, string>? profileColors = null;

            if (t1profi != null
                && t1profi.ACAC
                && !FnString.IsNullEmptyOrWhitespace(t1profi.ACCO)
                && AppEnvironment.GetAppConfig().AppSettings.AllowProfileSpecificStyleChanges)
            {
                profileColors = new CAccentColors(t1profi.ACCO).Dezerialize()?.AccentColors;
            }

            if (profileColors != null
                && profileColors.ContainsKey("accent")
                && profileColors.ContainsKey("hover")
                && profileColors.ContainsKey("pressed"))
            {
                ApplyThemeColors(profileColors);
                return;
            }

            ApplyDefaultTheme();
        }

        private static void ApplyAccentColors(Color accent, Color hover, Color pressed)
        {
            GetDefaultAccentColors();

            var colors = new Dictionary<string, Color>
            {
                [ColorAccent] = accent,
                [ColorAccentHover] = hover,
                [ColorAccentPressed] = pressed,
            };

            // Every change of the application resources updates every element of every window (setting the resources
            // one by one took 50-90 ms per profile click in a large library), so unchanged colors are not set again
            // and all accent resources are swapped at once as one merged dictionary
            var resources = System.Windows.Application.Current.Resources;

            if (colors.All(c => resources[c.Key] is Color current && current == c.Value))
                return;

            var accentDictionary = new System.Windows.ResourceDictionary();

            // Color resources: for DynamicResource references in elements and templates
            foreach (var (key, color) in colors)
                accentDictionary[key] = color;

            foreach (var (resourceKey, colorKey) in AccentResources)
            {
                Brush? brush = CreateAccentResourceCopy(resourceKey, colors[colorKey]);

                if (brush != null)
                    accentDictionary[resourceKey] = brush;
            }

            int index = _accentDictionary == null ? -1 : resources.MergedDictionaries.IndexOf(_accentDictionary);

            if (index >= 0)
                resources.MergedDictionaries[index] = accentDictionary;
            else
                resources.MergedDictionaries.Add(accentDictionary);

            _accentDictionary = accentDictionary;

            // Brushes handed out for code are snapshots of the old colors
            FnColor.ClearCache();
        }

        // Accent resources of the current colors, last merged dictionary of the application (overrides Colors.xaml)
        private static System.Windows.ResourceDictionary? _accentDictionary;

        private static Dictionary<string, Color> GetDefaultAccentColors()
        {
            if (_defaultAccentColors != null)
                return _defaultAccentColors;

            var resources = System.Windows.Application.Current.Resources;

            _defaultAccentColors = new Dictionary<string, Color>
            {
                [ColorAccent] = (Color)resources[ColorAccent],
                [ColorAccentHover] = (Color)resources[ColorAccentHover],
                [ColorAccentPressed] = (Color)resources[ColorAccentPressed],
            };

            return _defaultAccentColors;
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

        // Opacity of the black tint layer of the windows with the setting "Dark tinted glass"
        private const double DarkGlassTintOpacity = 0.45;

        /// <summary>
        /// Setting "Dark tinted glass": darkens the window background and with it the translucent glass surfaces
        /// in all GTN windows (GlassTintLayer in GTNWindowStyle). Works at startup and at runtime.
        /// </summary>
        public static void ApplyGlassTint(bool darkTintedGlass)
        {
            System.Windows.Application.Current.Resources["Window.GlassTintOpacity"] = darkTintedGlass ? DarkGlassTintOpacity : 0d;
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

        /// <summary>
        /// Copy of the accent brush in the new color (null if the resource is no brush); opacity and the other gradient
        /// stops are kept. Frozen: styles reference it with DynamicResource.
        /// </summary>
        private static Brush? CreateAccentResourceCopy(string key, Color color)
        {
            Brush? copy = null;

            switch (System.Windows.Application.Current.Resources[key])
            {
                case SolidColorBrush solidBrush:
                    SolidColorBrush solidCopy = solidBrush.CloneCurrentValue();
                    solidCopy.Color = color;
                    copy = solidCopy;
                    break;

                case GradientBrush gradientBrush when gradientBrush.GradientStops.Count > 0:
                    GradientBrush gradientCopy = gradientBrush.CloneCurrentValue();
                    gradientCopy.GradientStops[0].Color = color;
                    copy = gradientCopy;
                    break;
            }

            copy?.Freeze();

            return copy;
        }

        private static Color ToColor(string colorHex)
        {
            return (Color)ColorConverter.ConvertFromString(colorHex);
        }
    }
}
