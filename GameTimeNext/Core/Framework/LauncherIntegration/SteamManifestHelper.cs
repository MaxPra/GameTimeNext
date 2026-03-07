using System.IO;
using System.Text.RegularExpressions;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    public record SteamGame(uint AppId, string Name, string InstallDir, string LibraryPath);

    internal static class SteamManifestHelper
    {
        private static readonly Regex rxAppId = new(@"""appid""\s*""(\d+)""", RegexOptions.IgnoreCase);
        private static readonly Regex rxName = new(@"""name""\s*""([^""]+)""", RegexOptions.IgnoreCase);
        private static readonly Regex rxDir = new(@"""installdir""\s*""([^""]+)""", RegexOptions.IgnoreCase);

        /// <summary>
        /// Scannt alle appmanifest_*.acf in den angegebenen steamapps-Ordnern.
        /// </summary>
        public static List<SteamGame> ScanAllGames(IEnumerable<string> steamappsFolders)
        {
            var result = new List<SteamGame>();

            foreach (var steamapps in steamappsFolders)
            {
                if (!Directory.Exists(steamapps)) continue;

                foreach (var file in Directory.EnumerateFiles(steamapps, "appmanifest_*.acf"))
                {
                    var s = File.ReadAllText(file);

                    var idMatch = rxAppId.Match(s);
                    var nmMatch = rxName.Match(s);
                    var dirMatch = rxDir.Match(s);

                    if (!idMatch.Success || !nmMatch.Success) continue;

                    uint.TryParse(idMatch.Groups[1].Value, out var appid);
                    var name = nmMatch.Groups[1].Value;
                    var installdir = dirMatch.Success ? dirMatch.Groups[1].Value : string.Empty;

                    result.Add(new SteamGame(appid, name, installdir, steamapps));
                }
            }

            return result;
        }

        /// <summary>
        /// Ermittelt den erwarteten Installationspfad des Spiels.
        /// </summary>
        public static string? ResolveInstallPath(SteamGame g)
        {
            // <library>\common\<installdir>
            var common = Path.Combine(g.LibraryPath.Replace('/', '\\'), "common");
            if (!string.IsNullOrWhiteSpace(g.InstallDir))
            {
                var p = Path.Combine(common, g.InstallDir);
                if (Directory.Exists(p)) return p;
            }

            return Directory.Exists(common) ? common : null;
        }
    }
}
