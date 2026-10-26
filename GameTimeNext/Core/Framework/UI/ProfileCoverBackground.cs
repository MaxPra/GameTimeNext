using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Framework.Config;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Provides the cover of the selected profile, cropped to landscape and heavily blurred, as application resource
    /// (same key as DesktopWallpaper) for window backgrounds (GTNWindow.UseWallpaperBackground).
    /// Switched by the setting AppSettings.BackgroundSource (see FnTheme.ApplyWallpaperBackground), the cover follows
    /// the profile selection (FnTheme.ApplyProfileBackground). Without a cover the resource is missing and windows keep
    /// their normal background.
    /// </summary>
    public static class ProfileCoverBackground
    {
        /// <summary>Aspect ratio (width / height) the portrait cover is cropped to before blurring.</summary>
        private const double TargetAspectRatio = 16.0 / 9.0;

        private static bool _isEnabled;
        private static string _coverFileName = string.Empty;

        /// <summary>
        /// Enabled: loads the cover of the current profile into the application resources.
        /// Disabled: removes it again (windows fall back to their normal background).
        /// </summary>
        public static void SetEnabled(System.Windows.Application app, bool enabled)
        {
            if (enabled == _isEnabled)
                return;

            _isEnabled = enabled;

            if (!enabled)
            {
                app.Resources.Remove(DesktopWallpaper.ResourceKey);
                return;
            }

            Apply(app);
        }

        /// <summary>
        /// Sets the cover file (T1PROFI.PPFN) of the selected profile; empty for no profile.
        /// Is remembered while disabled, so enabling later shows the current profile.
        /// </summary>
        public static void SetCover(System.Windows.Application app, string coverFileName)
        {
            coverFileName ??= string.Empty;

            if (coverFileName == _coverFileName)
                return;

            _coverFileName = coverFileName;

            if (_isEnabled)
                Apply(app);
        }

        private static void Apply(System.Windows.Application app)
        {
            try
            {
                BitmapSource? blurred = LoadBlurredCover();

                if (blurred == null)
                {
                    app.Resources.Remove(DesktopWallpaper.ResourceKey);
                    return;
                }

                var brush = new ImageBrush(blurred) { Stretch = Stretch.UniformToFill };
                brush.Freeze();

                app.Resources[DesktopWallpaper.ResourceKey] = brush;
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", "Profile cover could not be loaded as background", ex);
                app.Resources.Remove(DesktopWallpaper.ResourceKey);
            }
        }

        private static BitmapSource? LoadBlurredCover()
        {
            if (FnString.IsNullEmptyOrWhitespace(_coverFileName))
                return null;

            return DesktopWallpaper.Blur(CropToLandscape(LoadCover()));
        }

        /// <summary>
        /// Cover of the selected profile, decoded small; the placeholder cover if the file is missing or can't be read.
        /// </summary>
        private static BitmapSource LoadCover()
        {
            string path = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath, _coverFileName);

            try
            {
                BitmapImage cover = new BitmapImage();

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    cover.BeginInit();
                    cover.StreamSource = stream;
                    cover.DecodePixelWidth = DesktopWallpaper.DecodePixelWidth;
                    cover.CacheOption = BitmapCacheOption.OnLoad;
                    cover.EndInit();
                }

                cover.Freeze();

                return cover;
            }
            catch (Exception ex)
            {
                FnLog.AddWarning("MainApp", $"Cover \"{_coverFileName}\" could not be loaded as background, the placeholder cover is used instead", ex);

                // Same 2:3 ratio as the placeholder
                return CFProfilesApp.LoadPlaceholderCover(DesktopWallpaper.DecodePixelWidth, DesktopWallpaper.DecodePixelWidth * 3 / 2);
            }
        }

        /// <summary>
        /// Cuts the centered landscape section (TargetAspectRatio) out of the cover, so the portrait image is not
        /// distorted or blown up by the window's aspect ratio.
        /// </summary>
        private static BitmapSource CropToLandscape(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;

            if (width <= 0 || height <= 0)
                return source;

            Int32Rect section;

            if ((double)width / height < TargetAspectRatio)
            {
                int croppedHeight = Math.Max(1, (int)Math.Round(width / TargetAspectRatio));
                section = new Int32Rect(0, (height - croppedHeight) / 2, width, croppedHeight);
            }
            else
            {
                int croppedWidth = Math.Max(1, (int)Math.Round(height * TargetAspectRatio));
                section = new Int32Rect((width - croppedWidth) / 2, 0, croppedWidth, height);
            }

            var cropped = new CroppedBitmap(source, section);
            cropped.Freeze();

            return cropped;
        }
    }
}
