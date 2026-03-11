using GameTimeNext.Core.Application.Profiles.Viewmodel;
using System.IO;
using System.Text.RegularExpressions;

namespace GameTimeNext.Core.Framework.Utils
{
    public class FnSystem
    {
        public static List<Executable> FindExecutables(string path)
        {
            List<Executable> foundExecutables = new List<Executable>();

            try
            {
                foreach (var file in Directory.GetFiles(path, "*.exe"))
                {
                    foundExecutables.Add(new Executable()
                    {
                        Name = Path.GetFileName(file),
                        IsSelected = true
                    });
                }

                foreach (var dir in Directory.GetDirectories(path))
                {
                    foundExecutables.AddRange(FindExecutables(dir));
                }
            }
            catch
            {
            }

            return foundExecutables;
        }

        public static List<Executable> SortOutExecutables(List<Executable> executables)
        {
            if (executables == null || executables.Count == 0)
                return new List<Executable>();

            var ratedExecutables = executables
                .Where(e => e != null && !string.IsNullOrWhiteSpace(e.Name))
                .Select(e => new RatedExecutable
                {
                    Executable = e,
                    NormalizedName = NormalizeName(e.Name),
                    Score = RateExecutable(e.Name)
                })
                .ToList();

            bool hasClearlyPlayableExe = ratedExecutables.Any(x => x.Score >= 40);

            List<Executable> result = ratedExecutables
                .Where(x =>
                {
                    if (string.IsNullOrWhiteSpace(x.NormalizedName))
                        return false;

                    if (IsHardBlocked(x.NormalizedName))
                        return false;

                    if (x.Score < 0)
                        return false;

                    if (hasClearlyPlayableExe && IsProbablyLauncher(x.NormalizedName) && x.Score < 40)
                        return false;

                    return true;
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Executable.Name.Length)
                .Select(x => x.Executable)
                .ToList();

            if (result.Count == 0)
            {
                result = ratedExecutables
                    .Where(x => !IsHardBlocked(x.NormalizedName))
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Executable.Name.Length)
                    .Take(3)
                    .Select(x => x.Executable)
                    .ToList();
            }

            return result;
        }

        private static int RateExecutable(string exeName)
        {
            string name = NormalizeName(exeName);
            int score = 0;

            if (string.IsNullOrWhiteSpace(name))
                return -1000;

            if (IsHardBlocked(name))
                return -1000;

            foreach (string exactName in ExactToolNames)
            {
                if (name == exactName)
                    score -= 500;
            }

            foreach (string token in StrongNegativeTokens)
            {
                if (ContainsToken(name, token))
                    score -= 120;
            }

            foreach (string token in MediumNegativeTokens)
            {
                if (ContainsToken(name, token))
                    score -= 60;
            }

            foreach (string token in SoftNegativeTokens)
            {
                if (ContainsToken(name, token))
                    score -= 15;
            }

            foreach (string token in PositiveTokens)
            {
                if (ContainsToken(name, token))
                    score += 20;
            }

            if (Regex.IsMatch(name, @"\bwin64\b"))
                score += 20;

            if (Regex.IsMatch(name, @"\bx64\b"))
                score += 15;

            if (Regex.IsMatch(name, @"\bshipping\b"))
                score += 35;

            if (Regex.IsMatch(name, @"\bgame\b"))
                score += 20;

            if (Regex.IsMatch(name, @"\bclient\b"))
                score += 10;

            if (Regex.IsMatch(name, @"\bserver\b"))
                score -= 120;

            if (Regex.IsMatch(name, @"\beditor\b"))
                score -= 150;

            if (Regex.IsMatch(name, @"\btool\b"))
                score -= 120;

            if (Regex.IsMatch(name, @"\bviewer\b"))
                score -= 120;

            if (Regex.IsMatch(name, @"\bcompiler\b"))
                score -= 150;

            if (Regex.IsMatch(name, @"\bconvert\b"))
                score -= 100;

            if (Regex.IsMatch(name, @"\bpublish\b"))
                score -= 120;

            if (Regex.IsMatch(name, @"\bdebug\b"))
                score -= 60;

            if (Regex.IsMatch(name, @"\btest\b"))
                score -= 60;

            if (Regex.IsMatch(name, @"\bdemo\b"))
                score -= 40;

            if (IsProbablyLauncher(name))
                score -= 10;

            if (LooksLikePrimaryGameExe(name))
                score += 40;

            return score;
        }

        private static bool IsHardBlocked(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            if (HardBlockedNames.Any(blocked => name == blocked))
                return true;

            if (ExactToolNames.Any(tool => name == tool))
                return true;

            foreach (string token in HardBlockedTokens)
            {
                if (ContainsToken(name, token))
                    return true;
            }

            return false;
        }

