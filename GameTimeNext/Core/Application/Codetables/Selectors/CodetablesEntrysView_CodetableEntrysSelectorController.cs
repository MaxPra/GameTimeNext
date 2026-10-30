using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Codetables.Selectors
{
    public class CodetablesEntrysView_CodetableEntrysSelectorController : UIXSelectorControllerBase<CodetablesEntrysApp, CodetablesEntrysView, UIXDataGrid>
    {
        public CodetablesEntrysView_CodetableEntrysSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1CTABD? GetSelectedEntry()
        {
            return GetSelectedItem<CodetableEntryDataGridRow>()?.RowObject as T1CTABD;
        }

        private void BuildContextMenu(DataGridRow dgRow, CodetableEntryDataGridRow ctRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("CodetablesEntrysDataGridContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            T1CTABD t1ctabd = (T1CTABD)ctRow.RowObject!;
            bool userCanEdit = GetUserIsAllowedToEdit(t1ctabd);

            // Development codetables may only be changed in debug mode
            if (userCanEdit)
                contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            contextBuilder.AddItem("ctxtView", "View", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.View), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            // Development codetables may only be deleted in debug mode
            if (userCanEdit)
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
        }

        private bool GetUserIsAllowedToEdit(T1CTABD t1ctabd)
        {
            if (!GetView().ViewIndicator.Contains("ED")) return false;

            if (FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions()) return true;

            if (GetApp().T1CTABH.PERMI.Equals("D")) return false;

            if (GetApp().T1CTABH.NRANA && t1ctabd.TXNUM.StartsWith("D_")) return false;

            return true;
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not CodetableEntryDataGridRow entryRow)
                return;

            BuildContextMenu(row, entryRow);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtEdit()
        {
            if (GetSelectedEntry() is not T1CTABD t1ctabd)
                return;

            CodetablesEntryEditApp app = GetApp().GetApplication<CodetablesEntryEditApp>(UIXApplicationStartTarget.Window);
            app.Edit(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();

            }, t1ctabd);
        }

        protected void EV_ctxtView()
        {
            if (GetSelectedEntry() is not T1CTABD t1ctabd)
                return;

            CodetablesEntryEditApp app = GetApp().GetApplication<CodetablesEntryEditApp>(UIXApplicationStartTarget.Window);
            app.View(t1ctabd);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedEntry() is not T1CTABD t1ctabd)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", "this entry"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXCTABD().Delete(t1ctabd.TXTYP, t1ctabd.TXNUM);

                await GetSelector().LoadDataAsync();
            }
        }
    }
}
