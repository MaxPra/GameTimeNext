using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UIX.ViewController.Engine.DataBaseObjects;

namespace GameTimeNext.Core.Application.CreateImportPackage
{
    /// <summary>
    /// Funktionsklasse für Export
    /// </summary>
    public class CFCreateImportPackageApp
    {
        /// <summary>
        /// Erstellt ein Exportpaket mit den angegebenen Parametern
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="exportType"></param>
        /// <returns></returns>
        public static void CreateImportPackage(string outputPath, string exportType)
        {
            string exportPath = ResolveExportPath(outputPath);
            string tempDirectory = Path.Combine(Path.GetDirectoryName(exportPath)!, $"GTN_ImportPackage_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDirectory);

            try
            {
                switch (exportType)
                {
                    case "":
                        CreateImportPackageAll(tempDirectory);
                        break;

                    case ExportTypes.Metadata:
                        CreateImportPackageMetadata(tempDirectory);
                        break;

                    case ExportTypes.Codetables:
                        CreateImportPackageCodetables(tempDirectory);
                        break;
                }

                if (File.Exists(exportPath))
                    File.Delete(exportPath);

                ZipFile.CreateFromDirectory(tempDirectory, exportPath, CompressionLevel.Optimal, false);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);
            }
        }

        private static void CreateImportPackageAll(string tempDirectory)
        {
            CreateImportPackageMetadata(tempDirectory);
            CreateImportPackageCodetables(tempDirectory);
        }

        private static void CreateImportPackageCodetables(string tempDirectory)
        {
            List<T1CTABH> t1ctabhs = new TXCTABH().ReadAll();
            HashSet<string> exportableTxtyps = t1ctabhs
                .Where(p => p.EXPRT)
                .Select(p => p.TXTYP)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<T1CTABD> t1ctabds = new TXCTABD()
                .ReadAll()
                .Where(p => exportableTxtyps.Contains(p.TXTYP))
                .ToList();

            string filenameT1CTABH = "03_T1CTABH.txt";
            string filenameT1CTABD = "04_T1CTABD.txt";

            WriteToFile(Path.Combine(tempDirectory, filenameT1CTABH), t1ctabhs);
            WriteToFile(Path.Combine(tempDirectory, filenameT1CTABD), t1ctabds);

            CreateImportPackageCodetablesFiles(tempDirectory);
        }

        private static void CreateImportPackageCodetablesFiles(string tempDirectory)
        {
            string sourceDefaultPath = AppEnvironment.GetAppConfig().ImagesSymbolsPathDefault;

            if (!Directory.Exists(sourceDefaultPath))
                return;

            string filesDirectory = Path.Combine(tempDirectory, "files");
            string destinationDefaultPath = Path.Combine(filesDirectory, "default");

            CopyDirectory(sourceDefaultPath, destinationDefaultPath);
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (string filePath in Directory.GetFiles(sourceDirectory))
            {
                string destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
                File.Copy(filePath, destinationFilePath, true);
            }

            foreach (string directoryPath in Directory.GetDirectories(sourceDirectory))
            {
                string destinationSubDirectory = Path.Combine(destinationDirectory, Path.GetFileName(directoryPath));
                CopyDirectory(directoryPath, destinationSubDirectory);
            }
        }

        private static void CreateImportPackageMetadata(string tempDirectory)
        {
            List<T1METAH> t1metahs = new TXMETAH().ReadAll();
            List<T1METAP> t1metaps = new TXMETAP().ReadAll();

            string filenameT1METAH = "01_T1METAH.txt";
            string filenameT1METAP = "02_T1METAP.txt";

            WriteToFile(Path.Combine(tempDirectory, filenameT1METAH), t1metahs);
            WriteToFile(Path.Combine(tempDirectory, filenameT1METAP), t1metaps);
        }

        private static string ResolveExportPath(string outputPath)
        {
            if (Directory.Exists(outputPath) || string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
            {
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                return Path.Combine(outputPath, $"Import_Package_{Guid.NewGuid():N}.zip");
            }

            string? exportDirectory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(exportDirectory) && !Directory.Exists(exportDirectory))
                Directory.CreateDirectory(exportDirectory);

            return outputPath;
        }
        private static readonly HashSet<string> IgnoredProperties =
                                                    [
                                                        "IsDevSynced"
                                                    ];

        private static void WriteToFile<T>(string filePath, List<T> data) where T : UIXTableObjectBase
        {
            PropertyInfo[] properties = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(p =>
                    p.CanRead &&
                    p.GetIndexParameters().Length == 0 &&
                    !IgnoredProperties.Contains(p.Name))
                .ToArray();

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                string headerLine = string.Join(";", properties.Select(p => p.Name));
                writer.WriteLine(headerLine);

                foreach (T item in data)
                {
                    string line = string.Join(";",
                        properties.Select(p => EscapeCsv(SerializeForCsv(p.GetValue(item)))));

                    writer.WriteLine(line);
                }
            }
        }

        private static string SerializeForCsv(object? value)
        {
            if (value == null)
                return string.Empty;

            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            if (value is DateTimeOffset dateTimeOffset)
                return dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            if (value is bool boolValue)
                return boolValue ? "1" : "0";

            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            return value.ToString() ?? string.Empty;
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
                return string.Empty;

            bool mustQuote = value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!mustQuote)
                return value;

            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        public class ExportTypes
        {
            public const string Codetables = "cT";
            public const string Metadata = "mE";
        }
    }
}
