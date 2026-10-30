using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static class ImportPackage
        {
            public static void ExportPackage(string outputDirectoryPath)
            {
                Guid packageGuid = Guid.NewGuid();

                FnDirectory.EnsureDirectoryExists(outputDirectoryPath);

                string packageName = $"Import_Package_{packageGuid:N}";
                string tempDirectoryPath = Path.Combine(outputDirectoryPath, packageName);
                FnDirectory.EnsureDirectoryExists(tempDirectoryPath);

                try
                {
                    ExportToTemp(tempDirectoryPath);
                    ZipFile.CreateFromDirectory(tempDirectoryPath, Path.Combine(outputDirectoryPath, $"{packageName}.zip"), CompressionLevel.Optimal, false);
                }
                finally
                {
                    if (Directory.Exists(tempDirectoryPath))
                        Directory.Delete(tempDirectoryPath, true);
                }
            }

            public static void ImportPackages()
            {
                if (FnSystem.IsDebug()) return;

                List<string> filePaths = Directory.GetFiles(AppConfig.Temp.ImportDirectoryPath, "*.zip").ToList();
                filePaths.ForEach(p => ImportSinglePackage(p));
            }

            private static void ExportToTemp(string tempDirectoryPath)
            {
                SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();

                ExportMetadataToTemp(connection, tempDirectoryPath);
                ExportTablesToTemp(connection, tempDirectoryPath);
            }

            private static void ExportMetadataToTemp(SQLiteConnection connection, string tempDirectoryPath)
            {
                ToCsv.ExportCsvFileFor(connection, "T1METAH", ImportType.ImportPackages, tempDirectoryPath);
                ToCsv.ExportCsvFileFor(connection, "T1METAP", ImportType.ImportPackages, tempDirectoryPath);
            }

            private static void ExportTablesToTemp(SQLiteConnection connection, string tempDirectoryPath)
            {
                TXMETAH txmetah = new TXMETAH();
                List<T1METAH> t1metahs = txmetah.ReadAll();

                t1metahs.ForEach(t1metah =>
                {
                    ToCsv.ExportCsvFileFor(connection, t1metah, ImportType.ImportPackages, tempDirectoryPath);
                });
            }

            private static void ImportSinglePackage(string packageFilePath)
            {
                FileInfo packageFileInfo = new FileInfo(packageFilePath);
                string tempDirectoryName = packageFileInfo.Name.Split('.').SkipLast(1).Last();
                LogInfo($"Importing ImportPackage \"{tempDirectoryName}\"...", subSystem: "ImportPackage", method: "ImportSinglePackage");
                string tempDirectoryPath = Path.Combine(AppConfig.Temp.ImportDirectoryPath, tempDirectoryName);
                FnDirectory.EnsureDirectoryExists(tempDirectoryPath);

                try
                {
                    ZipFile.ExtractToDirectory(packageFilePath, tempDirectoryPath);

                    FromCsv.MigrateTables(ImportType.ImportPackages, importDirectoryPathOverride: tempDirectoryPath);
                }
                finally
                {
                    if (Directory.Exists(tempDirectoryPath))
                        Directory.Delete(tempDirectoryPath, true);
                }

                if (File.Exists(packageFilePath))
                    File.Delete(packageFilePath);

                LogInfo($"ImportPackage \"{tempDirectoryName}\" done.", subSystem: "ImportPackage", method: "ImportSinglePackage");
            }
        }
    }
}
