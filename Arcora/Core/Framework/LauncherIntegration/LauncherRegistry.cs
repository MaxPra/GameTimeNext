using Arcora.Core.Framework.LauncherIntegration.Epic;
using Arcora.Core.Framework.LauncherIntegration.Gog;
using Arcora.Core.Framework.LauncherIntegration.Link;
using Arcora.Core.Framework.LauncherIntegration.Rockstar;
using Arcora.Core.Framework.LauncherIntegration.Steam;
using Arcora.Core.Framework.LauncherIntegration.Ubisoft;
using Arcora.Core.Framework.LauncherIntegration.Xbox;

namespace Arcora.Core.Framework.LauncherIntegration
{
    /// <summary>
    /// All available launcher integrations, found by their key of the codetable "lT".
    /// </summary>
    public static class LauncherRegistry
    {
        private static readonly List<ILauncherIntegration> _launchers = new List<ILauncherIntegration>
        {
            new SteamLauncher(),
            new EpicLauncher(),
            new XboxLauncher(),
            new GogLauncher(),
            new UbisoftLauncher(),
            new RockstarLauncher(),
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
