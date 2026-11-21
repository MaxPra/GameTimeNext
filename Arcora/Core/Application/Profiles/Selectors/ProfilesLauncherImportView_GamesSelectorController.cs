using GameTimeNext.Core.Application.Profiles.Controller;
using GameTimeNext.Core.Application.Profiles.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Profiles.Selectors
{
    public class ProfilesLauncherImportView_GamesSelectorController : UIXSelectorControllerBase<ProfilesLauncherImportApp, ProfilesLauncherImportView, UIXDataGrid>
    {
        public ProfilesLauncherImportView_GamesSelectorController(UIXApplication app) : base(app)
        {

        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not GameImportRow)
                return;

            BuildContextMenu(row);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtImport()
        {
            GetViewController<ProfilesLauncherImportViewController>().ImportGame();
        }

        private static void BuildContextMenu(DataGridRow dgRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("LauncherImportDataGridContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            contextBuilder.AddItem("ctxtImport", "Import", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Select), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
        }
    }
}
