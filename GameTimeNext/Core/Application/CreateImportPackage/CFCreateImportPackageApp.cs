using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
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
            switch (exportType)
            {
                case ExportTypes.Codetables:
                    CreateImportPackageCodetables(outputPath);
                    break;
            }
        }

        private static void CreateImportPackageCodetables(string outputPath)
        {
            // Daten sammeln
            List<T1CTABH> t1ctabhs = new TXCTABH().ReadAll();
            List<T1CTABD> t1ctabds = new TXCTABD().ReadAll();

            string filenameT1CTABH = "T1CTABH.txt";
            string filenameT1CTABD = "T1CTABD.txt";

            string exportPath = outputPath;

            if (Directory.Exists(outputPath) || string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
            {
                exportPath = Path.Combine(outputPath, $"Import_Package_{Guid.NewGuid():N}.zip");
            }
            else
            {
                string? exportDirectory = Path.GetDirectoryName(exportPath);

                if (!string.IsNullOrWhiteSpace(exportDirectory) && !Directory.Exists(exportDirectory))
                    Directory.CreateDirectory(exportDirectory);
            }

            string tempDirectory = Path.Combine(outputPath, $"GTN_Codetables_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDirectory);

            try
            {
                // Daten in temporäre Dateien schreiben
                WriteToFile(Path.Combine(tempDirectory, filenameT1CTABH), t1ctabhs);
                WriteToFile(Path.Combine(tempDirectory, filenameT1CTABD), t1ctabds);

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
        private static void WriteToFile<T>(string filePath, List<T> data) where T : UIXTableObjectBase
        {
            PropertyInfo[] properties = typeof(T)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToArray();

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                string headerLine = string.Join(";", properties.Select(p => p.Name));
                writer.WriteLine(headerLine);

                foreach (T item in data)
                {
                    string line = string.Join(";", properties.Select(p => p.GetValue(item)?.ToString() ?? string.Empty));
                    writer.WriteLine(line);
                }
            }
        }

        public class ExportTypes
        {
            public const string Codetables = "cT";
        }
    }
}
