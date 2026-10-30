using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Screensetups.Controller;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
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

namespace GameTimeNext.Core.Application.Screensetups.Selectors
{
    public class ScreensetupsView_ScreensetupsSelectorController : UIXSelectorControllerBase<ScreensetupsApp, ScreensetupsView, UIXDataGrid>
    {
        public ScreensetupsView_ScreensetupsSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1SCRSH? GetSelectedScreensetup()
        {
            return GetSelectedItem<ScreensetupDataGridRow>()?.RowObject as T1SCRSH;
        }

        private static void BuildContextMenu(DataGridRow dgRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ScreensetupsDataGridContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtApply","Switch to this screensetup", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Play), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        private async Task ApplyScreensetup(T1SCRSH t1scrsh)
        {
            string? errorMessage;

            GetApp().Loader.Begin();

            try
            {
                errorMessage = await CFScreensetupsApp.ApplyScreensetupAsync(GetApp(), t1scrsh);
            }
            finally
            {
                GetApp().Loader.Stop();
            }

            if (errorMessage != null)
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                cfmbox.Show(errorMessage, CFMBOXResult.Ok, CFMBOXIcon.Error);
            }

            await GetSelector().LoadDataAsync();
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not ScreensetupDataGridRow screensetupRow)
                return;

            GetSelector().SelectedItem = screensetupRow;

            BuildContextMenu(row);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        // Switch button in the cell template of the row
        protected async Task EV_BtnApplyScreensetup(FrameworkElement target)
        {
            if (target.DataContext is not ScreensetupDataGridRow screensetupRow || screensetupRow.RowObject is not T1SCRSH t1scrsh)
                return;

            GetSelector().SelectedItem = screensetupRow;

            await ApplyScreensetup(t1scrsh);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedScreensetup() is not T1SCRSH t1scrsh)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", "this screensetup"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                TFSCRSH.DeleteScreensetupAndMonitors(t1scrsh);

                if (AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup == t1scrsh.SSID)
                {
                    AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup = 0;
                    AppEnvironment.SaveAppConfig();
                }

                await GetSelector().LoadDataAsync();
            }
        }

        protected void EV_ctxtEdit()
        {
            if (GetSelectedScreensetup() is not T1SCRSH t1scrsh)
                return;

            ScreensetupsEditApp screensetupsEditApp = GetApp().GetApplication<ScreensetupsEditApp>(UIXApplicationStartTarget.Window);
            screensetupsEditApp.Edit(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();
            }, t1scrsh);
        }

        protected void EV_ctxtCopy()
        {
            if (GetSelectedScreensetup() is not T1SCRSH t1scrsh)
                return;

            ScreensetupsEditApp screensetupsEditApp = GetApp().GetApplication<ScreensetupsEditApp>(UIXApplicationStartTarget.Window);
            screensetupsEditApp.Copy(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();
            }, t1scrsh);
        }

        protected async Task EV_ctxtApply()
        {
            if (GetSelectedScreensetup() is not T1SCRSH t1scrsh)
                return;

            await ApplyScreensetup(t1scrsh);
        }
    }
}