        private static bool IsProbablyLauncher(string name)
        {
            return ContainsToken(name, "launcher")
                || ContainsToken(name, "launch")
                || ContainsToken(name, "start")
                || ContainsToken(name, "bootstrap");
        }

        private static bool LooksLikePrimaryGameExe(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (IsHardBlocked(name))
                return false;

            if (StrongNegativeTokens.Any(t => ContainsToken(name, t)))
                return false;

            if (MediumNegativeTokens.Any(t => ContainsToken(name, t)))
                return false;

            if (HardBlockedTokens.Any(t => ContainsToken(name, t)))
                return false;

            if (Regex.IsMatch(name, @"^[a-z0-9]{2,20}$"))
                return true;

            if (Regex.IsMatch(name, @"^[a-z0-9\s]{2,30}$"))
                return true;

            return false;
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name.Trim().ToLowerInvariant();

            if (name.EndsWith(".exe"))
                name = name[..^4];

            name = name.Replace("_", " ");
            name = name.Replace("-", " ");
            name = Regex.Replace(name, @"\s+", " ").Trim();

            return name;
        }

        private static bool ContainsToken(string input, string token)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(token))
                return false;

            string escapedToken = Regex.Escape(token).Replace("\\ ", @"\s+");
            string pattern = $@"(?<![a-z0-9]){escapedToken}(?![a-z0-9])";

            if (Regex.IsMatch(input, pattern))
                return true;

            return input.Contains(token, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class RatedExecutable
        {
            public required Executable Executable { get; set; }
            public required string NormalizedName { get; set; }
            public int Score { get; set; }
        }

        private static readonly string[] HardBlockedNames =
        {
            "crashreporter",
            "crashreportclient",
            "crashpad handler",
            "crashpadhandler",
            "unitycrashhandler",
            "unitycrashhandler64",
            "unrealcrashhandler",
            "unrealcrashreporter",
            "werfault",
            "7z",
            "7za",
            "7zr",
            "unins000",
            "vcredist",
            "dxsetup",
            "vc redist",
            "vc_redist",
            "eula",
            "register",
            "install",
            "installer",
            "setup",
            "uninstall"
        };

        private static readonly string[] ExactToolNames =
        {
            "bspzip",
            "captioncompiler",
            "demoinfo",
            "dmxconvert",
            "dmxedit",
            "elementviewer",
            "fixenvmapmasks",
            "glview",
            "hammer",
            "height2normal",
            "height2ssbump",
            "hlfaceposer",
            "hlmv",
            "mdl2legacy",
            "mksheet",
            "motionmapper",
            "normal2ssbump",
            "pfm2tgas",
            "qceyes",
            "shadercompile",
            "splitskybox",
            "studiomdl",
            "vbsp",
            "vbspinfo",
            "vpk",
            "vrad",
            "vtex",
            "vtf2tga",
            "vvis"
        };

        private static readonly string[] HardBlockedTokens =
        {
            "crash",
            "reporter",
            "crashpad",
            "bugreport",
            "updater",
            "patcher",
            "installer",
            "uninstall",
            "redist",
            "redistributable",
            "telemetry",
            "benchmark",
            "diagnostic",
            "configtool",
            "tool",
            "tools",
            "compiler",
            "viewer",
            "editor",
            "convert",
            "converter",
            "publish",
            "faceposer",
            "shadercompile",
            "studiomdl",
            "hammer",
            "vtex",
            "vrad",
            "vbsp",
            "vvis",
            "vpk",
            "qceyes",
            "glview",
            "elementviewer",
            "dmxedit",
            "dmxconvert",
            "captioncompiler",
            "motionmapper",
            "mksheet",
            "pfm2tgas",
            "vtf2tga",
            "height2normal",
            "height2ssbump",
            "normal2ssbump",
            "fixenvmapmasks",
            "demoinfo",
            "hlmv",
            "mdl2legacy"
        };

        private static readonly string[] StrongNegativeTokens =
        {
            "crash",
            "reporter",
            "updater",
            "patcher",
            "installer",
            "uninstall",
            "redist",
            "redistributable",
            "benchmark",
            "telemetry",
            "helper",
            "diagnostic",
            "configtool",
            "compiler",
            "viewer",
            "editor",
            "convert",
            "converter",
            "publish",
            "sdk",
            "modtools",
            "tool",
            "tools"
        };

        private static readonly string[] MediumNegativeTokens =
        {
            "bootstrap",
            "repair",
            "service",
            "assistant",
            "checker",
            "eac",
            "battleye",
            "anticheat",
            "config",
            "settings",
            "dedicated",
            "server",
            "test",
            "debug",
            "demo"
        };

        private static readonly string[] SoftNegativeTokens =
        {
            "launcher",
            "launch",
            "start"
        };

        private static readonly string[] PositiveTokens =
        {
            "game",
            "shipping",
            "win64",
            "x64",
            "client"
        };
    }
}