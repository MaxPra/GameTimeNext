using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.ViewModel;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
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
using UIX.ViewController.Engine.Viewmodel;
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

            // Profile rows: start a new playthrough (only while none is running, same as in the profiles view)
            if (ptRow.RowObject is T1PROFI t1profi)
            {
                if (!TFPLTHR.HasCurrentPlaythrough(t1profi.PFID))
                    contextBuilder.AddItem("ctxtStartNewPlaythrough", "Start new playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(""), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

                dgRow.ContextMenu = contextBuilder.HasItems() ? contextBuilder.Build() : null;
                return;
            }

            if (ptRow.RowObject is not T1PLTHR t1plthr)
            {
                dgRow.ContextMenu = null;
                return;
            }

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (t1plthr.PTTY != PlaythroughType.INITIAL_PLAYTHROUGH)
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (!t1plthr.PTCO && !t1plthr.PTCA)
            {
                contextBuilder.AddItem("ctxtCompletePlaythrough", "Complete Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon("\uE930"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

                if (!t1plthr.PTPA)
                    contextBuilder.AddItem("ctxtPausePlaythrough", "Pause Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Pause), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

                contextBuilder.AddItem("ctxtCancelPlaythrough", "Cancel Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon("\uE711"), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
            }
            if (t1plthr.PTPA)
            {
                contextBuilder.AddItem("ctxtUnpausePlaythrough", "Unpause Playthrough", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Refresh), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);
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
                List<PlaythroughDataGridRow> rows = BuildRows();

                View.Dispatcher.Invoke(() =>
                {
                    _viewModel = new PlaythroughsViewModel();
                    _viewModel!.PlaythroughDataGridRows = new System.Collections.ObjectModel.ObservableCollection<PlaythroughDataGridRow>(rows);

                    GetView().DataContext = _viewModel;

                    GetApp().Loader.Stop();
                }, DispatcherPriority.Normal);
            });
        }

        /// <summary>
        /// Builds the tree rows: one row per profile (ordered by name) with its playthroughs below.
        /// </summary>
        private List<PlaythroughDataGridRow> BuildRows()
        {
            List<PlaythroughDataGridRow> rows = new List<PlaythroughDataGridRow>();

            UIXQuery query = BuildQueryPlaythroughs();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long ptid = UIXQuery.GetInt64(reader, K1PLTHR.Name, K1PLTHR.Fields.PTID);

                    T1PLTHR t1plthr = new TXPLTHR().Read(ptid);

                    long pfid = BuildRowsProfiles(rows, t1plthr.PFID);

                    BuildRowsPlaythroughs(rows, pfid, t1plthr);
                }
            }

            return rows;
        }

        /// <summary>
        /// Adds the parent row of the profile, if it does not exist yet, and returns the PFID.
        /// </summary>
        private long BuildRowsProfiles(List<PlaythroughDataGridRow> rows, long pfid)
        {
            UIXStructureKey structureKey = new UIXStructureKey().Add(pfid);

            if (rows.Exists(r => r.StructureKey == structureKey))
                return pfid;

            T1PROFI t1profi = new TXPROFI().Read(pfid);

            PlaythroughDataGridRow row = GetView().DgPlaythroughs.CreateNewRow<PlaythroughDataGridRow>();
            row.StructureKey = structureKey;
            // Initially expanded; after a refresh the grid restores the state the user left
            row.IsExpanded = true;
            row.CONAME = t1profi?.GANA ?? string.Empty;
            row.COPTPLMI = GetProfilePlaytimeInMinutes(pfid);
            row.COPTPL = FnTime.FormatMinutesAsHoursAndMinutes(row.COPTPLMI, FnTime.NoValue);
            row.RowObject = t1profi;

            rows.Add(row);

            return pfid;
        }

        /// <summary>
        /// Playtime of the profile: sum of the sessions of its playthroughs that match the filters of the list.
        /// </summary>
        private double GetProfilePlaytimeInMinutes(long pfid)
        {
            UIXQuery query = BuildQueryProfilePlaytime(pfid);

            using (var reader = query.Execute())
                if (reader.Read())
                    return UIXQuery.GetDouble(reader, "TotalPlaytime");

            return 0;
        }

        /// <summary>
        /// Adds the row of the playthrough below its profile.
        /// </summary>
        private void BuildRowsPlaythroughs(List<PlaythroughDataGridRow> rows, long pfid, T1PLTHR t1plthr)
        {
            PlaythroughDataGridRow row = GetView().DgPlaythroughs.CreateNewRow<PlaythroughDataGridRow>();
            row.StructureKey = new UIXStructureKey().Add(pfid).Add(t1plthr.PTID);
            row.IsPlaythroughRow = true;
            row.CONAME = t1plthr.PTDE;
            row.COPTTY = TFCTABD.GetDescription("pL", t1plthr.PTTY);
            row.COPTCO = t1plthr.PTCO;
            row.COPTCA = t1plthr.PTCA;
            row.COPTPA = t1plthr.PTPA;
            row.COPTPLMI = TFPLTHR.GetPlaytimeInMinutes(t1plthr.PTID);
            row.COPTPL = FnTime.FormatMinutesAsHoursAndMinutes(row.COPTPLMI, FnTime.NoValue);
            row.RowObject = t1plthr;

            rows.Add(row);
        }

        private UIXQuery BuildQueryPlaythroughs()
        {
            UIXQuery query = new UIXQuery(K1PLTHR.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1PLTHR.Name, K1PLTHR.Fields.PTID);

            // Profile join for the name filter and the order of the profile rows
            UIXQueryTable table_profiles = query.AddJoinTable(K1PROFI.Name, JoinType.INNER);
            table_profiles.AddJoinCondition(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddOrderBy(K1PROFI.Name, K1PROFI.Fields.GANA, OrderDirection.ASC);
            query.AddOrderBy(K1PLTHR.Name, K1PLTHR.Fields.PTID, OrderDirection.ASC);

            AddWherePlaythroughFilters(query);

            return query;
        }

        private UIXQuery BuildQueryProfilePlaytime(long pfid)
        {
            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddSum(K1SESSI.Name, K1SESSI.Fields.PLTI, "TotalPlaytime");

            UIXQueryTable table_playthroughs = query.AddJoinTable(K1PLTHR.Name, JoinType.INNER);
            table_playthroughs.AddJoinCondition(K1SESSI.Name, K1SESSI.Fields.PTID, QueryCompareType.EQUALS, K1PLTHR.Name, K1PLTHR.Fields.PTID);

            UIXQueryTable table_profiles = query.AddJoinTable(K1PROFI.Name, JoinType.INNER);
            table_profiles.AddJoinCondition(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, K1PROFI.Name, K1PROFI.Fields.PFID);

            query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, pfid);

            AddWherePlaythroughFilters(query);

            return query;
        }

        /// <summary>
        /// Filters of the toolbar. The query has to join T1PLTHR and T1PROFI.
        /// </summary>
        private void AddWherePlaythroughFilters(UIXQuery query)
        {
            View.Dispatcher.Invoke(() =>
            {

                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbProfileName.Text))
                {
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

                if (GetView().ChbPaused.IsChecked == true)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PTPA, QueryCompareType.EQUALS, true);

                if (GetApp().AppRunSelections.Pfid > 0)
                    query.AddWhere(K1PLTHR.Name, K1PLTHR.Fields.PFID, QueryCompareType.EQUALS, GetApp().AppRunSelections.Pfid);

            });
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

        protected void EV_ctxtStartNewPlaythrough()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PROFI selectedT1profi)
                return;

            PlaythroughEditApp editApp = GetApp().GetApplication<PlaythroughEditApp>();
            editApp.CreateNew(async (r) =>
            {
                if (!r.HasChanged)
                    return;

                await BuildDataGridAsync();

                GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;

            }, selectedT1profi);
        }

        protected async Task EV_ctxtCompletePlaythrough()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref selectedT1plthr, TFPLTHR.PlaythroughStatus.Completed);

            await BuildDataGridAsync();

            GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;

        }

        protected async Task EV_ctxtCancelPlaythrough()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref selectedT1plthr, TFPLTHR.PlaythroughStatus.Canceled);

            await BuildDataGridAsync();

            GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;
        }

        protected async Task EV_ctxtPausePlaythrough()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref selectedT1plthr, TFPLTHR.PlaythroughStatus.Paused);

            await BuildDataGridAsync();

            GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;
        }

        protected async Task EV_ctxtUnpausePlaythrough()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            T1PLTHR currentPlaythrough = TFPLTHR.GetCurrentPlaythrough(selectedT1plthr.PFID);
            TFPLTHR.UpdatePlaythroughStatus(GetApp(), ref selectedT1plthr, TFPLTHR.PlaythroughStatus.Open, currentT1plthr: currentPlaythrough);

            await BuildDataGridAsync();

            GetViewReturn<PlaythroughsViewReturn>().HasChanged = true;
        }

        protected async Task EV_ctxtDelete()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1PLTHR selectedT1plthr)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0002", "this playthrough"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Warning);

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
