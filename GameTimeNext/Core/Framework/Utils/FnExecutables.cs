using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using System.IO;

namespace GameTimeNext.Core.Framework.Utils
{
    public class FnExecutables
    {
        public static List<string> GetAllActiveExecutablesFromDBObj(T1PROFI t1profi)
        {
            List<string> activeExes = new List<string>();

            if (!Directory.Exists(t1profi.EXGF))
                return activeExes;

            CExecutables cExecutables = new CExecutables(t1profi.EXEC).Dezerialize();

            foreach (var kvp in cExecutables.KeyValuePairs)
            {
                // Nur aktive Exes in die Liste aufnehmen
                if (kvp.Value)
                    activeExes.Add(kvp.Key);
            }

            return activeExes;
        }


        /// <summary>
        /// Liefert alle Exes (auch in Unterordnern) zum übergebenen Ordnerpfad
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static List<string> GetAllExecutablesFromDirectory(string directoryPath)
        {
            var allExes = new List<string>();

            if (!Directory.Exists(directoryPath))
                return allExes;

            // Alle .exe-Dateien im angegebenen Verzeichnis und in Unterverzeichnissen abrufen
            string[] exeFiles = Directory.GetFiles(directoryPath, "*.exe", SearchOption.AllDirectories);

            // Alle gefundenen .exe-Dateien sammeln
            foreach (string exeFile in exeFiles)
            {
                allExes.Add(Path.GetFileName(exeFile));
            }

            return allExes;
        }
    }
}
