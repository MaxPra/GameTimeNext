using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.AppSearch.ViewModels
{
    /// <summary>
    /// Row of the application search tree: a menu folder or an application call (RowObject T1MENUE).
    /// </summary>
    public class AppSearchTreeGridRow : UIXTreeGridRow
    {
        public bool IsFolder { get; set; } = false;

        // Fully qualified class name of the application to launch, empty for folders
        public string ClassName { get; set; } = string.Empty;

        // Header of the tab the application is opened in
        public string TabName { get; set; } = string.Empty;

        // Description of the menu entry (tree column)
        public UIXDataGridCell COMEDE { get; set; } = "";
    }
}
