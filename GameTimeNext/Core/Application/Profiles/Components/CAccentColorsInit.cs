using GameTimeNext.Core.Framework.Components.Base;

namespace GameTimeNext.Core.Application.Profiles.Components
{
    public class CAccentColorsInit : GTNComponent<CAccentColorsInit>
    {

        public Dictionary<string, bool>? AccentColors { get; set; } = new Dictionary<string, bool>();

        public CAccentColorsInit() : base() { }

        public CAccentColorsInit(string rawValue) : base(rawValue) { }

        public void Initialize(Dictionary<string, bool> accentColors)
        {
            AccentColors = accentColors;
        }
    }
}
