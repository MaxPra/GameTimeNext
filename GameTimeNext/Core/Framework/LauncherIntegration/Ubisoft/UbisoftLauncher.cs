using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Ubisoft
{
    /// <summary>
    /// Ubisoft Connect. Game ID = Ubisoft install ID. Installed games are read from the registry.
    /// </summary>
    public class UbisoftLauncher : LauncherIntegrationBase
    {
        private const string InstallsKey = @"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs";
        private const string UninstallKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        public override string LauncherType => "UBI";

        public override bool SupportsImport => true;

        public override List<LauncherGame> GetInstalledGames()
        {
            List<LauncherGame> games = new List<LauncherGame>();

            foreach (string gameId in GetLocalMachineSubKeyNames(InstallsKey))
            {
                string installPath = GetInstallPath(gameId);

                if (!Directory.Exists(installPath))
                    continue;

                games.Add(new LauncherGame(LauncherType, gameId, GetName(gameId, installPath), installPath));
            }

            return games;
        }

        public override bool IsGameInstalled(string gameId)
        {
            return Directory.Exists(GetInstallPath(gameId));
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            StartShell($"uplay://launch/{gameId}/0");
        }

        private static string GetInstallPath(string gameId)
        {
            return NormalizeFolder(GetLocalMachineValue($@"{InstallsKey}\{gameId}", "InstallDir"));
        }

        private static string GetName(string gameId, string installPath)
        {
            // The name is only in the uninstall entry, the install folder is named after the game too
            string name = GetLocalMachineValue($@"{UninstallKey}\Uplay Install {gameId}", "DisplayName");

            return FnString.IsNullEmptyOrWhitespace(name) ? Path.GetFileName(installPath) : name;
        }
    }
}
