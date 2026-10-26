using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Rockstar
{
    /// <summary>
    /// Rockstar Games Launcher. Game ID = name of the game's registry key (e.g. "Grand Theft Auto V").
    /// Installed games are read from the registry. The game is started with its own starter in the game folder
    /// (as its desktop shortcut does), which signs in through the Rockstar Games Launcher and then starts the game.
    /// </summary>
    public class RockstarLauncher : LauncherIntegrationBase
    {
        private const string RockstarKey = @"SOFTWARE\WOW6432Node\Rockstar Games";

        // Keys of the launcher itself and of its store integrations, no games
        private static readonly string[] NoGameKeys = { "Launcher", "Rockstar Games Social Club", "Steam", "Epic Games" };

        public override string LauncherType => "RSG";

        public override bool SupportsImport => true;

        public override bool SupportsLaunchArguments => true;

        public override List<LauncherGame> GetInstalledGames()
        {
            List<LauncherGame> games = new List<LauncherGame>();

            foreach (string gameId in GetLocalMachineSubKeyNames(RockstarKey))
            {
                if (NoGameKeys.Contains(gameId, StringComparer.OrdinalIgnoreCase))
                    continue;

                string installPath = GetInstallPath(gameId);

                // A game has its executable directly in its folder (e.g. GTA5.exe, RDR2.exe)
                if (FnString.IsNullEmptyOrWhitespace(GetStartExecutable(installPath)))
                    continue;

                games.Add(new LauncherGame(LauncherType, gameId, gameId, installPath));
            }

            return games;
        }

        public override bool IsGameInstalled(string gameId)
        {
            return !FnString.IsNullEmptyOrWhitespace(GetStartExecutable(GetInstallPath(gameId)));
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            string installPath = GetInstallPath(gameId);

            StartShell(GetStartExecutable(installPath), launchArgs, installPath);
        }

        private static string GetInstallPath(string gameId)
        {
            return NormalizeFolder(GetLocalMachineValue($@"{RockstarKey}\{gameId}", "InstallFolder"));
        }

        /// <summary>
        /// Returns the starter of the game (e.g. PlayGTAV.exe), otherwise the game executable itself
        /// (the biggest executable in the game folder). Empty if the folder contains no executable.
        /// </summary>
        private static string GetStartExecutable(string installPath)
        {
            if (FnString.IsNullEmptyOrWhitespace(installPath) || !Directory.Exists(installPath))
                return string.Empty;

            string[] executables = Directory.GetFiles(installPath, "*.exe");

            string? starter = executables.FirstOrDefault(e => Path.GetFileName(e).StartsWith("Play", StringComparison.OrdinalIgnoreCase));

            return starter ?? executables.OrderByDescending(e => new FileInfo(e).Length).FirstOrDefault() ?? string.Empty;
        }
    }
}
