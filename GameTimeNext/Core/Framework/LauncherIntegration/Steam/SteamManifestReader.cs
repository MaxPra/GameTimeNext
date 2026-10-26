using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Steam
{
    internal record SteamManifest(uint AppId, string Name, string InstallDir, string LibraryPath, uint StateFlags = 0);

    /// <summary>
    /// Reads the appmanifest_*.acf files of the Steam libraries.
    /// </summary>
    internal static class SteamManifestReader
    {
        private static readonly Regex rxAppId = new(@"""appid""\s*""(\d+)""", RegexOptions.IgnoreCase);
        private static readonly Regex rxName = new(@"""name""\s*""([^""]+)""", RegexOptions.IgnoreCase);
        private static readonly Regex rxDir = new(@"""installdir""\s*""([^""]+)""", RegexOptions.IgnoreCase);
        private static readonly Regex rxStateFlags = new(@"""StateFlags""\s*""(\d+)""", RegexOptions.IgnoreCase);

        /// <summary>
        /// Scans all appmanifest_*.acf files in the given steamapps folders.
        /// </summary>
        public static List<SteamManifest> ScanAllGames(IEnumerable<string> steamappsFolders)
        {
            var result = new List<SteamManifest>();

            foreach (var steamapps in steamappsFolders)
            {
                if (!Directory.Exists(steamapps)) continue;

                foreach (var file in Directory.EnumerateFiles(steamapps, "appmanifest_*.acf"))
                {
                    var s = File.ReadAllText(file);

                    var idMatch = rxAppId.Match(s);
                    var nmMatch = rxName.Match(s);
                    var dirMatch = rxDir.Match(s);
                    var stateFlagsMatch = rxStateFlags.Match(s);

                    if (!idMatch.Success || !nmMatch.Success) continue;

                    uint.TryParse(idMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var appid);
                    var name = nmMatch.Groups[1].Value;
                    var installdir = dirMatch.Success ? dirMatch.Groups[1].Value : string.Empty;
                    uint stateFlags = 0;
                    if (stateFlagsMatch.Success)
                        uint.TryParse(stateFlagsMatch.Groups[1].Value, CultureInfo.InvariantCulture, out stateFlags);

                    result.Add(new SteamManifest(appid, name, installdir, steamapps, stateFlags));
                }
            }

            return result;
        }

        /// <summary>
        /// Determines the expected installation path of the game.
        /// </summary>
        public static string? ResolveInstallPath(SteamManifest g)
        {
            // <library>\common\<installdir>
            var common = Path.Combine(g.LibraryPath.Replace('/', '\\'), "common");
            if (!FnString.IsNullEmptyOrWhitespace(g.InstallDir))
            {
                var p = Path.Combine(common, g.InstallDir);
                if (Directory.Exists(p)) return p;
            }

            return Directory.Exists(common) ? common : null;
        }

        /// <summary>
        /// Gets the first "key" "value" entry from the ACF text
        /// </summary>
        public static string GetVdfValue(string vdfText, string key)
        {
            // Search key case-insensitively:  "key"   "value"
            var m = Regex.Match(vdfText, $"\"{Regex.Escape(key)}\"\\s*\"([^\"]*)\"",
                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }
    }
}
