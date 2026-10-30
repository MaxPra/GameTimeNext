using System.IO;

namespace GameTimeNext.Core.Framework.LauncherIntegration.Link
{
    /// <summary>
    /// Generic link: Game ID = path of an executable, a shortcut (.lnk / .url) or a URI (e.g. "steam://...").
    /// Started through the Windows shell, so it works for every launcher without its own integration.
    /// </summary>
    public class LinkLauncher : LauncherIntegrationBase
    {
        public override string LauncherType => "LINK";

        /// <summary>
        /// Creates the game for a chosen file. Only an executable defines the game folder.
        /// </summary>
        public static LauncherGame CreateGame(string filePath)
        {
            string installPath = string.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(filePath) ?? string.Empty
                : string.Empty;

            return new LauncherGame("LINK", filePath, Path.GetFileNameWithoutExtension(filePath), installPath);
        }

        public override bool SupportsLaunchArguments => true;

        public override bool IsGameInstalled(string gameId)
        {
            // A URI can't be checked, the registered handler takes care of it
            if (IsUri(gameId))
                return true;

            return File.Exists(gameId);
        }

        public override void StartGame(string gameId, string launchArgs)
        {
            if (IsUri(gameId))
            {
                StartShell(gameId);
                return;
            }

            StartShell(gameId, launchArgs, Path.GetDirectoryName(gameId) ?? string.Empty);
        }

        private static bool IsUri(string gameId)
        {
            return gameId.Contains("://", StringComparison.Ordinal);
        }
    }
}
