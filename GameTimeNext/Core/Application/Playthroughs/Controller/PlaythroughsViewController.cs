using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.ViewModel;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Playthroughs.Controller
{
    public class PlaythroughsViewController : UIXViewControllerBase
    {
        private PlaythroughsViewModel? _viewModel;

        public PlaythroughsViewController(UIXApplication app) : base(app)
        {
            _viewModel = new PlaythroughsViewModel();
        }

        public class PlaythroughsViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new PlaythroughsViewReturn();

            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {

            GetView().TxbProfileName.Text = GetApp().AppRunSelections.Gana;

            FnControls.SetEnabled(GetView().TxbProfileName, FnString.IsNullEmptyOrWhitespace(GetApp().AppRunSelections.Gana) && !GetView().ViewIndicator.Contains("SE"));
        }

        protected override async Task BuildFirstImplAsync()
        {
            if (GetView().ViewIndicator.Contains("SE"))
                await BuildDataGridAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override async Task BuildImplAsync()
        {
        }

        protected override void Check()
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

        protected override void Event_Closing()
        {
            Exit(true);
        }

        private PlaythroughsApp GetApp()
        {
            return (PlaythroughsApp)App;
        }

        private PlaythroughsView GetView()
        {
            return (PlaythroughsView)View;
        }

        protected void EV_DgPlaythroughs_CtxtOpening(FrameworkElement target)
        {
            DataGridRow row = target as DataGridRow;

            if (row == null)
                return;

            if (row.DataContext is not PlaythroughDataGridRow ptRow)
                return;

            if (_viewModel != null)
                _viewModel.SelectedRow = ptRow;

            GetView().DgPlaythroughs.SelectedItem = ptRow;

            BuildContextMenu(row, ptRow);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        private void BuildContextMenu(DataGridRow dgRow, PlaythroughDataGridRow ptRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("PlaythroughDataGridContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            T1PLTHR t1plthr = (T1PLTHR)ptRow.RowObject!;

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (t1plthr.PTTY != PlaythroughType.INITIAL_PLAYTHROUGH)
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (!t1plthr.PTCO && !t1plthr.PTCA)
            {
                contextBuilder.AddItem("ctxtCompletePlaythrough", "Complete Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon("\uE930"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
                contextBuilder.AddItem("ctxtCancelPlaythrough", "Cancel Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon("\uE711"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            }

            if (contextBuilder.HasItems())
                dgRow.ContextMenu = contextBuilder.Build();
            else
                dgRow.ContextMenu = null;
        }

        private async Task BuildDataGridAsync()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<PlaythroughDataGridRow> rows = BuildRowsPlaythroughs();

                View.Dispatcher.Invoke(() =>
                {
                    _viewModel = new PlaythroughsViewModel();
                    _viewModel!.PlaythroughDataGridRows = new System.Collections.ObjectModel.ObservableCollection<PlaythroughDataGridRow>(rows);

                    GetView().DataContext = _viewModel;

                    GetApp().Loader.Stop();
                }, DispatcherPriority.Normal);
            });
        }

        private List<PlaythroughDataGridRow> BuildRowsPlaythroughs()
        {
            List<PlaythroughDataGridRow> rows = new List<PlaythroughDataGridRow>();

            UIXQuery query = BuildQueryPlaythroughs();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {

                    long ptid = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PTID);

                    TXPLTHR txplthr = new TXPLTHR();
                    T1PLTHR t1plthr = txplthr.Read(ptid);

                    PlaythroughDataGridRow row = GetView().DgPlaythroughs.CreateNewRow<PlaythroughDataGridRow>();
                    row.COGANA = TFPROFI.GetProfileName(t1plthr.PFID);
                    row.COPTDE = t1plthr.PTDE;
                    row.COPTTY = TFCTABD.GetDescription("pL", t1plthr.PTTY);
                    row.COPTCA = t1plthr.PTCA;
                    row.COPTCO = t1plthr.PTCO;
                    row.RowObject = t1plthr;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryPlaythroughs()
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);

            View.Dispatcher.Invoke(() =>
            {

                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbProfileName.Text))
                {
                    UIXQueryTable table_profiles = query.AddJoinTable(K1PROFI.Name, JoinType.INNER);
                    table_profiles.AddJoinCondition(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

                    query.AddWhere(K1PROFI.Name, K1PROFI.Fields.GANA, QueryCompareType.LIKE, GetView().TxbProfileName.Text);
                }

                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbDescription.Text))
                {
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTDE, QueryCompareType.LIKE, GetView().TxbDescription.Text);
                }


                if (!FnString.IsNullEmptyOrWhitespace(GetView().CmbPlaythroughType.SelectedValue?.ToString()))
                {
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTTY, QueryCompareType.EQUALS, GetView().CmbPlaythroughType.SelectedValue?.ToString());
                }

                if (GetView().ChbCompleted.IsChecked == true)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCO, QueryCompareType.EQUALS, true);

                if (GetView().ChbCanceled.IsChecked == true)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTCA, QueryCompareType.EQUALS, true);

                if (GetApp().AppRunSelections.Pfid > 0)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, GetApp().AppRunSelections.Pfid);

            });

            return query;
        }

        protected void EV_ctxtEdit()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            PlaythroughEditApp editApp = GetApp().GetApplication<PlaythroughEditApp>();
            editApp.Edit(async (r) =>
            {
                if (!r.HasChanged)
                    return;

                await BuildDataGridAsync();

            }, selectedT1plthr);
        }

        protected async Task EV_ctxtCompletePlaythroughAsync()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            // Playthrough als Abgeschlossen markieren
            selectedT1plthr.PTCO = true;

            new TXPLTHR().Save(selectedT1plthr);

            await BuildDataGridAsync();

            GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;

        }

        protected async Task EV_ctxtCancelPlaythrough()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            string text = "Do you really want to cancel your current playthrough?\nYou won't be able to undo this action!\n\nInformation: The gametime of this playthrough will be added to your overall gametime!";
            CFMBOXResult result = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show("Question", text, CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.No)
                return;

            // Playthrough canceln
            selectedT1plthr.PTCA = true;

            new TXPLTHR().Save(selectedT1plthr);

            await BuildDataGridAsync();

            GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;
        }

        protected async Task EV_ctxtDelete()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show("Delete Playthrough", "Are you sure you want to delete this playthrough?\nAll linked sessions will also be deleted!", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Warning);

            if (result == CFMBOXResult.Yes)
            {
                TFPLTHR.DeletePlaythroughAndSessions(selectedT1plthr.PTID);

                GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;

                await BuildDataGridAsync();
            }
        }

        protected async void EV_BtnRefresh()
        {
            await BuildDataGridAsync();
        }
    }
}
