using GameTimeNext.Core.Application.Codetables.Viewmodels;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesEntrysViewController : UIXWindowControllerBase
    {

        private CodetablesEntrysViewModel? _viewModel;

        public CodetablesEntrysViewController(UIXApplication app) : base(app)
        {
        }

        public class CodetablesEntrysViewReturn : UIXViewReturn
        {
        }

        protected override void Init()
        {
            ViewReturn = new CodetablesEntrysViewReturn();
            _viewModel = new CodetablesEntrysViewModel();

            AddIdentifier("T1CTABH", GetApp().T1CTABH!);
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            FnControls.SetVisible(GetWnd().BtnAdd, GetWnd().ViewIndicator.Contains("ED"));
            await BuildDG();
        }

        protected override void BuildImpl()
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

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void Event_Closing()
        {
            GetViewReturn<CodetablesEntrysViewReturn>().Canceled = true;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private CodetablesEntrysApp GetApp() => (CodetablesEntrysApp)App;

        private CodetablesEntrysView GetWnd() => (CodetablesEntrysView)View;

        private async Task BuildDG()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<CodetableEntryDataGridRow> rows = BuildRowsCodetableEntrys();

                View.Dispatcher.Invoke(() =>
                {
                    _viewModel = new CodetablesEntrysViewModel();
                    _viewModel.CodetableEntryDataGridRows = new System.Collections.ObjectModel.ObservableCollection<CodetableEntryDataGridRow>(rows);
                    GetWnd().DataContext = _viewModel;
                    GetApp().Loader.Stop();
                });
            });
        }

        private List<CodetableEntryDataGridRow> BuildRowsCodetableEntrys()
        {
            UIXQuery query = BuildQueryCodetableEntrys();

            List<CodetableEntryDataGridRow> rows = new List<CodetableEntryDataGridRow>();

            using (var reader = query.Execute())
            {

                while (reader.Read())
                {
                    TXCTABD txctabd = new TXCTABD();

                    string txtyp = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXTYP, string.Empty);
                    string txnum = UIXQuery.GetString(reader, K1CTABD.Name, K1CTABD.Fields.TXNUM, string.Empty);

                    T1CTABD t1ctabd = txctabd.Read(txtyp, txnum);

                    CodetableEntryDataGridRow row = GetWnd().DgCodetableEntrys.CreateNewRow<CodetableEntryDataGridRow>();
                    row.COTXTYP = txtyp;
                    row.COTXNUM = txnum;
                    row.CODESCR = t1ctabd.DESCR;
                    row.RowObject = t1ctabd;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryCodetableEntrys()
        {
            UIXQuery query = new UIXQuery(K1CTABD.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXTYP);
            query.AddField(K1CTABD.Name, K1CTABD.Fields.TXNUM);

            query.AddWhere(K1CTABD.Name, K1CTABD.Fields.TXTYP, QueryCompareType.EQUALS, GetApp().T1CTABH!.TXTYP);

            query.AddOrderBy(K1CTABD.Name, K1CTABD.Fields.TXNUM, OrderDirection.ASC);

            return query;
        }

        private void BuildContextMenu(DataGridRow dgRow, CodetableEntryDataGridRow ctRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("CodetablesEntrysDataGridContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            T1CTABD t1ctabd = (T1CTABD)ctRow.RowObject!;

            // Nur im Debugmodus dürfen Development-CTs geändert werden
            if (GetWnd().ViewIndicator.Contains("ED"))
                contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            contextBuilder.AddItem("ctxtView", "View", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.View), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            // Nur im Debugmodus dürfen Development-CTs gelöscht werden
            if (GetWnd().ViewIndicator.Contains("ED") && (!GetApp().T1CTABH!.NRANA && !t1ctabd.TXNUM.StartsWith("D_") || FnSystem.IsDebug()))
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (contextBuilder.HasItems())
                dgRow.ContextMenu = contextBuilder.Build();
            else
                dgRow.ContextMenu = null;
        }

        protected void EV_DgCodetableEntrys_CtxtOpening(FrameworkElement target)
        {
            DataGridRow row = target as DataGridRow;

            if (row == null)
                return;

            if (row.DataContext is not CodetableEntryDataGridRow)
                return;

            BuildContextMenu(row, (CodetableEntryDataGridRow)row.DataContext);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtEdit()
        {
            CodetableEntryDataGridRow row = _viewModel!.SelectedRow!;

            CodetablesEntryEditApp app = GetApp().GetApplication<CodetablesEntryEditApp>();
            app.Edit(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await BuildDG();

            }, (T1CTABD)(row.RowObject!));
        }

        protected void EV_ctxtView()
        {
            CodetableEntryDataGridRow row = _viewModel!.SelectedRow!;
            CodetablesEntryEditApp app = GetApp().GetApplication<CodetablesEntryEditApp>();
            app.View((T1CTABD)(row.RowObject!));
        }

        protected async Task EV_ctxtDelete()
        {
            CodetableEntryDataGridRow row = _viewModel!.SelectedRow!;
            T1CTABD t1ctabd = (T1CTABD)(row.RowObject!);

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();

            CFMBOXResult result = cfmbox.Show("Are you sure you want to delete this entry?", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXCTABD().Delete(t1ctabd.TXTYP, t1ctabd.TXNUM);

                await BuildDG();
            }

        }

        protected void EV_BtnAdd()
        {
            CodetablesEntryEditApp app = GetApp().GetApplication<CodetablesEntryEditApp>();
            app.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await BuildDG();
            }, GetApp().T1CTABH!.TXTYP);

        }


        protected async Task EV_BtnRefresh()
        {
            await BuildDG();
        }
    }
}
