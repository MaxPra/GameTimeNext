using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.DataBase.Import.Base;
using GameTimeNext.Core.Framework.Logging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

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

            string importDirectory = AppConfig.Temp.ImportDirectoryPath;

            string[] files = Directory.GetFiles(importDirectory, "*.zip");

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

            string tempDirectory = Path.Combine(AppConfig.Temp.ImportDirectoryPath, $"GTN_Import_{Guid.NewGuid():N}");

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

                // Importieren von mitgelieferten Dateien
                ImportPackageFiles(tempDirectory);
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

            string[] files = Directory
                .GetFiles(packageDirectory, "*.txt", SearchOption.TopDirectoryOnly)
                .Where(f => int.TryParse(Path.GetFileName(f).Split('_')[0], out _))
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

            FnLog.AddInfo("DataBaseImporter", $"Importing data...");

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

            List<List<string>> csvRows = ParseCsv(importFile.Content, ';');

            if (csvRows.Count > 0)
            {
                importFile.Header = csvRows[0];
                importFile.Rows = csvRows.Skip(1).ToList();
            }

            return importFile;
        }

        private static List<List<string>> ParseCsv(string text, char separator)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (!inQuotes && c == separator)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (!inQuotes && (c == '\n' || c == '\r'))
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;

                    row.Add(field.ToString());
                    field.Clear();

                    if (row.Count > 1 || (row.Count == 1 && row[0].Length > 0))
                        rows.Add(row);

                    row = new List<string>();
                    continue;
                }

                field.Append(c);
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                if (row.Count > 1 || (row.Count == 1 && row[0].Length > 0))
                    rows.Add(row);
            }

            return rows;
        }

        private static void ImportPackageFiles(string packageDirectory)
        {
            FnLog.AddInfo("DataBaseImporter", "Importing files...");
            string packageDefaultDirectory = Path.Combine(packageDirectory, "files", "default");

            if (!Directory.Exists(packageDefaultDirectory))
                return;

            string targetDirectory = AppConfig.Storage.DefaultImagesSymbolsDirectoryPath;

            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, true);

            CopyDirectory(packageDefaultDirectory, targetDirectory);
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
