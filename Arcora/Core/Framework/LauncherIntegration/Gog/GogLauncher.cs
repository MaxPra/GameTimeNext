using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Gog
{
    /// <summary>
    /// GOG Galaxy. Game ID = GOG game ID. Installed games are read from the registry (also games installed
    /// with the offline installers). Started through GOG Galaxy, without Galaxy the game's executable is started.
    /// </summary>
    public class GogLauncher : LauncherIntegrationBase
    {
        private const string GamesKey = @"SOFTWARE\WOW6432Node\GOG.com\Games";
        private const string GalaxyClientKey = @"SOFTWARE\WOW6432Node\GOG.com\GalaxyClient";

        public override string LauncherType => "GOG";

        public override bool SupportsImport => true;

        public override bool SupportsOpenInLauncher => !FnString.IsNullEmptyOrWhitespace(GetGalaxyClientExe());

        public override List<LauncherGame> GetInstalledGames()
        {
            List<LauncherGame> games = new List<LauncherGame>();

            foreach (string gameId in GetLocalMachineSubKeyNames(GamesKey))
            {
                string gameKey = $@"{GamesKey}\{gameId}";

                // DLCs depend on their main game
                if (!FnString.IsNullEmptyOrWhitespace(GetLocalMachineValue(gameKey, "dependsOn")))
                    continue;

                string installPath = GetInstallPath(gameId);

                if (!Directory.Exists(installPath))
                    continue;

                games.Add(new LauncherGame(LauncherType, gameId, GetLocalMachineValue(gameKey, "gameName"), installPath));
            }

            return games;
        }

        public override bool IsGameInstalled(string gameId)
        {
            return Directory.Exists(GetInstallPath(gameId));
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            string galaxyClientExe = GetGalaxyClientExe();

            if (!FnString.IsNullEmptyOrWhitespace(galaxyClientExe))
            {
                StartShell(galaxyClientExe, $"/command=runGame /gameId={gameId} /path=\"{GetInstallPath(gameId)}\"");
                return;
            }

            // Without Galaxy: start the game directly, as its shortcut does
            string gameKey = $@"{GamesKey}\{gameId}";
            string workingDir = GetLocalMachineValue(gameKey, "workingDir");

            StartShell(GetLocalMachineValue(gameKey, "exe"), GetLocalMachineValue(gameKey, "launchParam"),
                       FnString.IsNullEmptyOrWhitespace(workingDir) ? GetInstallPath(gameId) : workingDir);
        }

        public override void OpenInLauncher(string gameId)
        {
            StartShell($"goggalaxy://openGameView/{gameId}");
        }

        private static string GetInstallPath(string gameId)
        {
            return NormalizeFolder(GetLocalMachineValue($@"{GamesKey}\{gameId}", "path"));
        }

        private static string GetGalaxyClientExe()
        {
            string clientFolder = GetLocalMachineValue($@"{GalaxyClientKey}\paths", "client");
            string clientExe = GetLocalMachineValue(GalaxyClientKey, "clientExecutable");

            if (FnString.IsNullEmptyOrWhitespace(clientFolder) || FnString.IsNullEmptyOrWhitespace(clientExe))
                return string.Empty;

            string path = Path.Combine(clientFolder, clientExe);

            return File.Exists(path) ? path : string.Empty;
        }
    }
}
