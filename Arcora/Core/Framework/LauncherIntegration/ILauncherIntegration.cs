namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    /// <summary>
    /// Integration of one game launcher (Steam, Epic, ...). The launcher type is the key of the codetable "lT".
    /// </summary>
    public interface ILauncherIntegration
    {
        /// <summary>
        /// Key of the launcher in the codetable "lT" (stored in T1PROFI.LNTY).
        /// </summary>
        string LauncherType { get; }

        /// <summary>
        /// Installed games can be read from the launcher (import dialog).
        /// </summary>
        bool SupportsImport { get; }

        /// <summary>
        /// Additional launch arguments can be passed to the game.
        /// </summary>
        bool SupportsLaunchArguments { get; }

        /// <summary>
        /// The game can be shown in the launcher's library.
        /// </summary>
        bool SupportsOpenInLauncher { get; }

        /// <summary>
        /// Returns all installed games of this launcher.
        /// </summary>
        List<LauncherGame> GetInstalledGames();

        /// <summary>
        /// Returns whether the game with the given ID (T1PROFI.LNID) is installed.
        /// </summary>
        bool IsGameInstalled(string gameId);

        /// <summary>
        /// Starts the game with the given ID (T1PROFI.LNID).
        /// </summary>
        void StartGame(string gameId, string launchArgs);

        /// <summary>
        /// Shows the game in the launcher's library.
        /// </summary>
        void OpenInLauncher(string gameId);
    }
}
