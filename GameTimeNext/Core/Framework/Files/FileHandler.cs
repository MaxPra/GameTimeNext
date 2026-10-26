using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.Files
{
    internal class FileHandler
    {
        /// <summary>
        /// Creates the development application folder if it does not already exist and the application is running in
        /// debug mode.
        /// </summary>
        /// <remarks>If a normal application folder exists, its contents are copied to the development
        /// folder. Otherwise, an empty development folder is created. This method has no effect when not running in
        /// debug mode.</remarks>
        public static void CreateDevAppFolder()
        {
            if (!FnSystem.IsDebug())
                return;

            string appFolderPathNormal = AppConfig.Storage.NormalStorageDirectoryPath;
            string appFolderPathDev = AppConfig.Storage.StorageDirectoryPath;

            if (Directory.Exists(appFolderPathDev))
                return;

            if (Directory.Exists(appFolderPathNormal))
            {
                CopyDirectory(appFolderPathNormal, appFolderPathDev, true);

                AppEnvironment.InformationList.Add(InformationListItem.FromErrorMessage(UI.Dialogs.CFMBOXIcon.Info, "GLO0020"));
            }
            else
            {
                FnDirectory.EnsureDirectoryExists(appFolderPathDev);
            }
        }

        public static void CopyDirectory(string sourceDirectory, string targetDirectory, bool overwriteFiles)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException("Quellordner nicht gefunden: " + sourceDirectory);
            }

            FnDirectory.EnsureDirectoryExists(targetDirectory);

            // Copy files
            string[] files = Directory.GetFiles(sourceDirectory);

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string targetFilePath = Path.Combine(targetDirectory, fileName);

                File.Copy(file, targetFilePath, overwriteFiles);
            }

            // Copy subfolders recursively
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
        /// Keeps all files from today.
        /// For each of the last 2 days before today, only the newest file is kept.
        /// All older files are deleted.
        /// </summary>
        public static void DeleteOldBackupFiles()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.AutoDelete)
                return;

            string backupPath = AppEnvironment.GetAppConfig().AppSettings.BackupExportPath;

            if (FnSystem.IsDebug())
            {
                backupPath = AppConfig.Dev.BackupDirectoryPath;

                FnDirectory.EnsureDirectoryExists(backupPath);
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
        /// Determines all files that should be deleted.
        /// Kept are:
        /// - all files from today
        /// - the newest file from yesterday
        /// - the newest file from the day before yesterday
        /// All other files are returned.
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

                // Delete everything older than the retention period
                if (fileDay < oldestKeptDay)
                {
                    filesToDelete.AddRange(dayGroup);
                    continue;
                }

                // Keep all files from today
                if (fileDay == today)
                    continue;

                // For the remaining days in the 3-day window, keep only the newest file
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
    }
}
