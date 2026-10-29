using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Screensetups.Viewmodels;
using GameTimeNext.Core.Application.Screensetups.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using UIXMonitorTweaks;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Screensetups.Controller
{
    public class ScreensetupsViewController : UIXViewControllerBase<ScreensetupsApp, ScreensetupsView>
    {

        private ScreensetupsViewModel? _viewModel;

        public ScreensetupsViewController(UIXApplication app) : base(app)
        {
            _viewModel = new ScreensetupsViewModel();
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
            await BuildScreensetupViewModel();
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
        }

        protected override async Task HandleUIEventImplAsync(FrameworkElement source, string eventName, RoutedEventArgs args)
        {
            // The switch button lives in a cell template and the engine normalizes the source to
            // its DataGridRow, so the button itself comes from OriginalSource
            if (eventName == UIXEventNames.Button.Click
                && args.OriginalSource is FrameworkElement { Name: "BtnApplyScreensetup" } button
                && button.DataContext is ScreensetupDataGridRow screensetupRow)
            {
                if (_viewModel != null)
                    _viewModel.SelectedRow = screensetupRow;

                await ApplyScreensetup(screensetupRow);
            }
        }
        #endregion

        private async Task BuildScreensetupViewModel()
        {
            GetApp().Loader.Begin();

            try
            {
                await Task.Run(() =>
                {
                    List<ScreensetupDataGridRow> screensetupDataGridRows = BuildRowsScreensetups();

                    View.Dispatcher.Invoke(() =>
                    {
                        _viewModel = new ScreensetupsViewModel();
                        _viewModel!.ScreensetupDataGridRows = new System.Collections.ObjectModel.ObservableCollection<ScreensetupDataGridRow>(screensetupDataGridRows);

                        GetView().DataContext = _viewModel;

                    });

                });
            }
            finally
            {
                GetApp().Loader.Stop();
            }
        }

        private List<ScreensetupDataGridRow> BuildRowsScreensetups()
        {
            List<ScreensetupDataGridRow> rows = new List<ScreensetupDataGridRow>();

            long selectedScreensetup = AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup;

            UIXQuery query = BuildQueryScreensetups();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long ssid = UIXQuery.GetInt64(reader, K1SCRSH.Name, K1SCRSH.Fields.SSID);

                    T1SCRSH? t1scrsh = new TXSCRSH().Read(ssid);

                    if (t1scrsh == null)
                        continue;

                    List<T1SCRSP> t1scrsps = TFSCRSP.GetMonitorsOfScreensetup(ssid);

                    ScreensetupDataGridRow row = GetView().DgScreensetups.CreateNewRow<ScreensetupDataGridRow>();
                    row.COSSNA = t1scrsh.SSNA;
                    row.COSSDE = t1scrsh.SSDE;
                    row.COMONI = CFScreensetupsApp.FormatMonitors(t1scrsps);
                    row.COAUDI = CFScreensetupsApp.FormatAudioDevice(t1scrsh);
                    row.COISACT = t1scrsh.SSID == selectedScreensetup;
                    row.RowObject = t1scrsh;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryScreensetups()
        {
            UIXQuery query = new UIXQuery(K1SCRSH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SCRSH.Name, K1SCRSH.Fields.SSID);

            View.Dispatcher.Invoke(() =>
             {
                 if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbName.Text))
                 {
                     query.AddWhere(K1SCRSH.Name, K1SCRSH.Fields.SSNA, QueryCompareType.LIKE, GetView().TxbName.Text);
                 }
             });

            query.SetTopX(1000);
            query.AddOrderBy(K1SCRSH.Name, K1SCRSH.Fields.SSNA, OrderDirection.ASC);

            return query;
        }

        private void BuildContextMenu(DataGridRow dgRow, ScreensetupDataGridRow ssRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ScreensetupsDataGridContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtApply","Switch to this screensetup", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Play), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        private async Task ApplyScreensetup(ScreensetupDataGridRow screensetupRow)
        {
            if (screensetupRow.RowObject is not T1SCRSH t1scrsh)
                return;

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

            await EV_BtnRefresh();
        }

        protected void EV_DgScreensetups_CtxtOpening(FrameworkElement target)
        {
            DataGridRow? row = target as DataGridRow;

            if (row == null)
                return;

            if (row.DataContext is not ScreensetupDataGridRow screensetupRow)
                return;

            if (_viewModel != null)
                _viewModel.SelectedRow = screensetupRow;

            GetView().DgScreensetups.SelectedItem = screensetupRow;

            BuildContextMenu(row, screensetupRow);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected async Task EV_ctxtDelete()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1SCRSH selectedT1scrsh)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", "this screensetup"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                TFSCRSH.DeleteScreensetupAndMonitors(selectedT1scrsh);

                if (AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup == selectedT1scrsh.SSID)
                {
                    AppEnvironment.GetAppConfig().UserSettings.SelectedScreensetup = 0;
                    AppEnvironment.SaveAppConfig();
                }

                await EV_BtnRefresh();
            }
        }

        protected void EV_ctxtEdit()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1SCRSH selectedT1scrsh)
                return;

            ScreensetupsEditApp screensetupsEditApp = GetApp().GetApplication<ScreensetupsEditApp>(UIXApplicationStartTarget.Window);
            screensetupsEditApp.Edit(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_BtnRefresh();
            }, selectedT1scrsh);
        }

        protected void EV_ctxtCopy()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1SCRSH selectedT1scrsh)
                return;

            ScreensetupsEditApp screensetupsEditApp = GetApp().GetApplication<ScreensetupsEditApp>(UIXApplicationStartTarget.Window);
            screensetupsEditApp.Copy(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_BtnRefresh();
            }, selectedT1scrsh);
        }

        protected async Task EV_ctxtApply()
        {
            if (_viewModel?.SelectedRow is not ScreensetupDataGridRow selectedRow)
                return;

            await ApplyScreensetup(selectedRow);
        }

        protected void EV_BtnAddScreensetup()
        {
            ScreensetupsEditApp screensetupsEditApp = GetApp().GetApplication<ScreensetupsEditApp>(UIXApplicationStartTarget.Window);
            screensetupsEditApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_BtnRefresh();
            });
        }

        protected void EV_BtnIdentify()
        {
            IReadOnlyList<string> identifiedMonitors = UIXMonitorManager.Identify(CFScreensetupsApp.CreateIdentifyOptions());

            // Identify only works on monitors that are switched on
            if (identifiedMonitors.Count == 0)
            {
                CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);
                cfmbox.Show(GetErrorMessage("SCS0003"), CFMBOXResult.Ok, CFMBOXIcon.Info);
            }
        }

        protected async Task EV_BtnRefresh()
        {
            await BuildScreensetupViewModel();
        }
    }
}
