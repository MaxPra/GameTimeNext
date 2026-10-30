using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Menus.Controller;
using GameTimeNext.Core.Application.Menus.ViewModel;
using GameTimeNext.Core.Application.Menus.Views;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Menus.Selectors
{
    public class MenusView_MenusSelectorController : UIXSelectorControllerBase<MenusApp, MenusView, UIXTreeGrid>
    {
        public MenusView_MenusSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1MENUE? GetSelectedEntry()
        {
            return GetSelectedItem<MenusTreeGridRow>()?.RowObject as T1MENUE;
        }

        private static void BuildContextMenu(DataGridRow dgRow, T1MENUE t1menue)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("MenusContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // First entry = double click
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (t1menue.METY == "01")
            {
                contextBuilder.AddItem("ctxtNewFolder", "New folder", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
                contextBuilder.AddItem("ctxtNewApplication", "New application call", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Add), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            }

            contextBuilder.AddItem("ctxtMoveUp", "Move up", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Up), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtMoveDown", "Move down", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Down), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row)
                return;

            if (row.DataContext is not MenusTreeGridRow menuRow || menuRow.RowObject is not T1MENUE t1menue)
                return;

            GetSelector().SelectedItem = menuRow;

            BuildContextMenu(row, t1menue);

            row.ContextMenu!.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtEdit()
        {
            GetApp().MenusDetailView!.txbDescription.Focus();
        }

        protected void EV_ctxtNewFolder()
        {
            if (GetSelectedEntry() is T1MENUE parent)
                GetViewController<MenusViewController>().CreateNewEntry("01", parent.MEID);
        }

        protected void EV_ctxtNewApplication()
        {
            if (GetSelectedEntry() is T1MENUE parent)
                GetViewController<MenusViewController>().CreateNewEntry("02", parent.MEID);
        }

        protected async Task EV_ctxtMoveUp()
        {
            if (GetSelectedEntry() is T1MENUE t1menue && TFMENUE.Move(t1menue, -1))
                await GetViewController<MenusViewController>().BuildTreeAsync(t1menue.MEID);
        }

        protected async Task EV_ctxtMoveDown()
        {
            if (GetSelectedEntry() is T1MENUE t1menue && TFMENUE.Move(t1menue, 1))
                await GetViewController<MenusViewController>().BuildTreeAsync(t1menue.MEID);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedEntry() is not T1MENUE t1menue)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            // A folder takes everything below it along
            bool hasChildren = TFMENUE.GetChildren(t1menue.MEID).Count > 0;
            string message = hasChildren
                ? GetErrorMessage("GLO0002", $"the folder \"{t1menue.MEDE}\"")
                : GetErrorMessage("GLO0001", $"\"{t1menue.MEDE}\"");

            if (cfmbox.Show(message, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question) != CFMBOXResult.Yes)
                return;

            TFMENUE.DeleteWithChildren(t1menue.MEID);

            await GetViewController<MenusViewController>().BuildTreeAsync(t1menue.MEPA);
        }
    }
}
