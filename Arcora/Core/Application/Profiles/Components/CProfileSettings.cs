using GameTimeNext.Core.Framework.Components.Base;

namespace GameTimeNext.Core.Application.Profiles.Components
{
    public class CProfileSettings : GTNComponent<CProfileSettings>
    {

        public bool HDREnabled { get; set; } = false;
        public string LaunchArgs { get; set; } = string.Empty;

        public bool OverrideGlobalBlackout { get; set; } = false;
        public bool BlackoutSideMonitors { get; set; } = false;

        // Still shrink-wrapped (setting SealNewGames); only effective while that setting is on
        public bool Sealed { get; set; } = false;

        /// <summary>
        /// Added by the automatic import and not looked at yet: the tile shows the badge "New" until the profile is
        /// selected (a sealed one keeps it until it is unwrapped).
        /// </summary>
        public bool IsNew { get; set; } = false;

        /// <summary>
        /// Suspends the game while the Big Picture mode is brought in front of it, so it doesn't react to the
        /// controller there (FnLauncher.PauseGame). On by default, also for profiles saved before the setting existed.
        /// </summary>
        public bool PauseInBigPicture { get; set; } = true;

        public CProfileSettings() { }

        public CProfileSettings(string rawValue) : base(rawValue) { }

    }
}
