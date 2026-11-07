using GameTimeNext.Core.Framework.Config;
using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Api
{
    /// <summary>
    /// For the parameter "Path" of the codetable "Images / Symbols": the user chooses an image in the file dialog, the image is
    /// copied into the user folder of the symbols and the path relative to the symbol folder (e.g. <c>\user\switch.png</c>) is returned.
    /// </summary>
    public class CTAPI_ImageImport : CTAPIBase
    {
        // Formats the app can show as symbol
        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico" };

        public override string ButtonText => "Choose image...";

        public override string ButtonToolTip => "Choose an image. It is copied into the symbol folder for you.";

        public override async Task<CTAPIResult> ExecuteAsync(CTAPIContext context)
        {
            string filter = "Images|" + string.Join(";", SupportedExtensions.Select(e => "*" + e));
            string sourcePath = FnSystemDialogs.ShowFileDialog("Choose an image", filter, false, context.Owner);

            if (FnString.IsNullEmptyOrWhitespace(sourcePath))
                return CTAPIResult.Cancelled();

            if (!SupportedExtensions.Contains(Path.GetExtension(sourcePath), StringComparer.OrdinalIgnoreCase))
                return CTAPIResult.Failure($"This file type is not supported. Please choose one of: {string.Join(", ", SupportedExtensions)}");

            string targetPath = await Task.Run(() => CopyIntoUserFolder(sourcePath));

            // Stored like the existing entries: relative to the symbol folder, starting with a backslash
            return CTAPIResult.WithValue("\\" + Path.GetRelativePath(AppConfig.Storage.ImagesSymbolsDirectoryPath, targetPath));
        }

        /// <summary>
        /// Copies the file into the user folder of the symbols and returns the path of the copy. A file that is already there is
        /// used as it is; a different file with the same name gets a number, so nothing is overwritten.
        /// </summary>
        private static string CopyIntoUserFolder(string sourcePath)
        {
            string targetDirectory = Path.GetFullPath(AppConfig.Storage.UserImagesSymbolsDirectoryPath);
            string fileName = Path.GetFileName(sourcePath);
            string targetPath = Path.Combine(targetDirectory, fileName);

            if (string.Equals(Path.GetFullPath(sourcePath), targetPath, StringComparison.OrdinalIgnoreCase))
                return targetPath;

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            for (int counter = 2; File.Exists(targetPath); counter++)
            {
                if (HaveSameContent(sourcePath, targetPath))
                    return targetPath;

                targetPath = Path.Combine(targetDirectory, $"{baseName} ({counter}){extension}");
            }

            File.Copy(sourcePath, targetPath);

            return targetPath;
        }

        private static bool HaveSameContent(string pathA, string pathB)
        {
            if (new FileInfo(pathA).Length != new FileInfo(pathB).Length)
                return false;

            return File.ReadAllBytes(pathA).AsSpan().SequenceEqual(File.ReadAllBytes(pathB));
        }
    }
}
