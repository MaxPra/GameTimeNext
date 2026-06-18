using System.IO;
using System.IO.Compression;

namespace GameTimeNext.Core.Framework.DataBase.Import
{
    public class DataBaseImporter
    {
        /// <summary>
        /// Importiert ein GTN-Importpaket (ZIP) in ein temporäres Verzeichnis und startet den Datenimport.
        /// </summary>
        public static void Import(string importPackagePath)
        {
            if (!File.Exists(importPackagePath))
                throw new FileNotFoundException($"Import package not found: {importPackagePath}");

            string tempDirectory = Path.Combine(Path.GetTempPath(), $"GTN_Import_{Guid.NewGuid():N}");

            Directory.CreateDirectory(tempDirectory);
            try
            {
                // Entpacken des Importpakets
                ZipFile.ExtractToDirectory(importPackagePath, tempDirectory);

                // Todo Paket lesen und Import starten (UIXStatement)
            }
            finally
            {
                // Temporäres Verzeichnis löschen
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, true);
            }
        }
    }
}
