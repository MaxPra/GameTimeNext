using GameTimeNext.Core.Framework.DataBase.Import.Base;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase.Import
{
    public class DataBaseImporter
    {

        private static readonly List<DataBaseImporterBase> Importers =
            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    typeof(DataBaseImporterBase).IsAssignableFrom(t) &&
                    !t.IsAbstract)
                .Select(t => (DataBaseImporterBase)Activator.CreateInstance(t)!)
                .ToList();


        public static void Import()
        {
            //if (FnSystem.IsDebug())
            //    return;

            string[] files = Directory.GetFiles(AppEnvironment.GetAppConfig().AppDataLocalPath + Path.DirectorySeparatorChar + "Import", "*.zip");

            foreach (string file in files)
            {
                ImportSpecificPackage(file);
            }

        }


        /// <summary>
        /// Importiert ein GTN-Importpaket (ZIP) in ein temporäres Verzeichnis und startet den Datenimport.
        /// </summary>
        private static void ImportSpecificPackage(string importPackagePath)
        {
            if (!File.Exists(importPackagePath))
                throw new FileNotFoundException($"Import package not found: {importPackagePath}");

            string tempDirectory = Path.Combine(AppEnvironment.GetAppConfig().AppDataLocalPath + Path.DirectorySeparatorChar + "Import", $"GTN_Import_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDirectory);
            try
            {
                // Entpacken des Importpakets
                ZipFile.ExtractToDirectory(importPackagePath, tempDirectory);

                // Lesen des Import-Packets
                ImportPackage importPackage = ReadImportPackage(tempDirectory);

                if (importPackage == null)
                    return;

                // Importieren der Daten
                ImportFilesData(importPackage);
            }
            finally
            {
                // Temporäres Verzeichnis löschen
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);

                // ZipDatei löschen
                if (File.Exists(importPackagePath))
                    File.Delete(importPackagePath);
            }
        }

        private static ImportPackage ReadImportPackage(string packageDirectory)
        {
            if (!Directory.Exists(packageDirectory))
                return null!;

            ImportPackage importPackage = new ImportPackage();
            importPackage.Name = new DirectoryInfo(packageDirectory).Name;

            string[] files = Directory.GetFiles(packageDirectory)
                                        .OrderBy(f => int.Parse(Path.GetFileName(f).Split('_')[0]))
                                        .ToArray();

            List<ImportFile> importFiles = new List<ImportFile>();

            foreach (string file in files)
            {
                importPackage.ImportFiles.Add(ReadImportFile(file));
            }

            return importPackage;
        }

        private static void ImportFilesData(ImportPackage importPackage)
        {
            foreach (ImportFile importFile in importPackage.ImportFiles)
            {
                if (importFile == null)
                    continue;

                ImportFileData(importFile);
            }
        }

        private static void ImportFileData(ImportFile importFile)
        {
            DataBaseImporterBase? importer = Importers.FirstOrDefault(
                x => x.GetValidTables().Contains(importFile.TableName));

            importer?.Import(importFile);
        }

        private static ImportFile ReadImportFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null!;

            ImportFile importFile = new ImportFile();
            importFile.FileName = Path.GetFileName(filePath);
            importFile.TableName = Path.GetFileNameWithoutExtension(filePath).Split('_', 2).Last();
            importFile.Content = File.ReadAllText(filePath);

            UIXCSVReader csvReader = new UIXCSVReader(";");

            UIXCSVReader.CSVData csvData = csvReader.ReadCSV(importFile.Content);

            importFile.Header = csvData.Headers;
            importFile.Rows = csvData.Rows;

            return importFile;
        }

        public class ImportPackage
        {
            public string Name { get; set; } = string.Empty;
            public List<ImportFile> ImportFiles { get; set; } = new List<ImportFile>();
        }

        public class ImportFile
        {
            public string FileName { get; set; } = string.Empty;

            public string TableName { get; set; } = string.Empty;
            public List<string> Header { get; set; } = new List<string>();

            public List<List<string>> Rows { get; set; } = new List<List<string>>();

            public string Content { get; set; } = string.Empty;
        }
    }
}
