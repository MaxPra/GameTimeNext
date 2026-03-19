using GameTimeNext.Core.Application.Settings;
using GameTimeNext.Core.Framework.Utils;
using System.IO;
using System.IO.Compression;

namespace GameTimeNext.Core.Framework.Files
{
    public class FnBackup
    {
        /// <summary>
        /// Erstellt ein Backup und legt es auf dem angegebenen Pfad ab
        /// </summary>
        /// <param name="expPath">Export Pfad</param>
        public static async Task CreateBackupAsync(string expPath, string backuptype = BackupType.APP_START_BACKUP)
        {
            string sourceFolder = AppEnvironment.GetAppConfig().AppFolderPath;
            string backupName = $"{DateTime.Now:yyyyMMddHHmmss}_GameTimeNext";

            if (backuptype == BackupType.APP_START_BACKUP)
                backupName += "_AppStarted";
            else if (backuptype == BackupType.APP_CLOSED_BACKUP)
                backupName += "_AppClosed";

            backupName += ".gtnbkp";

            expPath = Path.Combine(expPath, backupName);

            string tempFolderDest = Path.Combine(AppEnvironment.GetAppConfig().AppDataLocalPath, "gametimenext_bkptemp");

            // Appordner kopieren
            FnDirectory.CopyDirectory(sourceFolder, tempFolderDest, new string[] { "GameTimeNextDb.db" }, true);

            string dataBaseDest = Path.Combine(tempFolderDest, "Data");

            // Datenbankfile backup
            AppEnvironment.GetDataBaseManager().CreateBackup(Path.Combine(dataBaseDest, "GameTimeNextDb.db"));

            // Backup Zip erstellen
            ZipFile.CreateFromDirectory(tempFolderDest, expPath);

            if (Directory.Exists(tempFolderDest))
                Directory.Delete(tempFolderDest, true);
        }

        public static void CreateBackupSync(string expPath, string backuptype = BackupType.APP_START_BACKUP)
        {
            CreateBackupAsync(expPath, backuptype).Wait();
        }

        /// <summary>
        /// Importiert ein Backup
        /// </summary>
        /// <param name="impPath">Import Pfad</param>
        public static void ImportBackup(string impPath)
        {
            string extractPath = AppEnvironment.GetAppConfig().AppFolderPath;

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            ZipFile.ExtractToDirectory(impPath, extractPath);
        }
    }
}
