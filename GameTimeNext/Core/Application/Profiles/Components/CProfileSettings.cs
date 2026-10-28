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

        public CProfileSettings() { }

        public CProfileSettings(string rawValue) : base(rawValue) { }

    }
}
