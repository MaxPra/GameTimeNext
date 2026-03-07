using System.Windows.Media;

namespace GameTimeNext.Core.Application.General
{
    public class FnTheme
    {
        public static string[] CalculateAccentStateColors(string accentColorStart)
        {
            if (string.IsNullOrWhiteSpace(accentColorStart))
                throw new ArgumentException("Accent color is empty.", nameof(accentColorStart));

            if (accentColorStart.StartsWith("#"))
                accentColorStart = accentColorStart.Substring(1);

            // Unterstützt RRGGBB und AARRGGBB
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