using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework.Config;
using System.IO;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles
{
    /// <summary>
    /// Funktionsklasse für ProfilesEditApp
    /// </summary>
    public class CFProfilesEditApp
    {
        // Covers are displayed at max. 300x450, so 600x900 still looks sharp on high DPI screens
        public const int ProfileCoverMaxWidth = 600;
        public const int ProfileCoverMaxHeight = 900;
        public const int ProfileCoverJpegQuality = 85;

        /// <summary>
        /// Saves a profile cover as JPEG, downscaled (never upscaled) to the max. cover size.
        /// Use this for every cover that ends up in the profile covers folder.
        /// </summary>
        public static void SaveProfileCover(BitmapSource source, string path)
        {
            FnImage.SaveAsJpeg(FnImage.DownscaleToFit(source, ProfileCoverMaxWidth, ProfileCoverMaxHeight), path, ProfileCoverJpegQuality);
        }

        /// <summary>
        /// Kopiert das übergebene Cover zum App Coverordner
        /// </summary>
        /// <param name="currentPath"></param>
        public static void CopyProfileCoverToAppCoverFolder(string currentPath, string destFileName)
        {
            File.Copy(currentPath, Path.Join(AppConfig.Storage.ProfileCoversDirectoryPath, destFileName));
        }


        /// <summary>
        /// Gibt eine neue Guid für den Namen des Covers zurück (ink. Dateiendung)
        /// </summary>
        /// <returns></returns>
        public static string GetGUIDCoverName(string fileEnding)
        {
            string base64 = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                            .Replace("/", "_")
                            .Replace("+", "-")
                            .TrimEnd('=');

            return base64 + $".{fileEnding}";
        }

        public static ToggleButton? GetSelectedToggleButton(ProfilesEditView editView)
        {
            if (editView.tglAccent1.IsChecked == true)
                return editView.tglAccent1;
            else if (editView.tglAccent2.IsChecked == true)
                return editView.tglAccent2;
            else if (editView.tglAccent3.IsChecked == true)
                return editView.tglAccent3;

            return null;
        }

        /// <summary>
        /// Opens the HowLongToBeat website in the default browser with a search for the specified game profile name.
        /// </summary>
        /// <remarks>If the specified profile name is null, empty, or white space, the method does
        /// nothing. The method attempts to launch the default web browser using the operating system shell.</remarks>
        /// <param name="profileName">The name of the game profile to search for on HowLongToBeat. Cannot be null, empty, or consist only of
        /// white-space characters.</param>
        public static void OpenHowLongToBeatForGame(string profileName)
        {
            if (FnString.IsNullEmptyOrWhitespace(profileName))
                return;

            string url = $"https://howlongtobeat.com/?q={Uri.EscapeDataString(profileName)}";
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
        }
    }
}
