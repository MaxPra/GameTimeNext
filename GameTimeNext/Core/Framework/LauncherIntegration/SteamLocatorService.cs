using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    public class SteamLocatorService
    {
        /// <summary>
        /// Ermittelt das Steam-Root-Verzeichnis
        /// </summary>
        /// <returns></returns>
        public static string? GetSteamRoot()
        {
            // HKCU (User) bevorzugen
            using var u = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var user = u?.GetValue("SteamPath") as string;

            // HKLM (x86) Fallback
            using var m = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            var lm = m?.GetValue("InstallPath") as string;

            var root = Directory.Exists(user) ? user : Directory.Exists(lm) ? lm : null;
            return root;
        }

        public static bool IsGameInstalledByAppId(string appId)
        {
            var root = GetSteamRoot();
            if (string.IsNullOrWhiteSpace(root))
                return false;

            var libs = SteamLibrariesHelper.GetLibraryPaths(root);
            foreach (var lib in libs)
            {
                // Stelle sicher, dass wir genau auf ".../steamapps" zeigen
                var steamappsDir = lib.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!steamappsDir.EndsWith("steamapps", StringComparison.OrdinalIgnoreCase))
                    steamappsDir = Path.Combine(steamappsDir, "steamapps");

                var manifest = Path.Combine(steamappsDir, $"appmanifest_{appId}.acf");
                if (!File.Exists(manifest))
                    continue;

                var txt = File.ReadAllText(manifest);

                // 1) StateFlags als Bitmaske auswerten (Bit 3 = 4 -> installed)
                var flagsStr = GetVdfValue(txt, "StateFlags");
                if (int.TryParse(flagsStr, out var flags) && (flags & 4) != 0)
                    return true;

                // 2) Vollständig geladen?
                var btd = GetVdfValue(txt, "BytesToDownload");
                var bdl = GetVdfValue(txt, "BytesDownloaded");
                if (btd == "0" && bdl == "0" && txt.IndexOf("MountedDepots", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                // 3) Fallback über installdir -> .../steamapps/common/<installdir>
                var installDirName = GetVdfValue(txt, "installdir");
                if (!string.IsNullOrWhiteSpace(installDirName))
                {
                    var gameDir = Path.Combine(steamappsDir, "common", installDirName);
                    if (Directory.Exists(gameDir))
                    {
                        var hasExe = Directory.EnumerateFiles(gameDir, "*.exe", SearchOption.AllDirectories).Any();
                        if (hasExe) return true;
                    }
                }
            }

            return false;
        }

        // Klein helper: holt den ersten "key" "value" Eintrag aus dem ACF-Text
        private static string GetVdfValue(string vdfText, string key)
        {
            // key case-insensitiv suchen:  "key"   "value"
            var m = Regex.Match(vdfText, $"\"{Regex.Escape(key)}\"\\s*\"([^\"]*)\"",
                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return m.Success ? m.Groups[1].Value.Trim() : string.Empty;
        }
    }
}
