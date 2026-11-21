using Arcora.Core.Framework.Components.Base;

namespace Arcora.Core.Application.Profiles.Components
{
    public class CAccentColorsInit : ArcoraComponent<CAccentColorsInit>
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
