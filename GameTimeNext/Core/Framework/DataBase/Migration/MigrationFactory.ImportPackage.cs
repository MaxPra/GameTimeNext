using GameTimeNext.Core.Application.Metadata.Data;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static class ImportPackage
        {
            public static void ExportPackage(string outputDirectoryPath, string packageType)
            {
                Guid packageGuid = Guid.NewGuid();

                if (!Directory.Exists(outputDirectoryPath))
                    Directory.CreateDirectory(outputDirectoryPath);

                string packageName = $"Import_Package_{packageGuid:N}";
                string tempDirectoryPath = Path.Combine(outputDirectoryPath, packageName);
                if (!Directory.Exists(tempDirectoryPath))
                    Directory.CreateDirectory(tempDirectoryPath);

                try
                {
                    bool exportMetadata = false, exportCodetables = false;

                    switch (packageType)
                    {
                        case "":
                            exportMetadata = true;
                            exportCodetables = true;
                            break;
                        case "mE":
                            exportMetadata = true;
                            break;
                        case "cT":
                            exportCodetables = true;
                            break;
                        default:
                            throw new NotImplementedException($"ImportPackageType \"{packageType}\" is not implemented.");
                    }

                    ExportToTemp(tempDirectoryPath, exportMetadata, exportCodetables);
                    ZipFile.CreateFromDirectory(tempDirectoryPath, Path.Combine(outputDirectoryPath, $"{packageName}.zip"), CompressionLevel.Optimal, false);
                }
                finally
                {
                    if (Directory.Exists(tempDirectoryPath))
                        Directory.Delete(tempDirectoryPath, true);
                }
            }

            private static void ExportToTemp(string tempDirectoryPath, bool exportMetadata, bool exportCodetables)
            {
                SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();

                if (exportMetadata)
                    ExportMetadataToTemp(connection, tempDirectoryPath);

                if (exportCodetables)
                    ExportCodetablesToTemp(connection, tempDirectoryPath);
            }

            private static void ExportMetadataToTemp(SQLiteConnection connection, string tempDirectoryPath)
            {
                ToCsv.ExportCsvFileFor(connection, "T1METAH", ImportType.ImportPackages, tempDirectoryPath);
                ToCsv.ExportCsvFileFor(connection, "T1METAP", ImportType.ImportPackages, tempDirectoryPath);
            }

            private static void ExportCodetablesToTemp(SQLiteConnection connection, string tempDirectoryPath)
            {
                TXMETAH txmetah = new TXMETAH();
                List<T1METAH> t1metahs = txmetah.ReadAll();

                t1metahs.ForEach(t1metah =>
                {
                    ToCsv.ExportCsvFileFor(connection, t1metah, ImportType.ImportPackages, tempDirectoryPath);
                });
            }
        }
    }
}
