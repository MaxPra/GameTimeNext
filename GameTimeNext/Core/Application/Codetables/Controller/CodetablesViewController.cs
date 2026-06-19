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
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesViewController : UIXViewControllerBase
    {

        private CodetablesViewModel? _viewModel;

        public CodetablesViewController(UIXApplication app) : base(app)
        {
            _viewModel = new CodetablesViewModel();
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            // Hinzufügenbutton nur bei Debug / Developmentmodus anzeigen
            FnControls.SetVisible(GetView().BtnAddCodetable, FnSystem.IsDebug());
        }

        protected override async Task BuildFirstImplAsync()
        {

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
        #endregion

        private CodetablesApp GetApp()
        {
            return (CodetablesApp)App;
        }

        private CodetablesView GetView()
        {
            return (CodetablesView)View;
        }

        private async Task BuildCodetableViewModel()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<CodetableDataGridRow> codetableDataGridRows = BuildRowsCodetables();

                View.Dispatcher.Invoke(() =>
                {
                    _viewModel = new CodetablesViewModel();
                    _viewModel!.CodetableDataGridRows = new System.Collections.ObjectModel.ObservableCollection<CodetableDataGridRow>(codetableDataGridRows);

                    GetView().DataContext = _viewModel;

                    GetApp().Loader.Stop();
                });
            });
        }

        private List<CodetableDataGridRow> BuildRowsCodetables()
        {

            List<CodetableDataGridRow> rows = new List<CodetableDataGridRow>();

            UIXQuery query = BuildQueryCodetables();

            string s = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    TXCTABH txctabd = new TXCTABH();

                    string txtyp = UIXQuery.GetString(reader, K1CTABH.Name, K1CTABH.Fields.TXTYP, string.Empty);

                    T1CTABH t1ctabd = txctabd.Read(txtyp);

                    CodetableDataGridRow row = GetView().DgCodetables.CreateNewRow<CodetableDataGridRow>();
                    row.COTXTYP = t1ctabd.TXTYP;
                    row.CODESCR = t1ctabd.DESCR;
                    row.COPERMI = TFCTABD.GetDescription("cP", t1ctabd.PERMI);
                    row.RowObject = t1ctabd;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryCodetables()
        {
            UIXQuery query = new UIXQuery(K1CTABH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1CTABH.Name, K1CTABH.Fields.TXTYP);

            View.Dispatcher.Invoke(() =>
             {
                 if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbTextType.Text))
                 {
                     query.AddWhere(K1CTABH.Name, K1CTABH.Fields.TXTYP, QueryCompareType.EQUALS, GetView().TxbTextType.Text);
                 }

                 if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbDescription.Text))
                 {
                     query.AddWhere(K1CTABH.Name, K1CTABH.Fields.DESCR, QueryCompareType.LIKE, GetView().TxbDescription.Text);
                 }

                 if (!FnString.IsNullEmptyOrWhitespace(GetView().CmbPermission.SelectedValue.ToString()!))
                 {
                     query.AddWhere(K1CTABH.Name, K1CTABH.Fields.PERMI, QueryCompareType.EQUALS, GetView().CmbPermission.SelectedValue);
                 }
             });

            query.SetTopX(1000);
            query.AddOrderBy(K1CTABH.Name, K1CTABH.Fields.TXTYP, OrderDirection.ASC);

            return query;
        }

        private void BuildContextMenu(DataGridRow dgRow, CodetableDataGridRow ctRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            T1CTABH t1ctabh = (T1CTABH)ctRow.RowObject!;

            // Nur im Debugmodus dürfen Development-CTs geändert werden
            if (t1ctabh.PERMI == "D" && FnSystem.IsDebug() || t1ctabh.PERMI == "U")
                contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            contextBuilder.AddItem("ctxtView", "View", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.View), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (FnSystem.IsDebug())
                contextBuilder.AddItem("ctxtProperties", "Properties", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Info), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            // Nur im Debugmodus dürfen Development-CTs gelöscht werden
            if (FnSystem.IsDebug())
                contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (contextBuilder.HasItems())
                dgRow.ContextMenu = contextBuilder.Build();
            else
                dgRow.ContextMenu = null;
        }

        protected void EV_DgCodetables_CtxtOpening(FrameworkElement target)
        {
            DataGridRow row = target as DataGridRow;

            if (row == null)
                return;

            if (row.DataContext is not CodetableDataGridRow)
                return;

            BuildContextMenu(row, (CodetableDataGridRow)row.DataContext);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected async Task EV_ctxtDelete()
        {
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();

            CFMBOXResult result = cfmbox.Show("Are you sure you want to delete this codetable?", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                T1CTABH selectedT1ctabh = (T1CTABH)_viewModel!.SelectedRow!.RowObject!;
                TFCTABH.DeleteCodetableAndEntries(selectedT1ctabh);

                await EV_BtnRefresh();
            }
        }

        protected void EV_ctxtProperties()
        {
            T1CTABH selectedT1ctabh = (T1CTABH)_viewModel!.SelectedRow!.RowObject!;
            CodetablesEditApp codetablesEditApp = GetApp().GetApplication<CodetablesEditApp>();
            codetablesEditApp.Properties(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await EV_BtnRefresh();
            }, selectedT1ctabh, (FnSystem.IsDebug() && selectedT1ctabh.PERMI == "D"));
        }

        protected void EV_ctxtEdit()
        {
            T1CTABH selectedT1ctabh = (T1CTABH)_viewModel!.SelectedRow!.RowObject!;
            CodetablesEntrysApp codetablesEntrysEditApp = GetApp().GetApplication<CodetablesEntrysApp>();
            codetablesEntrysEditApp.Edit(selectedT1ctabh);
        }

        protected void EV_ctxtView()
        {
            T1CTABH selectedT1ctabh = (T1CTABH)_viewModel!.SelectedRow!.RowObject!;
            CodetablesEntrysApp codetablesEntrysEditApp = GetApp().GetApplication<CodetablesEntrysApp>();
            codetablesEntrysEditApp.View(selectedT1ctabh);
        }

        protected async Task EV_BtnAddCodetable()
        {
            CodetablesEditApp codetablesEditApp = GetApp().GetApplication<CodetablesEditApp>();
            codetablesEditApp.CreateNew(async (result) =>
            {
                if (result.Canceled)
                    return;

                await EV_BtnRefresh();
            });
        }

        protected async Task EV_BtnRefresh()
        {
            await BuildCodetableViewModel();
        }
    }
}
