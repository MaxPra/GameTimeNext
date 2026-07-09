using GameTimeNext.Core.Framework.Components.Base;

namespace GameTimeNext.Core.Application.Profiles.Components
{
    public class CProfileSettings : GTNComponent<CProfileSettings>
    {

        public bool HDREnabled { get; set; } = false;
        public string SteamGameArgs { get; set; } = string.Empty;

        public bool OverrideGlobalBlackout { get; set; } = false;
        public bool BlackoutSideMonitors { get; set; } = false;

        public CProfileSettings() { }

        public CProfileSettings(string rawValue) : base(rawValue) { }

    }
}
