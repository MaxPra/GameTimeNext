using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Steam
{
    /// <summary>
    /// Finds the Steam installation, its libraries and installed games.
    /// </summary>
    internal static class SteamLocator
    {
        /// <summary>
        /// Determines the Steam root directory
        /// </summary>
        public static string? GetSteamRoot()
        {
            // Prefer HKCU (user)
            using var u = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var user = u?.GetValue("SteamPath") as string;

            // HKLM (x86) Fallback
            using var m = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            var lm = m?.GetValue("InstallPath") as string;

            var root = Directory.Exists(user) ? user : Directory.Exists(lm) ? lm : null;
            return root;
        }

        /// <summary>
        /// Returns all steamapps folders (default + additional libraries).
        /// </summary>
        public static List<string> GetLibraryPaths(string steamRoot)
        {
            steamRoot = steamRoot.Replace("/", "\\");

            var paths = new List<string> { Path.Combine(steamRoot, "steamapps") };

            var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) return paths;

            var text = File.ReadAllText(vdf);

            // matched "path" "D:\\SteamLibrary"
            var rx = new Regex(@"""path""\s*""([^""]+)""", RegexOptions.IgnoreCase);
            foreach (Match m in rx.Matches(text))
            {
                var p = m.Groups[1].Value.Replace(@"\\", @"\");
                var steamapps = Path.Combine(p, "steamapps");
                if (Directory.Exists(steamapps) && !paths.Contains(steamapps))
                    paths.Add(steamapps);
            }

            return paths;
        }

        public static bool IsGameInstalledByAppId(string appId)
        {
            var root = GetSteamRoot();
            if (FnString.IsNullEmptyOrWhitespace(root))
                return false;

            var libs = GetLibraryPaths(root!);
            foreach (var lib in libs)
            {
                // Make sure we point exactly to ".../steamapps"
                var steamappsDir = lib.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!steamappsDir.EndsWith("steamapps", StringComparison.OrdinalIgnoreCase))
                    steamappsDir = Path.Combine(steamappsDir, "steamapps");

                var manifest = Path.Combine(steamappsDir, $"appmanifest_{appId}.acf");
                if (!File.Exists(manifest))
                    continue;

                var txt = File.ReadAllText(manifest);

                // 1) Evaluate StateFlags as bit mask (bit 3 = 4 -> installed)
                var flagsStr = SteamManifestReader.GetVdfValue(txt, "StateFlags");
                if (int.TryParse(flagsStr, System.Globalization.CultureInfo.InvariantCulture, out var flags) && (flags & 4) != 0)
                    return true;

                // 2) Fully loaded?
                var btd = SteamManifestReader.GetVdfValue(txt, "BytesToDownload");
                var bdl = SteamManifestReader.GetVdfValue(txt, "BytesDownloaded");
                if (btd == "0" && bdl == "0" && txt.IndexOf("MountedDepots", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                // 3) Fallback via installdir -> .../steamapps/common/<installdir>
                var installDirName = SteamManifestReader.GetVdfValue(txt, "installdir");
                if (!FnString.IsNullEmptyOrWhitespace(installDirName))
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
    }
}
