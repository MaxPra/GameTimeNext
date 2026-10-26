using GameTimeNext.Core.Framework.LauncherIntegration.Link;
using GameTimeNext.Core.Framework.LauncherIntegration.Steam;

namespace GameTimeNext.Core.Framework.LauncherIntegration
{
    /// <summary>
    /// All available launcher integrations, found by their key of the codetable "lT".
    /// </summary>
    public static class LauncherRegistry
    {
        private static readonly List<ILauncherIntegration> _launchers = new List<ILauncherIntegration>
        {
            new SteamLauncher(),
            new LinkLauncher()
        };

        /// <summary>
        /// Returns the integration for the given launcher type, null if there is none.
        /// </summary>
        public static ILauncherIntegration? Get(string launcherType)
        {
            return _launchers.FirstOrDefault(l => l.LauncherType == launcherType);
        }

        public static List<ILauncherIntegration> GetAll()
        {
            return _launchers;
        }
    }
}
