using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Viewmodels;
using GameTimeNext.Core.Application.Metadata.Views;
using GameTimeNext.Core.Application.Profiles;
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

namespace GameTimeNext.Core.Application.Metadata.Controller
{

    /************ TODO **************/
    // - T1METAH und T1METAP nicht in Metdaten übernehmnen (Systemtabellen werden immer noch händisch erzeugt / wird sowieso nicht so viel daran geändert)
    // - Danach alle bisherigen manuellen Tabellen in Metadaten übenehmen



    public class MetadataViewController : UIXViewControllerBase
    {
        private MetadataViewModel? _viewModel;

        public MetadataViewController(UIXApplication app) : base(app)
        {
            _viewModel = new MetadataViewModel();
        }

        protected override void Init()
        {
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
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

        private MetadataApp GetApp()
        {
            return (MetadataApp)App;
        }

        private MetadataView GetView()
        {
            return (MetadataView)View;
        }

        protected void EV_DgMetadata_CtxtOpening(FrameworkElement target)
        {
            DataGridRow row = target as DataGridRow;

            if (row == null)
                return;

            if (row.DataContext is not MetadataDataGridRow metadataRow)
                return;

            if (_viewModel != null)
                _viewModel.SelectedRow = metadataRow;

            GetView().DgMetadata.SelectedItem = metadataRow;

            BuildContextMenu(row, metadataRow);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        private void BuildContextMenu(DataGridRow row, MetadataDataGridRow mdRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            T1METAH t1metah = (T1METAH)mdRow.RowObject!;

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (contextBuilder.HasItems())
                row.ContextMenu = contextBuilder.Build();
            else
                row.ContextMenu = null;
        }

        protected void EV_ctxtEdit()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1METAH selectedT1metah)
                return;

            MetadataEditApp metadataEditApp = GetApp().GetApplication<MetadataEditApp>();
            metadataEditApp.Edit(selectedT1metah);
        }

        protected async Task EV_ctxtDelete()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1METAH selectedT1metah)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();

            CFMBOXResult result = cfmbox.Show(
                "Delete metadata and generated table?",
                CFMBOXResult.Yes | CFMBOXResult.No,
                CFMBOXIcon.Question);

            if (result != CFMBOXResult.Yes)
                return;

            TFMETAH.DeleteMetadataAndLinkedData(selectedT1metah, deleteTable: true);

            await EV_BtnRefresh();
        }

        private async Task BuildDG()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<MetadataDataGridRow> metadataRows = BuildMetadataRows();

                View.Dispatcher.Invoke(() =>
                {
                    _viewModel = new MetadataViewModel();
                    _viewModel.MetadataDataGridRows = new System.Collections.ObjectModel.ObservableCollection<MetadataDataGridRow>(metadataRows);

                    GetView().DataContext = _viewModel;
                    GetApp().Loader.Stop();
                });
            });
        }

        private List<MetadataDataGridRow> BuildMetadataRows()
        {

            List<MetadataDataGridRow> metadataRows = new List<MetadataDataGridRow>();

            UIXQuery query = BuildQueryMetadata();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string menam = UIXQuery.GetString(reader, K1METAH.Name, K1METAH.Fields.MENAM, string.Empty);

                    // Objekt aus Datenbank auslesen
                    TXMETAH txmetah = new TXMETAH();
                    T1METAH t1metah = txmetah.Read(menam);

                    // Neue Zeile erstellen
                    MetadataDataGridRow row = GetView().DgMetadata.CreateNewRow<MetadataDataGridRow>();
                    row.COMENAM = t1metah.MENAM;
                    row.CODESCR = t1metah.DESCR;
                    row.COMTYPE = TFCTABD.GetDescription("mT", t1metah.MTYPE);
                    row.RowObject = t1metah;

                    metadataRows.Add(row);

                }
            }

            return metadataRows;
        }

        private UIXQuery BuildQueryMetadata()
        {
            UIXQuery query = new UIXQuery(K1METAH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            // Felder hinzufügen
            query.AddField(K1METAH.Name, K1METAH.Fields.MENAM);

            View.Dispatcher.Invoke(() =>
            {
                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbTableObjectName.Text))
                    query.AddWhere(K1METAH.Name, K1METAH.Fields.MENAM, QueryCompareType.LIKE, GetView().TxbTableObjectName.Text);

                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbDescription.Text))
                    query.AddWhere(K1METAH.Name, K1METAH.Fields.DESCR, QueryCompareType.LIKE, GetView().TxbDescription.Text);

                if (!FnString.IsNullEmptyOrWhitespace(GetView().CmbType.SelectedValue.ToString()!))
                    query.AddWhere(K1METAH.Name, K1METAH.Fields.MTYPE, QueryCompareType.EQUALS, GetView().CmbType.SelectedValue.ToString()!);
            });

            query.SetTopX(1000);
            query.AddOrderBy(K1METAH.Name, K1METAH.Fields.MENAM, OrderDirection.ASC);

            return query;
        }

        protected void EV_BtnAdd()
        {
            MetadataEditApp metadataEditApp = GetApp().GetApplication<MetadataEditApp>();
            metadataEditApp.CreateNew();

        }



        protected async Task EV_BtnRefresh()
        {
            await BuildDG();
        }
    }
}
