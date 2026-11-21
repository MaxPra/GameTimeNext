using GameTimeNext.Core.Framework.Components.Base;

namespace GameTimeNext.Core.Application.Profiles.Components
{
    public class CAccentColors : GTNComponent<CAccentColors>
    {

        public Dictionary<string, string>? AccentColors { get; set; } = new Dictionary<string, string>();

        public CAccentColors() : base() { }

        public CAccentColors(string rawValue) : base(rawValue) { }

        public void Initialize(Dictionary<string, string> accentColors)
        {
            AccentColors = accentColors;
        }
    }
}
