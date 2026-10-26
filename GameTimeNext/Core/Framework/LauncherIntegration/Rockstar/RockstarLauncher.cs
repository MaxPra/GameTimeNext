using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Rockstar
{
    /// <summary>
    /// Rockstar Games Launcher. Game ID = name of the game's registry key (e.g. "Grand Theft Auto V").
    /// Installed games are read from the registry and started through the launcher with their folder.
    /// </summary>
    public class RockstarLauncher : LauncherIntegrationBase
    {
        private const string RockstarKey = @"SOFTWARE\WOW6432Node\Rockstar Games";

        // Keys of the launcher itself and of its store integrations, no games
        private static readonly string[] NoGameKeys = { "Launcher", "Rockstar Games Social Club", "Steam", "Epic Games" };

        public override string LauncherType => "RSG";

        public override bool SupportsImport => true;

        public override List<LauncherGame> GetInstalledGames()
        {
            List<LauncherGame> games = new List<LauncherGame>();

            foreach (string gameId in GetLocalMachineSubKeyNames(RockstarKey))
            {
                if (NoGameKeys.Contains(gameId, StringComparer.OrdinalIgnoreCase))
                    continue;

                string installPath = GetInstallPath(gameId);

                // A game has its executable directly in its folder (e.g. GTA5.exe, RDR2.exe)
                if (!Directory.Exists(installPath) || !Directory.EnumerateFiles(installPath, "*.exe").Any())
                    continue;

                games.Add(new LauncherGame(LauncherType, gameId, gameId, installPath));
            }

            return games;
        }

        public override bool IsGameInstalled(string gameId)
        {
            return Directory.Exists(GetInstallPath(gameId)) && File.Exists(GetLauncherExe());
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            StartShell(GetLauncherExe(), $"-launchTitleInFolder \"{GetInstallPath(gameId)}\"");
        }

        private static string GetInstallPath(string gameId)
        {
            return NormalizeFolder(GetLocalMachineValue($@"{RockstarKey}\{gameId}", "InstallFolder"));
        }

        private static string GetLauncherExe()
        {
            string launcherFolder = GetLocalMachineValue($@"{RockstarKey}\Launcher", "InstallFolder");

            return FnString.IsNullEmptyOrWhitespace(launcherFolder) ? string.Empty : Path.Combine(launcherFolder, "Launcher.exe");
        }
    }
}
