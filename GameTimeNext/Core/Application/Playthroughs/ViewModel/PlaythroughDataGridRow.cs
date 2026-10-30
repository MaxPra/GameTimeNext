using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Playthroughs.ViewModel
{
    /// <summary>
    /// Row of the playthroughs tree: a profile (RowObject T1PROFI) with its playthroughs (RowObject T1PLTHR) below.
    /// </summary>
    public class PlaythroughDataGridRow : UIXTreeGridRow
    {
        public bool IsPlaythroughRow { get; set; } = false;

        // Profile name on profile rows, playthrough description on playthrough rows
        public string CONAME { get; set; } = string.Empty;

        public bool COPTCO { get; set; } = false;
        public bool COPTCA { get; set; } = false;
        public bool COPTPA { get; set; } = false;

        public string COPTTY { get; set; } = string.Empty;

        public string COPTPL { get; set; } = string.Empty;

        // Raw minutes, used as sort key for the formatted playtime column
        public double COPTPLMI { get; set; } = 0;
    }
}
