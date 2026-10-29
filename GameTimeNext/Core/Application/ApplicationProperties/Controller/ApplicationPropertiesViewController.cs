using GameTimeNext.Core.Application.ApplicationProperties.DataWrapper;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.ApplicationProperties.Controller
{
    public class ApplicationPropertiesViewController : UIXViewControllerBase<ApplicationPropertiesApp, ApplicationPropertiesView>
    {
        private ApplicationPropertiesDataWrapper? _dataWrapper;

        public ApplicationPropertiesViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<ApplicationPropertiesDataWrapper>();
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            // Nothing is selected yet: no edit area
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, false);
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildListAsync(string.Empty);
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, true);
        }
        #endregion

        /// <summary>
        /// Rebuilds the list and selects the definition with the given ID, which opens it in the edit area.
        /// Empty = no selection, the edit area is closed.
        /// </summary>
        public async Task BuildListAsync(string selectApdid)
        {
            GetApp().Loader.Begin();

            try
            {
                List<ApplicationPropertiesDataGridRow> rows = await Task.Run(BuildRowsApplicationProperties);

                View.Dispatcher.Invoke(() =>
                {
                    GetView().dgApplicationProperties.ItemsSource = new ObservableCollection<ApplicationPropertiesDataGridRow>(rows);

                    ApplicationPropertiesDataGridRow? selectedRow = FnString.IsNullEmptyOrWhitespace(selectApdid)
                        ? null
                        : rows.FirstOrDefault(r => r.APAPDID == selectApdid);

                    if (selectedRow != null)
                    {
                        GetView().dgApplicationProperties.SelectedItem = selectedRow;
                        GetView().dgApplicationProperties.ScrollIntoView(selectedRow);
                    }
                    else
                    {
                        CloseDetail();
                    }
                });
            }
            finally
            {
                GetApp().Loader.Stop();
            }
        }

        /// <summary>
        /// Closes the edit area without saving and clears the selection.
        /// </summary>
        public void CloseDetail()
        {
            GetView().dgApplicationProperties.SelectedItem = null;

            _dataWrapper!.Set(null);
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, false);
        }

        /// <summary>
        /// Opens the definition, a new and unsaved one or a saved one, in the edit area.
        /// </summary>
        private void OpenInDetail(T1APRDH t1aprdh)
        {
            GetView().dgApplicationProperties.SelectedItem = null;

            _dataWrapper!.Set(t1aprdh);
            FnControls.SetVisible(GetView().cpApplicationPropertiesDetail, true);

            _dataWrapper.TargetController.Open(true);
        }

        private T1APRDH? GetSelectedDefinition()
        {
            return (GetView().dgApplicationProperties.SelectedItem as ApplicationPropertiesDataGridRow)?.RowObject as T1APRDH;
        }

        private List<ApplicationPropertiesDataGridRow> BuildRowsApplicationProperties()
        {
            List<ApplicationPropertiesDataGridRow> rows = new List<ApplicationPropertiesDataGridRow>();

            UIXQuery query = BuildQueryApplicationProperties();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string apdid = UIXQuery.GetString(reader, K1APRDH.Name, K1APRDH.Fields.APDID, string.Empty);

                    T1APRDH? t1aprdh = new TXAPRDH().Read(apdid);

                    if (t1aprdh == null)
                        continue;

                    ApplicationPropertiesDataGridRow row = GetView().dgApplicationProperties.CreateNewRow<ApplicationPropertiesDataGridRow>();
                    row.APAPDID = t1aprdh.APDID;
                    row.APAPDES = t1aprdh.APDES;
                    row.APAPMCP = t1aprdh.APMCP;
                    row.RowObject = t1aprdh;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryApplicationProperties()
        {
            UIXQuery query = new UIXQuery(K1APRDH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1APRDH.Name, K1APRDH.Fields.APDID);

            View.Dispatcher.Invoke(() =>
            {
                if (!FnString.IsNullEmptyOrWhitespace(GetView().txbId.Text))
                {
                    query.AddWhere(K1APRDH.Name, K1APRDH.Fields.APDID, QueryCompareType.LIKE, GetView().txbId.Text);
                }

                if (!FnString.IsNullEmptyOrWhitespace(GetView().txbDescription.Text))
                {
                    query.AddWhere(K1APRDH.Name, K1APRDH.Fields.APDES, QueryCompareType.LIKE, GetView().txbDescription.Text);
                }
            });

            query.SetTopX(1000);
            query.AddOrderBy(K1APRDH.Name, K1APRDH.Fields.APDID, OrderDirection.ASC);

            return query;
        }

        private void BuildContextMenu(DataGridRow dgRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ApplicationPropertiesContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            // First entry = double click
            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_dgApplicationProperties_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row)
                return;

            if (row.DataContext is not ApplicationPropertiesDataGridRow applicationPropertiesRow)
                return;

            GetView().dgApplicationProperties.SelectedItem = applicationPropertiesRow;

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

            OpenInDetail(t1aprdh);
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

                await BuildListAsync(string.Empty);
            }
        }

        protected void EV_btnAddApplicationProperties()
        {
            OpenInDetail(new TXAPRDH().CreateNew());
        }

        protected async Task EV_btnRefresh()
        {
            await BuildListAsync(GetSelectedDefinition()?.APDID ?? string.Empty);
        }
    }
}
