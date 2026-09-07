using System.IO;

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

            List<string> filePaths = Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.TopDirectoryOnly).ToList();

            Parallel.ForEach(filePaths, filePath =>
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string newPath = Path.Combine(destinationDirectoryPath, fileInfo.Name);
                File.Copy(fileInfo.FullName, newPath, true);
            });
        }
    }
}
