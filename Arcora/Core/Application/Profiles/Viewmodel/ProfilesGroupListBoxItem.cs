using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    /// <summary>
    /// Tag card of the profile editor (ItemObject = the codetable entry <c>T1CTABD</c>).
    /// </summary>
    public class ProfilesGroupListBoxItem : UIXListItem
    {
        public string CONAME { get; set; } = string.Empty;
        public long COGRPID { get; set; }
        public bool COISSEL { get; set; }
    }
}
