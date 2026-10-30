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
    public class CodetablesView_CodetablesSelectorController : UIXSelectorControllerBase<CodetablesApp, CodetablesView, UIXDataGrid>
    {
        public CodetablesView_CodetablesSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1CTABH? GetSelectedCodetable()
        {
            return GetSelectedItem<CodetableDataGridRow>()?.RowObject as T1CTABH;
        }

        private static void BuildContextMenu(DataGridRow dgRow, T1CTABH t1ctabh)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // Development codetables may only be changed in debug mode
            if ((t1ctabh.PERMI == "D" && FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions()) || t1ctabh.PERMI == "U")
                contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            contextBuilder.AddItem("ctxtView", "View", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.View), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions())
                contextBuilder.AddItem("ctxtProperties", "Properties", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Info), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            // Development codetables may only be deleted in debug mode
            if (FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions())
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not CodetableDataGridRow codetableRow)
                return;

            GetSelector().SelectedItem = codetableRow;

            BuildContextMenu(row, (T1CTABH)codetableRow.RowObject!);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedCodetable() is not T1CTABH t1ctabh)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0002", "this codetable"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                TFCTABH.DeleteCodetableAndEntries(t1ctabh);

                await GetSelector().LoadDataAsync();
            }
        }

        protected void EV_ctxtProperties()
        {
            if (GetSelectedCodetable() is not T1CTABH t1ctabh)
                return;

            CodetablesEditApp codetablesEditApp = GetApp().GetApplication<CodetablesEditApp>(UIXApplicationStartTarget.Window);
            codetablesEditApp.Properties(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();
            }, t1ctabh, (FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions()));
        }

        protected void EV_ctxtEdit()
        {
            if (GetSelectedCodetable() is not T1CTABH t1ctabh)
                return;

            CodetablesEntrysApp codetablesEntrysApp = GetApp().GetApplication<CodetablesEntrysApp>(UIXApplicationStartTarget.Window);
            codetablesEntrysApp.Edit(t1ctabh);
        }

        protected void EV_ctxtView()
        {
            if (GetSelectedCodetable() is not T1CTABH t1ctabh)
                return;

            CodetablesEntrysApp codetablesEntrysApp = GetApp().GetApplication<CodetablesEntrysApp>(UIXApplicationStartTarget.Window);
            codetablesEntrysApp.View(t1ctabh);
        }
    }
}
