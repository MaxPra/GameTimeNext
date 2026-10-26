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
                // Only add active executables to the list
                if (kvp.Value)
                    activeExes.Add(kvp.Key);
            }

            return activeExes;
        }

        /// <summary>
        /// True if at least one active executable of the profile exists in its game folder (including subfolders),
        /// i.e. the game is installed where the profile expects it.
        /// </summary>
        public static bool IsAnyActiveExecutableInstalled(T1PROFI t1profi)
        {
            HashSet<string> activeExes = GetAllActiveExecutablesFromDBObj(t1profi).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (activeExes.Count == 0)
                return false;

            EnumerationOptions options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            try
            {
                return Directory.EnumerateFiles(t1profi.EXGF, "*.exe", options).Any(file => activeExes.Contains(Path.GetFileName(file)));
            }
            catch (Exception)
            {
                // e.g. the drive was removed meanwhile
                return false;
            }
        }


        /// <summary>
        /// Returns all executables (including subfolders) for the given folder path
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <returns></returns>
        public static List<string> GetAllExecutablesFromDirectory(string directoryPath)
        {
            var allExes = new List<string>();

            if (!Directory.Exists(directoryPath))
                return allExes;

            // Get all .exe files in the given directory and its subdirectories
            string[] exeFiles = Directory.GetFiles(directoryPath, "*.exe", SearchOption.AllDirectories);

            // Collect all found .exe files
            foreach (string exeFile in exeFiles)
            {
                allExes.Add(Path.GetFileName(exeFile));
            }

            return allExes;
        }
    }
}
