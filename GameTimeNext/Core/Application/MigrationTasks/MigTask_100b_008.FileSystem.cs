using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase;
using GameTimeNext.Core.Framework.DataBase.Migration;
using GameTimeNext.Core.Framework.Utils;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal partial class MigTask_100b_008
    {
        /// <summary>
        /// Migrates to the new filesystem.
        /// </summary>
        /// <returns>True if a restart is needed.</returns>
        public static bool MigrateFileSystem()
        {
            // Read old config
            AppConfigOld oldConfig = new AppConfigOld();

            // If old DB does not exist -> No migration needed
            if (!File.Exists(oldConfig.DataBaseFilePath))
                return false;

            // Move DB
            MFS_MigrateDatabase(oldConfig);

            // Move config file
            MFS_MoveFile(oldConfig.AppConfigPath, AppConfig.Storage.AppConfigFilePath);

            // Move covers
            MFS_MoveEntireContent(oldConfig.CoverFolderPath, AppConfig.Storage.ProfileCoversDirectoryPath);

            // Move images and symbols
            // Default
            MFS_MoveEntireContent(oldConfig.ImagesSymbolsPathDefault, AppConfig.Storage.DefaultImagesSymbolsDirectoryPath);
            // User
            MFS_MoveEntireContent(oldConfig.ImagesSymbolsPathUser, AppConfig.Storage.UserImagesSymbolsDirectoryPath);

            // Move logs
            MFS_MoveEntireContent(oldConfig.AppFolderPath, AppConfig.LogsDirectoryPath, filter: "*.log");

            MFS_Cleanup(oldConfig);

            return true;
        }

        private static void MFS_MigrateDatabase(AppConfigOld oldConfig)
        {
            DataBaseManager oldDbManager = new DataBaseManager();
            oldDbManager.Initialize(oldConfig.DataBaseFilePath);
            using SQLiteConnection oldDb = oldDbManager.GetConnection();
            DataBaseManager newDbManager = new DataBaseManager();
            if (!Directory.Exists(AppConfig.Storage.StorageDirectoryPath))
                Directory.CreateDirectory(AppConfig.Storage.StorageDirectoryPath);
            newDbManager.Initialize(AppConfig.Storage.DatabaseFilePath);
            using SQLiteConnection newDb = newDbManager.GetConnection();

            MigrationFactory.ImportType importType;
            if (FnSystem.IsDebug()) importType = MigrationFactory.ImportType.DevSync;
            else importType = MigrationFactory.ImportType.ImportPackages;

            MigrationFactory.Metadata.MigrateTables(newDb);
            MigrationFactory.FromCsv.CreateTables(importType, newDb);
            MigrationFactory.FromCsv.CopyDataToTargetDb(oldDb, newDb);

            oldDb.Close();
            oldDb.Dispose();
            newDb.Close();
            newDb.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(50);
        }

        private static void MFS_MoveFile(string oldPath, string newPath)
        {
            if (!File.Exists(oldPath)) return;

            FileInfo newFileInfo = new FileInfo(newPath);
            if (!newFileInfo.Directory!.Exists)
                Directory.CreateDirectory(newFileInfo.DirectoryName!);

            File.Copy(oldPath, newPath, true);
        }

        private static void MFS_MoveEntireContent(string oldDirectoryPath, string newDirectoryPath, string filter = "*")
        {
            if (!Directory.Exists(oldDirectoryPath)) return;

            List<string> filePaths = Directory.GetFiles(oldDirectoryPath, filter, SearchOption.TopDirectoryOnly).ToList();

            Parallel.ForEach(filePaths, filePath =>
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string newPath = Path.Combine(newDirectoryPath, fileInfo.Name);
                File.Copy(fileInfo.FullName, newPath, true);
            });
        }

        private static void MFS_Cleanup(AppConfigOld oldConfig)
        {
            MFS_TryDeleteDirectory(oldConfig.AppDataLocalPath);
            MFS_TryDeleteDirectory(oldConfig.AppFolderPath);

            if (!FnSystem.IsDebug()) return;

            MFS_TryDeleteDirectory(oldConfig.DevBackupFolderPath);
            MFS_TryDeleteDirectory(oldConfig.DevGeneratedFilesPath);
        }

        private static void MFS_TryDeleteDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
