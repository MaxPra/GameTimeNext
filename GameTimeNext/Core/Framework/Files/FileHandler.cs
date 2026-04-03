using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using Microsoft.VisualBasic.FileIO;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;

namespace GameTimeNext.Core.Framework.Files
{
    internal class FileHandler
    {
        public static void CreateApplicationFoldersAndFiles()
        {

            CreateDevAppFolder();

            if (!Directory.Exists(AppEnvironment.GetAppConfig().AppFolderPath))
            {
                Directory.CreateDirectory(AppEnvironment.GetAppConfig().AppFolderPath);
            }

            if (!Directory.Exists(AppEnvironment.GetAppConfig().DataFolderPath))
            {
                Directory.CreateDirectory(AppEnvironment.GetAppConfig().DataFolderPath);
            }

            if (!Directory.Exists(AppEnvironment.GetAppConfig().CoverFolderPath))
            {
                Directory.CreateDirectory(AppEnvironment.GetAppConfig().CoverFolderPath);
            }

            if (!Directory.Exists(AppEnvironment.GetAppConfig().CoverFolderTempPath))
            {
                Directory.CreateDirectory(AppEnvironment.GetAppConfig().CoverFolderTempPath);
            }

            if (!File.Exists(AppEnvironment.GetAppConfig().AppConfigPath))
            {
                AppConfig appConfig = new AppConfig();

                File.WriteAllText(AppEnvironment.GetAppConfig().AppConfigPath, JsonSerializer.Serialize(appConfig));
            }
        }

        public static void CopyDirectory(string sourceDirectory, string targetDirectory, bool overwriteFiles)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException("Quellordner nicht gefunden: " + sourceDirectory);
            }

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Dateien kopieren
            string[] files = Directory.GetFiles(sourceDirectory);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string targetFilePath = Path.Combine(targetDirectory, fileName);

                File.Copy(file, targetFilePath, overwriteFiles);
            }

            // Unterordner rekursiv kopieren
            string[] directories = Directory.GetDirectories(sourceDirectory);

            foreach (string directory in directories)
            {
                string directoryName = Path.GetFileName(directory);
                string targetSubDirectory = Path.Combine(targetDirectory, directoryName);

                CopyDirectory(directory, targetSubDirectory, overwriteFiles);
            }
        }

        public static void ConvertToJpeg(string sourcePath, string targetPath, long quality = 90L)
        {
            using Image image = Image.FromFile(sourcePath);

            ImageCodecInfo jpgEncoder = ImageCodecInfo.GetImageDecoders()
                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            image.Save(targetPath, jpgEncoder, encoderParams);
        }

        /// <summary>
        /// Behält alle Dateien von heute.
        /// Von den letzten 2 Tagen vor heute bleibt jeweils nur die neueste Datei bestehen.
        /// Alle älteren Dateien werden gelöscht.
        /// </summary>
        public static void DeleteOldBackupFiles()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.AutoDelete)
                return;

            string backupPath = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;

            if (FnSystem.IsDebug())
            {
                backupPath = SpecialDirectories.MyDocuments + @"\GameTimeNext_Backup_dev";

                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);
            }

            if (!Directory.Exists(backupPath))
                return;

            FileInfo[] backupFiles = new DirectoryInfo(backupPath)
                .GetFiles()
                .Where(f => f.Name.EndsWith("gtnbkp"))
                .ToArray();

            List<FileInfo> filesToDelete = GetFilesOlderThanDays(backupFiles, 3);

            foreach (FileInfo file in filesToDelete)
            {
                file.Delete();
            }
        }

        /// <summary>
        /// Ermittelt alle Dateien, die gelöscht werden sollen.
        /// Es bleiben:
        /// - alle Dateien von heute
        /// - die jeweils neueste Datei von gestern
        /// - die jeweils neueste Datei von vorgestern
        /// Alle anderen Dateien werden zurückgegeben.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        private static List<FileInfo> GetFilesOlderThanDays(FileInfo[] files, int days = 3)
        {
            List<FileInfo> filesToDelete = new List<FileInfo>();

            if (files == null || files.Length == 0)
                return filesToDelete;

            DateTime today = DateTime.Today;
            DateTime oldestKeptDay = today.AddDays(-(days - 1));

            var groupedByDay = files
                .GroupBy(f => f.LastWriteTime.Date)
                .OrderByDescending(g => g.Key)
                .ToList();

            foreach (var dayGroup in groupedByDay)
            {
                DateTime fileDay = dayGroup.Key;

                // Alles löschen, was älter ist als der zu behaltende Zeitraum
                if (fileDay < oldestKeptDay)
                {
                    filesToDelete.AddRange(dayGroup);
                    continue;
                }

                // Von heute alle Dateien behalten
                if (fileDay == today)
                    continue;

                // Von den restlichen Tagen im 3-Tage-Fenster nur die neueste Datei behalten
                FileInfo? newestFileOfDay = dayGroup
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (newestFileOfDay == null)
                    continue;

                filesToDelete.AddRange(dayGroup.Where(f => f.FullName != newestFileOfDay.FullName));
            }

            return filesToDelete;
        }

        private static FileInfo? GetNewestFile(FileInfo[] files)
        {
            if (files == null || files.Length == 0)
                return null;

            return files
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
        }

        /// <summary>
        /// Creates the development application folder if it does not already exist and the application is running in
        /// debug mode.
        /// </summary>
        /// <remarks>If a normal application folder exists, its contents are copied to the development
        /// folder. Otherwise, an empty development folder is created. This method has no effect when not running in
        /// debug mode.</remarks>
        private static void CreateDevAppFolder()
        {
            if (!FnSystem.IsDebug())
                return;

            string appFolderPathNormal = AppEnvironment.GetAppConfig().AppFolderPathNormal;
            string appFolderPathDev = AppEnvironment.GetAppConfig().AppFolderPath;

            if (Directory.Exists(appFolderPathDev))
                return;

            if (Directory.Exists(appFolderPathNormal))
            {
                CopyDirectory(appFolderPathNormal, appFolderPathDev, true);

                AppEnvironment.InformationList.Add(new InformationListItem(UI.Dialogs.CFMBOXIcon.Info, "Dev folder was created from production app folder!"));
            }
            else
            {
                Directory.CreateDirectory(appFolderPathDev);
            }
        }
    }
}
