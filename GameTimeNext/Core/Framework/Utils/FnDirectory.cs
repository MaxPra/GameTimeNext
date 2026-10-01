using System.IO;

namespace GameTimeNext.Core.Framework.Utils
{
    public class FnDirectory
    {
        /// <summary>
        /// Kopiert ein Verzeichnis an den angegebenen Ort
        /// </summary>
        /// <param name="sourceDir">Zu kopierendes Verzeichnis</param>
        /// <param name="targetDir">Ziel Ordner</param>
        /// <param name="filesToSkip">Dateien die nicht kopiert werden sollen (Filename)</param>
        /// <param name="overwrite">Überschreiben bereits vorhandener Dateien</param>
        public static void CopyDirectory(string sourceDir, string targetDir, string[] filesToSkip, bool overwrite = true)
        {
            FnDirectory.EnsureDirectoryExists(targetDir);

            // Dateien kopieren
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (filesToSkip.Contains<string>(file))
                    continue;

                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, overwrite);
            }

            // Unterordner kopieren
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(directory);
                var destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(directory, destDir, filesToSkip, overwrite);
            }
        }

        public static void EnsureDirectoryExists(string path, bool requireParentExists = false)
        {
            if (!Directory.Exists(path))
            {
                DirectoryInfo? parentDirectoryInfo = new DirectoryInfo(path).Parent;
                if (!requireParentExists || (parentDirectoryInfo is not null && parentDirectoryInfo.Exists))
                    Directory.CreateDirectory(path);
            }
        }
    }
}
