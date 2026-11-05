using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.SelectionLists.Controller;
using GameTimeNext.Core.Application.SelectionLists.Views;
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

namespace GameTimeNext.Core.Application.SelectionLists.Selectors
{
    public class SelectionListsView_EntriesSelectorController : UIXSelectorControllerBase<SelectionListsApp, SelectionListsView, UIXDataGrid>
    {
        public SelectionListsView_EntriesSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1CTABD? GetSelectedEntry()
        {
            return GetSelectedItem<SelectionListEntryDataGridRow>()?.RowObject as T1CTABD;
        }

        private T1CTABH? GetSelectedList()
        {
            return GetViewController<SelectionListsViewController>().GetSelectedList();
        }

        /// <summary>
        /// Edit (also by double click) and Delete (also by Del) for entries the user may change; none for locked entries.
        /// </summary>
        private void BuildContextMenu(DataGridRow dgRow, T1CTABH list, T1CTABD entry)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("SelectionListEntriesContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            if (CFSelectionListsApp.IsEntryEditable(list, entry))
            {
                contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            }

            dgRow.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not SelectionListEntryDataGridRow entryRow)
                return;

            if (GetSelectedList() is not T1CTABH list || entryRow.RowObject is not T1CTABD entry)
                return;

            GetSelector().SelectedItem = entryRow;

            BuildContextMenu(row, list, entry);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtEdit()
        {
            if (GetSelectedEntry() is not T1CTABD entry || GetSelectedList() is not T1CTABH list)
                return;

            SelectionListEntryEditApp app = GetApp().GetApplication<SelectionListEntryEditApp>(UIXApplicationStartTarget.Window);
            app.Edit(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetViewController<SelectionListsViewController>().ShowSelectedListAsync(true);
            }, entry, list);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedEntry() is not T1CTABD entry)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", $"\"{entry.DESCR}\""), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXCTABD().Delete(entry.TXTYP, entry.TXNUM);

                await GetViewController<SelectionListsViewController>().ShowSelectedListAsync(true);
            }
        }
    }
}
