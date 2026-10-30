using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.Controller;
using GameTimeNext.Core.Application.Playthroughs.ViewModel;
using GameTimeNext.Core.Application.Playthroughs.Views;
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
using static GameTimeNext.Core.Application.Playthroughs.Controller.PlaythroughsViewController;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Playthroughs.Selectors
{
    public class PlaythroughsView_PlaythroughsSelectorController : UIXSelectorControllerBase<PlaythroughsApp, PlaythroughsView, UIXTreeGrid>
    {
        public PlaythroughsView_PlaythroughsSelectorController(UIXApplication app) : base(app)
        {
        }

        private object? GetSelectedRowObject()
        {
            return GetSelectedItem<PlaythroughDataGridRow>()?.RowObject;
        }

        // Tells the caller of the view that playthroughs changed
        private void SetHasChanged()
        {
            GetViewController<PlaythroughsViewController>().GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not PlaythroughDataGridRow ptRow)
                return;

            GetSelector().SelectedItem = ptRow;

            BuildContextMenu(row, ptRow);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        private static void BuildContextMenu(DataGridRow dgRow, PlaythroughDataGridRow ptRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("PlaythroughDataGridContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // Profile rows: start a new playthrough (only while none is running, same as in the profiles view)
            if (ptRow.RowObject is T1PROFI t1profi)
            {
                if (!TFPLTHR.HasCurrentPlaythrough(t1profi.PFID))
                    contextBuilder.AddItem("ctxtStartNewPlaythrough", "Start new playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

                dgRow.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
                return;
            }

            if (ptRow.RowObject is not T1PLTHR t1plthr)
            {
                dgRow.ContextMenu = null;
                return;
            }

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (t1plthr.PTTY != PlaythroughType.INITIAL_PLAYTHROUGH)
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (!t1plthr.PTCO && !t1plthr.PTCA)
            {
                contextBuilder.AddItem("ctxtCompletePlaythrough", "Complete Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

                if (!t1plthr.PTPA)
                    contextBuilder.AddItem("ctxtPausePlaythrough", "Pause Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Pause), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

                contextBuilder.AddItem("ctxtCancelPlaythrough", "Cancel Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            }
            if (t1plthr.PTPA)
            {
                contextBuilder.AddItem("ctxtUnpausePlaythrough", "Unpause Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Refresh), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            }

            dgRow.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
        }

        protected void EV_ctxtEdit()
        {
            if (GetSelectedRowObject() is not T1PLTHR t1plthr)
                return;

            PlaythroughEditApp editApp = GetApp().GetApplication<PlaythroughEditApp>();
            editApp.Edit(async (r) =>
            {
                if (!r.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();

            }, t1plthr);
        }

        protected void EV_ctxtStartNewPlaythrough()
        {
            if (GetSelectedRowObject() is not T1PROFI t1profi)
                return;

            PlaythroughEditApp editApp = GetApp().GetApplication<PlaythroughEditApp>();
            editApp.CreateNew(async (r) =>
            {
                if (!r.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();

                SetHasChanged();

            }, t1profi);
        }

        protected async Task EV_ctxtCompletePlaythrough()
        {
            if (GetSelectedRowObject() is not T1PLTHR t1plthr)
                return;

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Completed);

            await GetSelector().LoadDataAsync();

            SetHasChanged();
        }

        protected async Task EV_ctxtCancelPlaythrough()
        {
            if (GetSelectedRowObject() is not T1PLTHR t1plthr)
                return;

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Canceled);

            await GetSelector().LoadDataAsync();

            SetHasChanged();
        }

        protected async Task EV_ctxtPausePlaythrough()
        {
            if (GetSelectedRowObject() is not T1PLTHR t1plthr)
                return;

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Paused);

            await GetSelector().LoadDataAsync();

            SetHasChanged();
        }

        protected async Task EV_ctxtUnpausePlaythrough()
        {
            if (GetSelectedRowObject() is not T1PLTHR t1plthr)
                return;

            T1PLTHR currentPlaythrough = TFPLTHR.GetCurrentPlaythrough(t1plthr.PFID);
            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref t1plthr, TFPLTHR.PlaythroughStatus.Open, currentT1plthr: currentPlaythrough);

            await GetSelector().LoadDataAsync();

            SetHasChanged();
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedRowObject() is not T1PLTHR t1plthr)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0002", "this playthrough"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Warning);

            if (result == CFMBOXResult.Yes)
            {
                TFPLTHR.DeletePlaythroughAndSessions(t1plthr.PTID);

                SetHasChanged();

                await GetSelector().LoadDataAsync();
            }
        }
    }
}
