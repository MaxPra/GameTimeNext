using GameTimeNext.Core.Framework.Config;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Provides the current desktop wallpaper, heavily blurred, as application resource (Brush.App.Wallpaper)
    /// for window backgrounds (GTNWindow.UseWallpaperBackground). Follows wallpaper changes at runtime.
    /// Switched by the setting AppSettings.BackgroundSource (see FnTheme.ApplyWallpaperBackground).
    /// Without a wallpaper (solid color, unreadable file) the resource is missing and windows keep their normal background.
    /// </summary>
    public static class DesktopWallpaper
    {
        public const string ResourceKey = "Brush.App.Wallpaper";

        /// <summary>The wallpaper is decoded small: it gets blurred anyway, and it keeps loading and memory cheap.</summary>
        internal const int DecodePixelWidth = 640;

        /// <summary>Blur radius relative to the decoded size (640px), strong enough that only colors and shapes remain.</summary>
        private const double BlurRadius = 40;

        private static bool _isEnabled;
        private static bool _isListening;

        /// <summary>
        /// Enabled: loads the wallpaper into the application resources and follows wallpaper changes.
        /// Disabled: removes it again (windows fall back to their normal background).
        /// </summary>
        public static void SetEnabled(System.Windows.Application app, bool enabled)
        {
            if (enabled == _isEnabled)
                return;

            _isEnabled = enabled;

            if (!enabled)
            {
                app.Resources.Remove(ResourceKey);
                return;
            }

            Apply(app);

            if (_isListening)
                return;

            _isListening = true;

            SystemEvents.UserPreferenceChanged += (sender, e) =>
            {
                if (_isEnabled && e.Category == UserPreferenceCategory.Desktop)
                    app.Dispatcher.BeginInvoke(() => Apply(app));
            };
        }

        private static void Apply(System.Windows.Application app)
        {
            try
            {
                BitmapSource? blurred = LoadBlurredWallpaper();

                if (blurred == null)
                {
                    app.Resources.Remove(ResourceKey);
                    return;
                }

                var brush = new ImageBrush(blurred) { Stretch = Stretch.UniformToFill };
                brush.Freeze();

                app.Resources[ResourceKey] = brush;
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", "Desktop wallpaper could not be loaded as background", ex);
                app.Resources.Remove(ResourceKey);
            }
        }

        private static BitmapSource? LoadBlurredWallpaper()
        {
            string? path = GetWallpaperFilePath();

            if (path == null)
                return null;

            BitmapImage wallpaper = new BitmapImage();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                wallpaper.BeginInit();
                wallpaper.StreamSource = stream;
                wallpaper.DecodePixelWidth = DecodePixelWidth;
                // Loaded from a stream (not a Uri), so WPF's image cache doesn't hand out an old wallpaper after a change
                wallpaper.CacheOption = BitmapCacheOption.OnLoad;
                wallpaper.EndInit();
            }

            wallpaper.Freeze();

            return Blur(wallpaper);
        }

        /// <summary>
        /// Blurs the image (radius in pixels of the source, default BlurRadius). It is drawn larger than the result
        /// (by the blur radius on each side), so the edges don't fade into transparency; the overhang is cut off.
        /// </summary>
        internal static BitmapSource Blur(BitmapSource source, double radius = BlurRadius)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            double overhang = radius;

            var image = new Image
            {
                Source = source,
                Stretch = Stretch.Fill,
                Width = width + 2 * overhang,
                Height = height + 2 * overhang,
                Effect = new BlurEffect
                {
                    Radius = radius,
                    KernelType = KernelType.Gaussian,
                    RenderingBias = RenderingBias.Quality
                }
            };

            var host = new Canvas { Width = width, Height = height };
            Canvas.SetLeft(image, -overhang);
            Canvas.SetTop(image, -overhang);
            host.Children.Add(image);

            host.Measure(new Size(width, height));
            host.Arrange(new Rect(0, 0, width, height));
            host.UpdateLayout();

            var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            target.Render(host);
            target.Freeze();

            return target;
        }

        /// <summary>
        /// Wallpaper file from the Windows setting; if that doesn't point to a file (slideshow, Spotlight, removed file),
        /// the copy Windows keeps of the current wallpaper. Null without wallpaper.
        /// </summary>
        private static string? GetWallpaperFilePath()
        {
            var buffer = new StringBuilder(MaxPath);

            if (SystemParametersInfo(SPI_GETDESKWALLPAPER, (uint)buffer.Capacity, buffer, 0))
            {
                string setting = buffer.ToString();

                if (!FnString.IsNullEmptyOrWhitespace(setting) && File.Exists(setting))
                    return setting;
            }

            string transcoded = AppConfig.Windows.TranscodedWallpaperFilePath;

            return File.Exists(transcoded) ? transcoded : null;
        }

        // --- Native ---

        private const uint SPI_GETDESKWALLPAPER = 0x0073;
        private const int MaxPath = 260;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(uint action, uint param, StringBuilder value, uint winIni);
    }
}
