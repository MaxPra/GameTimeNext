using GameTimeNext.Core.Application.ApplicationProperties.Viewmodels;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
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
    public class ApplicationPropertiesViewController : UIXViewControllerBase
    {

        private ApplicationPropertiesViewModel? _viewModel;

        public ApplicationPropertiesViewController(UIXApplication app) : base(app)
        {
            _viewModel = new ApplicationPropertiesViewModel();
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildApplicationPropertiesViewModel();
        }

        protected override void BuildImpl()
        {
        }

        protected override async Task BuildImplAsync()
        {

        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
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
        }
        #endregion

        private ApplicationPropertiesApp GetApp()
        {
            return (ApplicationPropertiesApp)App;
        }

        private ApplicationPropertiesView GetView()
        {
            return (ApplicationPropertiesView)View;
        }

        private async Task BuildApplicationPropertiesViewModel()
        {
            GetApp().Loader.Begin();

            try
            {
                await Task.Run(() =>
                {
                    List<ApplicationPropertiesDataGridRow> rows = BuildRowsApplicationProperties();

                    View.Dispatcher.Invoke(() =>
                    {
                        _viewModel = new ApplicationPropertiesViewModel();
                        _viewModel!.ApplicationPropertiesDataGridRows = new System.Collections.ObjectModel.ObservableCollection<ApplicationPropertiesDataGridRow>(rows);

                        GetView().DataContext = _viewModel;
                    });
                });
            }
            finally
            {
                GetApp().Loader.Stop();
            }
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

                    ApplicationPropertiesDataGridRow row = GetView().DgApplicationProperties.CreateNewRow<ApplicationPropertiesDataGridRow>();
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
                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbId.Text))
                {
                    query.AddWhere(K1APRDH.Name, K1APRDH.Fields.APDID, QueryCompareType.LIKE, GetView().TxbId.Text);
                }

                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbDescription.Text))
                {
                    query.AddWhere(K1APRDH.Name, K1APRDH.Fields.APDES, QueryCompareType.LIKE, GetView().TxbDescription.Text);
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

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtView", "View", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.View), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_DgApplicationProperties_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row)
                return;

            if (row.DataContext is not ApplicationPropertiesDataGridRow applicationPropertiesRow)
                return;

            if (_viewModel != null)
                _viewModel.SelectedRow = applicationPropertiesRow;

            GetView().DgApplicationProperties.SelectedItem = applicationPropertiesRow;

            BuildContextMenu(row);

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        private void OpenEditApp(T1APRDH t1aprdh, bool edit)
        {
            ApplicationPropertiesEditApp editApp = GetApp().GetApplication<ApplicationPropertiesEditApp>(UIXApplicationStartTarget.Window);
            editApp.Properties(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_BtnRefresh();
            }, t1aprdh, edit);
        }

        protected void EV_ctxtEdit()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1APRDH selectedT1aprdh)
                return;

            OpenEditApp(selectedT1aprdh, true);
        }

        protected void EV_ctxtView()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1APRDH selectedT1aprdh)
                return;

            OpenEditApp(selectedT1aprdh, false);
        }

        protected void EV_ctxtCopy()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1APRDH selectedT1aprdh)
                return;

            ApplicationPropertiesEditApp editApp = GetApp().GetApplication<ApplicationPropertiesEditApp>(UIXApplicationStartTarget.Window);
            editApp.Copy(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_BtnRefresh();
            }, selectedT1aprdh);
        }

        protected async Task EV_ctxtDelete()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1APRDH selectedT1aprdh)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show($"Are you sure you want to delete the application properties definition \"{selectedT1aprdh.APDID}\"?", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXAPRDH().Delete(selectedT1aprdh.APDID);

                await EV_BtnRefresh();
            }
        }

        protected void EV_BtnAddApplicationProperties()
        {
            ApplicationPropertiesEditApp editApp = GetApp().GetApplication<ApplicationPropertiesEditApp>(UIXApplicationStartTarget.Window);
            editApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_BtnRefresh();
            });
        }

        protected async Task EV_BtnRefresh()
        {
            await BuildApplicationPropertiesViewModel();
        }
    }
}
