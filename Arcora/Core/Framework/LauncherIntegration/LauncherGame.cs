namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    /// <summary>
    /// A game found in a launcher (import result).
    /// </summary>
    /// <param name="LauncherType">Key of the launcher in the codetable "lT"</param>
    /// <param name="GameId">ID used by the launcher to start the game (stored in T1PROFI.LNID)</param>
    /// <param name="Name">Name of the game</param>
    /// <param name="InstallPath">Folder of the installed game, empty if unknown</param>
    public record LauncherGame(string LauncherType, string GameId, string Name, string InstallPath)
    {
        /// <summary>
        /// Description of the launcher from the codetable, filled by FnLauncher.
        /// </summary>
        public string LauncherName { get; init; } = string.Empty;
    }
}
