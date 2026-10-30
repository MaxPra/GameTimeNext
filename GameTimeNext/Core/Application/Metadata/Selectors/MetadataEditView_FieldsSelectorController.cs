using GameTimeNext.Core.Application.Metadata.Controller;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using GameTimeNext.Core.Application.Profiles;
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

namespace GameTimeNext.Core.Application.Metadata.Selectors
{
    public class MetadataEditView_FieldsSelectorController : UIXSelectorControllerBase<MetadataEditApp, MetadataEditView, UIXDataGrid>
    {
        public MetadataEditView_FieldsSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1METAP? GetSelectedPosition()
        {
            return GetSelectedItem<MetadataPosDataGridRow>()?.RowObject as T1METAP;
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not MetadataPosDataGridRow metadataRow)
                return;

            GetSelector().SelectedItem = metadataRow;

            BuildContextMenu(row);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        private static void BuildContextMenu(DataGridRow row)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            row.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
        }

        protected void EV_ctxtEdit()
        {
            if (GetSelectedPosition() is not T1METAP t1metap)
                return;

            MetadataPosEditApp metadataPosEditApp = GetApp().GetApplication<MetadataPosEditApp>(UIXApplicationStartTarget.Window);
            metadataPosEditApp.Edit(async (result) =>
            {
                if (result.HasChanged)
                {
                    await GetSelector().LoadDataAsync();

                    GetViewController<MetadataEditViewController>().MarkGenerationRequired();
                }

            }, t1metap);
        }

        protected void EV_ctxtDelete()
        {
            if (GetSelectedPosition() is not T1METAP t1metap)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", "this metadata position"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXMETAP().Delete(t1metap.MENAM, t1metap.PONAM);

                GetApp().T1METAH!.GENER = false;
                new TXMETAH().Save(GetApp().T1METAH!);
            }
        }
    }
}
