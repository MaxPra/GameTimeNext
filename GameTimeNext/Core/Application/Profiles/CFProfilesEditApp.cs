using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Framework;
using System.IO;
using System.Windows.Controls.Primitives;

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
    }
}
