using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Menus.ViewModel
{
    /// <summary>
    /// Row of the menu tree: a folder or an application call (RowObject T1MENUE).
    /// </summary>
    public class MenusTreeGridRow : UIXTreeGridRow
    {
        public bool IsFolder { get; set; } = false;

        // Description of the entry (tree column)
        public UIXDataGridCell COMEDE { get; set; } = "";

        // Description of the entry type (codetable mN)
        public UIXDataGridCell COMETY { get; set; } = "";

        // Called application (APDID), empty for folders
        public UIXDataGridCell COMEAP { get; set; } = "";
    }
}
