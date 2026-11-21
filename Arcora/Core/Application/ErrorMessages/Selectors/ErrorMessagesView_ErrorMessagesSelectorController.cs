using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.ErrorMessages.Controller;
using GameTimeNext.Core.Application.ErrorMessages.Views;
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

namespace GameTimeNext.Core.Application.ErrorMessages.Selectors
{
    public class ErrorMessagesView_ErrorMessagesSelectorController : UIXSelectorControllerBase<ErrorMessagesApp, ErrorMessagesView, UIXDataGrid>
    {
        public ErrorMessagesView_ErrorMessagesSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1ERMSG? GetSelectedErrorMessage()
        {
            return GetSelectedItem<ErrorMessagesDataGridRow>()?.RowObject as T1ERMSG;
        }

        private static void BuildContextMenu(DataGridRow dgRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ErrorMessagesContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtView", "View", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.View), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not ErrorMessagesDataGridRow errorMessagesRow)
                return;

            GetSelector().SelectedItem = errorMessagesRow;

            BuildContextMenu(row);

            row.ContextMenu!.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        private void OpenEditApp(T1ERMSG t1ermsg, bool edit)
        {
            ErrorMessagesEditApp editApp = GetApp().GetApplication<ErrorMessagesEditApp>(UIXApplicationStartTarget.Window);
            editApp.Properties(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();
            }, t1ermsg, edit);
        }

        protected void EV_ctxtEdit()
        {
            if (GetSelectedErrorMessage() is not T1ERMSG t1ermsg)
                return;

            OpenEditApp(t1ermsg, true);
        }

        protected void EV_ctxtView()
        {
            if (GetSelectedErrorMessage() is not T1ERMSG t1ermsg)
                return;

            OpenEditApp(t1ermsg, false);
        }

        protected void EV_ctxtCopy()
        {
            if (GetSelectedErrorMessage() is not T1ERMSG t1ermsg)
                return;

            ErrorMessagesEditApp editApp = GetApp().GetApplication<ErrorMessagesEditApp>(UIXApplicationStartTarget.Window);
            editApp.Copy(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetSelector().LoadDataAsync();
            }, t1ermsg);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedErrorMessage() is not T1ERMSG t1ermsg)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            // Not while it is open elsewhere (record lock); checked before asking
            bool deleted = FnRecordLock.Run(GetApp(), () =>
            {
                if (!cfmbox.ConfirmDelete(GetErrorMessage("GLO0001", $"The error message {t1ermsg.ERMNR}")))
                    return false;

                GetApp().GetDataManager<TXERMSG>().Delete(t1ermsg.ERMNR);
                return true;
            }, t1ermsg);

            if (deleted)
                await GetSelector().LoadDataAsync();
        }
    }
}
