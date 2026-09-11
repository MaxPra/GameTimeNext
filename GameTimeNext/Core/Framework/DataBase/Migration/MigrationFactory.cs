using GameTimeNext.Core.Framework.Logging;
using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        private const string _SQL_TRANSACTION_BEGIN = "BEGIN TRANSACTION;";
        private const string _SQL_TRANSACTION_COMMIT = "COMMIT;";
        private const string _SQL_TRANSACTION_ROLLBACK = "ROLLBACK;";
        private const string _SQL_PRIMARYKEY_OFF = "PRAGMA foreign_keys = OFF;";
        private const string _SQL_PRIMARYKEY_ON = "PRAGMA foreign_keys = ON;";

        private static void CopyDirectory(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            if (!Directory.Exists(sourceDirectoryPath)) return;

            LogInfo($"Copying files from \"{sourceDirectoryPath}\" to \"{destinationDirectoryPath}\"...");
            List<string> filePaths = Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.TopDirectoryOnly).ToList();

            Parallel.ForEach(filePaths, filePath =>
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string newPath = Path.Combine(destinationDirectoryPath, fileInfo.Name);
                File.Copy(fileInfo.FullName, newPath, true);
            });
        }

        private static void LogInfo(string message, string? subSystem = null, string? method = null)
            => FnLog.AddInfo("MigrationFactory", BuildLogMessage(message, subSystem, method));

        private static void LogError(string message, Exception? exception = null, string? subSystem = null, string? method = null)
            => FnLog.AddError("MigrationFactory", BuildLogMessage(message, subSystem, method), exception: exception);

        private static string BuildLogMessage(string message, string? subSystem, string? method)
        {
            string temp = string.Empty;

            if (!FnString.IsNullEmptyOrWhitespace(subSystem) || !FnString.IsNullEmptyOrWhitespace(method))
            {
                temp += "[";

                if (!FnString.IsNullEmptyOrWhitespace(subSystem)) temp += subSystem;
                if (!FnString.IsNullEmptyOrWhitespace(subSystem) && !FnString.IsNullEmptyOrWhitespace(method)) temp += ".";
                if (!FnString.IsNullEmptyOrWhitespace(method)) temp += $"{method}()";

                temp += "] ";
            }

            temp += message;
            return temp;
        }
    }
}
