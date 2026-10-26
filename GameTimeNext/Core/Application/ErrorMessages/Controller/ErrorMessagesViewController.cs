using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.ErrorMessages.Viewmodels;
using GameTimeNext.Core.Application.ErrorMessages.Views;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.ErrorMessages.Controller
{
    public class ErrorMessagesViewController : UIXViewControllerBase
    {

        private ErrorMessagesViewModel? _viewModel;

        public ErrorMessagesViewController(UIXApplication app) : base(app)
        {
            _viewModel = new ErrorMessagesViewModel();
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
            await BuildErrorMessagesViewModel();
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

        private ErrorMessagesApp GetApp()
        {
            return (ErrorMessagesApp)App;
        }

        private ErrorMessagesView GetView()
        {
            return (ErrorMessagesView)View;
        }

        private async Task BuildErrorMessagesViewModel()
        {
            GetApp().Loader.Begin();

            try
            {
                await Task.Run(() =>
                {
                    List<ErrorMessagesDataGridRow> rows = BuildRowsErrorMessages();

                    View.Dispatcher.Invoke(() =>
                    {
                        _viewModel = new ErrorMessagesViewModel();
                        _viewModel!.ErrorMessagesDataGridRows = new System.Collections.ObjectModel.ObservableCollection<ErrorMessagesDataGridRow>(rows);

                        GetView().DataContext = _viewModel;
                    });
                });
            }
            finally
            {
                GetApp().Loader.Stop();
            }
        }

        private List<ErrorMessagesDataGridRow> BuildRowsErrorMessages()
        {
            List<ErrorMessagesDataGridRow> rows = new List<ErrorMessagesDataGridRow>();

            UIXQuery query = BuildQueryErrorMessages();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string ermnr = UIXQuery.GetString(reader, K1ERMSG.Name, K1ERMSG.Fields.ERMNR, string.Empty);

                    T1ERMSG? t1ermsg = new TXERMSG().Read(ermnr);

                    if (t1ermsg == null)
                        continue;

                    ErrorMessagesDataGridRow row = GetView().dgErrorMessages.CreateNewRow<ErrorMessagesDataGridRow>();
                    row.ERERMNR = t1ermsg.ERMNR;
                    row.ERERMSG = t1ermsg.ERMSG;
                    row.ERERHIN = t1ermsg.ERHIN;
                    row.ERCHAT = t1ermsg.CHAT == DateTime.MinValue ? string.Empty : t1ermsg.CHAT.ToString("g", CultureInfo.CurrentCulture);
                    row.ERCHUS = t1ermsg.CHUS;
                    row.RowObject = t1ermsg;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryErrorMessages()
        {
            UIXQuery query = new UIXQuery(K1ERMSG.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1ERMSG.Name, K1ERMSG.Fields.ERMNR);

            View.Dispatcher.Invoke(() =>
            {
                if (!FnString.IsNullEmptyOrWhitespace(GetView().txbNumber.Text))
                {
                    query.AddWhere(K1ERMSG.Name, K1ERMSG.Fields.ERMNR, QueryCompareType.LIKE, GetView().txbNumber.Text);
                }

                if (!FnString.IsNullEmptyOrWhitespace(GetView().txbMessage.Text))
                {
                    query.AddWhere(K1ERMSG.Name, K1ERMSG.Fields.ERMSG, QueryCompareType.LIKE, GetView().txbMessage.Text);
                }
            });

            query.SetTopX(1000);
            query.AddOrderBy(K1ERMSG.Name, K1ERMSG.Fields.ERMNR, OrderDirection.ASC);

            return query;
        }

        private void BuildContextMenu(DataGridRow dgRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ErrorMessagesContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtView", "View", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.View), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_dgErrorMessages_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row)
                return;

            if (row.DataContext is not ErrorMessagesDataGridRow errorMessagesRow)
                return;

            if (_viewModel != null)
                _viewModel.SelectedRow = errorMessagesRow;

            GetView().dgErrorMessages.SelectedItem = errorMessagesRow;

            BuildContextMenu(row);

            row.ContextMenu.PlacementTarget = row;
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

                await EV_btnRefresh();
            }, t1ermsg, edit);
        }

        protected void EV_ctxtEdit()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1ERMSG selectedT1ermsg)
                return;

            OpenEditApp(selectedT1ermsg, true);
        }

        protected void EV_ctxtView()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1ERMSG selectedT1ermsg)
                return;

            OpenEditApp(selectedT1ermsg, false);
        }

        protected void EV_ctxtCopy()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1ERMSG selectedT1ermsg)
                return;

            ErrorMessagesEditApp editApp = GetApp().GetApplication<ErrorMessagesEditApp>(UIXApplicationStartTarget.Window);
            editApp.Copy(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_btnRefresh();
            }, selectedT1ermsg);
        }

        protected async Task EV_ctxtDelete()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1ERMSG selectedT1ermsg)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", $"the error message {selectedT1ermsg.ERMNR}"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXERMSG().Delete(selectedT1ermsg.ERMNR);

                await EV_btnRefresh();
            }
        }

        protected void EV_btnAddErrorMessage()
        {
            ErrorMessagesEditApp editApp = GetApp().GetApplication<ErrorMessagesEditApp>(UIXApplicationStartTarget.Window);
            editApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_btnRefresh();
            });
        }

        protected async Task EV_btnRefresh()
        {
            await BuildErrorMessagesViewModel();
        }
    }
}
