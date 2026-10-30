using System.Windows.Media;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Utils
{
    /// <summary>
    /// Colors of the application (UI/Colors/Colors.xaml) for code, e.g. row and cell colors of lists (UIXDataGridRow,
    /// UIXDataGridCell). Returns frozen brushes, so they can be used in any thread (selector adapters build their rows in
    /// Task.Run). Alpha replaces the alpha of the color (0xFF = full color, e.g. 0x1F for a light tint).
    /// The brushes are snapshots of the current theme: FnTheme clears the cache when the accent colors change, elements
    /// that must follow a later change keep using DynamicResource in XAML.
    /// </summary>
    public static class FnColor
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<(string ColorKey, byte Alpha), SolidColorBrush> Cache = new Dictionary<(string ColorKey, byte Alpha), SolidColorBrush>();

        // -- Accent (profile dependent)
        public static SolidColorBrush Accent(byte alpha = 0xFF) => Get("Color.Accent", alpha);
        public static SolidColorBrush AccentHover(byte alpha = 0xFF) => Get("Color.AccentHover", alpha);
        public static SolidColorBrush AccentPressed(byte alpha = 0xFF) => Get("Color.AccentPressed", alpha);

        // -- States
        public static SolidColorBrush Success(byte alpha = 0xFF) => Get("Color.Success", alpha);
        public static SolidColorBrush Warning(byte alpha = 0xFF) => Get("Color.Warning", alpha);
        public static SolidColorBrush Danger(byte alpha = 0xFF) => Get("Color.Danger", alpha);

        // -- Text
        public static SolidColorBrush TextPrimary(byte alpha = 0xFF) => Get("Color.TextPrimary", alpha);
        public static SolidColorBrush TextSecondary(byte alpha = 0xFF) => Get("Color.TextSecondary", alpha);
        public static SolidColorBrush TextDisabled(byte alpha = 0xFF) => Get("Color.TextDisabled", alpha);

        // -- Surfaces
        public static SolidColorBrush Background(byte alpha = 0xFF) => Get("Color.Background", alpha);
        public static SolidColorBrush Surface(byte alpha = 0xFF) => Get("Color.Surface", alpha);
        public static SolidColorBrush Border(byte alpha = 0xFF) => Get("Color.Border", alpha);

        // -- App Start
        public static SolidColorBrush AppStart(byte alpha = 0xFF) => Get("Color.AppStart", alpha);

        /// <summary>
        /// Frozen brush of the color resource with the given key (e.g. "Color.Danger"); transparent if there is none.
        /// </summary>
        public static SolidColorBrush Get(string colorKey, byte alpha = 0xFF)
        {
            lock (SyncRoot)
            {
                if (Cache.TryGetValue((colorKey, alpha), out SolidColorBrush? cached))
                    return cached;
            }

            Color color = GetColor(colorKey);

            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            brush.Freeze();

            lock (SyncRoot)
                Cache[(colorKey, alpha)] = brush;

            return brush;
        }

        /// <summary>
        /// Current value of the color resource with the given key; transparent if there is none.
        /// Resources belong to the UI thread: from another thread the value is read there (waits for the UI thread).
        /// </summary>
        public static Color GetColor(string colorKey)
        {
            System.Windows.Application? application = System.Windows.Application.Current;

            if (application == null)
                return Colors.Transparent;

            object? resource = application.Dispatcher.CheckAccess()
                ? application.TryFindResource(colorKey)
                : application.Dispatcher.Invoke(() => application.TryFindResource(colorKey));

            if (resource is Color color)
                return color;

            FnLog.AddWarning(nameof(FnColor), $"Color resource \"{colorKey}\" not found, transparent is used.");
            return Colors.Transparent;
        }

        /// <summary>
        /// Drops the cached brushes; called by FnTheme when the theme colors change.
        /// </summary>
        public static void ClearCache()
        {
            lock (SyncRoot)
                Cache.Clear();
        }
    }
}
