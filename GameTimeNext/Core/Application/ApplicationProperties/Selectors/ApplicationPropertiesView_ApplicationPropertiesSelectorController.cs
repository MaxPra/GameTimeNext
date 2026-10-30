using GameTimeNext.Core.Application.ApplicationProperties.Controller;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
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

namespace GameTimeNext.Core.Application.ApplicationProperties.Selectors
{
    public class ApplicationPropertiesView_ApplicationPropertiesSelectorController : UIXSelectorControllerBase<ApplicationPropertiesApp, ApplicationPropertiesView, UIXDataGrid>
    {
        public ApplicationPropertiesView_ApplicationPropertiesSelectorController(UIXApplication app) : base(app)
        {
        }

        private T1APRDH? GetSelectedDefinition()
        {
            return GetSelectedItem<ApplicationPropertiesDataGridRow>()?.RowObject as T1APRDH;
        }

        private static void BuildContextMenu(DataGridRow dgRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ApplicationPropertiesContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // First entry = double click
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not ApplicationPropertiesDataGridRow applicationPropertiesRow)
                return;

            GetSelector().SelectedItem = applicationPropertiesRow;

            BuildContextMenu(row);

            row.ContextMenu!.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtEdit()
        {
            GetApp().ApplicationPropertiesDetailView!.txbDescription.Focus();
        }

        protected void EV_ctxtCopy()
        {
            if (GetSelectedDefinition() is not T1APRDH t1aprdhSource)
                return;

            // The ID is the primary key, so the copy starts without one and the user has to enter a new ID
            T1APRDH t1aprdh = new TXAPRDH().CreateNew();
            t1aprdh.APDES = t1aprdhSource.APDES;
            t1aprdh.APHPT = t1aprdhSource.APHPT;
            t1aprdh.APMCP = t1aprdhSource.APMCP;

            GetViewController<ApplicationPropertiesViewController>().OpenInDetail(t1aprdh);
        }

        protected async Task EV_ctxtDelete()
        {
            if (GetSelectedDefinition() is not T1APRDH t1aprdh)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", $"the application properties definition {t1aprdh.APDID}"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXAPRDH().Delete(t1aprdh.APDID);

                await GetViewController<ApplicationPropertiesViewController>().BuildListAsync(string.Empty);
            }
        }
    }
}
