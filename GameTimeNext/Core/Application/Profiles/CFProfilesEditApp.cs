using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework;
using System.IO;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles
{
    /// <summary>
    /// Funktionsklasse für ProfilesEditApp
    /// </summary>
    public class CFProfilesEditApp
    {

        /// <summary>
        /// Kopiert das übergebene Cover zum App Coverordner
        /// </summary>
        /// <param name="currentPath"></param>
        public static void CopyProfileCoverToAppCoverFolder(string currentPath, string destFileName)
        {
            File.Copy(currentPath, AppEnvironment.GetAppConfig().CoverFolderPath + Path.DirectorySeparatorChar + destFileName);
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
