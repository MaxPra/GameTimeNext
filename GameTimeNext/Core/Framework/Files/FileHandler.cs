using GameTimeNext.Core.Framework.Config;
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
        /// Diese Methode sorgt dafür, dass nur die neusten Backups der letzten 3 Backuptage bestehen bleiben
        /// </summary>
        public static void DeleteOldBackupFiles()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.AutoDelete)
                return;

            // Alle Backups einlesen
            if (Directory.Exists(AppEnvironment.GetAppConfig().AppSettings.BackupExportPath))
            {
                FileInfo[] backupFiles = new DirectoryInfo(AppEnvironment.GetAppConfig().AppSettings.BackupExportPath).GetFiles().Where(f => f.Name.EndsWith("gtnbkp")).ToArray();

                List<FileInfo> oldFiles = GetFilesOlderThanDays(backupFiles, 3);

                // Alle alten Dateien löschen
                foreach (FileInfo file in oldFiles)
                {
                    file.Delete();
                }
            }
        }

        /// <summary>
        /// Ermittelt alle Dateien, die älter sind als die letzten 5 Tage
        /// Nimmt dabei Rücksicht darauf, die neusten 3 Tage (verdichtet um die neusten der 3 Tage) zu "ignorieren"
        /// </summary>
        /// <param name="files"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        private static List<FileInfo> GetFilesOlderThanDays(FileInfo[] files, int days = 3)
        {
            DateTime today = DateTime.Today;
            DateTime fromDay;

            FileInfo? newestFile = GetNewestFile(files);

            if (newestFile == null)
                return new List<FileInfo>();

            fromDay = newestFile.LastWriteTime;

            // Alte Dateien bestimmen
            // Älter als [days] Tage

            var oldFiles = files.Where(f => f.LastWriteTime < (fromDay.AddDays(-days))).ToList();

            return oldFiles;
        }

        private static FileInfo? GetNewestFile(FileInfo[] files)
        {
            FileInfo? newestFile = files.OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

            return newestFile;
        }
    }
}
