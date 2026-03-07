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
    }
}
