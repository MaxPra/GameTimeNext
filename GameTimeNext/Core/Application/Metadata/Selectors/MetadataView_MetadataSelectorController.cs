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
    public class MetadataView_MetadataSelectorController : UIXSelectorControllerBase<MetadataApp, MetadataView, UIXDataGrid>
    {
        public MetadataView_MetadataSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1METAH? GetSelectedMetadata()
        {
            return GetSelectedItem<MetadataDataGridRow>()?.RowObject as T1METAH;
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not MetadataDataGridRow metadataRow)
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
            if (GetSelectedMetadata() is not T1METAH t1metah)
                return;

            MetadataEditApp metadataEditApp = GetApp().GetApplication<MetadataEditApp>(UIXApplicationStartTarget.Window);
            metadataEditApp.Edit(t1metah);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedMetadata() is not T1METAH t1metah)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(
                GetErrorMessage("MET0001", t1metah.MENAM),
                CFMBOXResult.Yes | CFMBOXResult.No,
                CFMBOXIcon.Question);

            if (result != CFMBOXResult.Yes)
                return;

            TFMETAH.DeleteMetadataAndLinkedData(t1metah, deleteTable: true);

            await GetSelector().LoadDataAsync();
        }
    }
}
