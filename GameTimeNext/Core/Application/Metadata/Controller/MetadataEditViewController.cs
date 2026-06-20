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
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataEditViewController : UIXWindowControllerBase
    {

        MetadataEditViewModel? _viewModel;

        public MetadataEditViewController(UIXApplication app) : base(app)
        {
        }

        public class MetadataEditViewReturn : UIXViewReturn
        {
        }

        protected override void Init()
        {
            AddIdentifier("T1METAH", GetApp().T1METAH!);
        }

        protected override void BuildFirstImpl()
        {
            InitializeMetadataTypeCombo();
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildDG();
        }

        protected override void BuildImpl()
        {
            FnControls.SetEnabled(GetWnd().TxbTableObject, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().TxbDescription, GetWnd().ViewIndicator.Count > 0);


            FnControls.SetVisible(GetWnd().tabControl, GetWnd().ViewIndicator.Contains("ED"));

            FnControls.SetVisible(GetWnd().lblGenerationRequired, !GetApp().T1METAH!.GENER);
        }


        protected override void Check()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbTableObject.Text))
                AddViewError(GetWnd().TxbTableObject, "Table Object cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbDescription.Text))
                AddViewError(GetWnd().TxbDescription, "Description cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().CmbType.SelectedValue.ToString()!))
                AddViewError(GetWnd().CmbType, "Type must be selected.");

            if (GetWnd().TxbTableObject.Text.Length > 7)
                AddViewError(GetWnd().TxbTableObject, "Table Object cannot exceed 7 characters.");

            if (GetWnd().TxbDescription.Text.Length > 200)
                AddViewError(GetWnd().TxbDescription, "Description cannot exceed 200 characters.");
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
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private async Task BuildDG()
        {
            GetApp().Loader.Begin();

            await Task.Run(() =>
            {
                List<MetadataPosDataGridRow> metadataRows = BuildMetadataPosRows();

                View.Dispatcher.Invoke(() =>
                {
                    _viewModel = new MetadataEditViewModel();
                    _viewModel.MetadataPositionDataGridRows = new System.Collections.ObjectModel.ObservableCollection<MetadataPosDataGridRow>(metadataRows);

                    GetWnd().DataContext = _viewModel;
                    GetApp().Loader.Stop();
                });
            });
        }

        private List<MetadataPosDataGridRow> BuildMetadataPosRows()
        {
            List<MetadataPosDataGridRow> rows = new List<MetadataPosDataGridRow>();

            TXMETAP txmetap = new TXMETAP();

            UIXQuery query = BuildQueryMetadataPositions();

            string s = query.PreviewQuery();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string menam = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.MENAM, string.Empty);
                    string ponam = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.PONAM, string.Empty);

                    T1METAP t1metap = txmetap.Read(menam, ponam);

                    MetadataPosDataGridRow row = GetWnd().DgFields.CreateNewRow<MetadataPosDataGridRow>();
                    row.COPONAM = t1metap.PONAM;
                    row.COMENAM = t1metap.MENAM;
                    row.CODATYP = UIXSQLiteDataTypes.NormalizeCSharpType(t1metap.DATYP);
                    row.CODESCR = t1metap.DESCR;
                    row.COPRIMK = t1metap.PRIMK;
                    row.CODALEN = t1metap.DALEN;
                    row.RowObject = t1metap;

                    rows.Add(row);
                }
            }

            return rows;
        }

        private UIXQuery BuildQueryMetadataPositions()
        {
            UIXQuery query = new UIXQuery(K1METAP.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1METAP.Name, K1METAP.Fields.MENAM);
            query.AddField(K1METAP.Name, K1METAP.Fields.PONAM);

            View.Dispatcher.Invoke(() =>
            {
                if (!FnString.IsNullEmptyOrWhitespace(GetWnd().TxbFieldSearch.Text))
                {
                    if (!FnString.IsNullEmptyOrWhitespace(GetWnd().TxbFieldSearch.Text))
                    {
                        string search = GetWnd().TxbFieldSearch.Text.Trim();
                        string like = $"%{search}%";

                        query.AddWhereRaw(
                            $"({K1METAP.Name}.{K1METAP.Fields.PONAM} LIKE ? OR {K1METAP.Name}.{K1METAP.Fields.DESCR} LIKE ?)",
                            like, like);
                    }
                }
            });

            query.AddWhere(K1METAP.Name, K1METAP.Fields.MENAM, QueryCompareType.EQUALS, GetApp().T1METAH!.MENAM);

            query.AddOrderBy(K1METAP.Name, K1METAP.Fields.PORDE, OrderDirection.ASC);

            return query;
        }

        private void InitializeMetadataTypeCombo()
        {
            GetWnd().CmbType.Items.Clear();

            foreach (MetadataObjectTypes.Entry entry in MetadataObjectTypes.GetEntries())
            {
                GetWnd().CmbType.Items.Add(new ComboBoxItem
                {
                    Content = entry.Text,
                    Tag = entry.Key
                });
            }

            if (GetWnd().CmbType.Items.Count > 0)
                GetWnd().CmbType.SelectedIndex = 0;
        }

        protected void EV_DgFields_CtxtOpening(FrameworkElement target)
        {
            DataGridRow row = target as DataGridRow;

            if (row == null)
                return;

            if (row.DataContext is not MetadataPosDataGridRow metadataRow)
                return;

            if (_viewModel != null)
                _viewModel.SelectedRow = metadataRow;

            GetWnd().DgFields.SelectedItem = metadataRow;

            BuildContextMenu(row, metadataRow);

            if (row.ContextMenu == null)
                return;

            row.ContextMenu.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        private void BuildContextMenu(DataGridRow row, MetadataPosDataGridRow metadataPosRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("ProfilesListBoxContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            T1METAP t1metah = (T1METAP)metadataPosRow.RowObject!;

            contextBuilder.AddItem("ctxtEdit", "Edit", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Edit), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            contextBuilder.AddItem("ctxtDelete", "Delete", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Delete), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            if (contextBuilder.HasItems())
                row.ContextMenu = contextBuilder.Build();
            else
                row.ContextMenu = null;
        }

        protected void EV_ctxtEdit()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1METAP selectedT1empo)
                return;

            MetadataPosEditApp metadataEditApp = GetApp().GetApplication<MetadataPosEditApp>();
            metadataEditApp.Edit(async (result) =>
            {
                if (result.HasChanged)
                {
                    await BuildDG();

                    GetApp().T1METAH!.GENER = false;
                    new TXMETAH().Save(GetApp().T1METAH!);

                    Build();
                }

            }, selectedT1empo);
        }

        protected void EV_ctxtDelete()
        {
            if (_viewModel?.SelectedRow?.RowObject is not T1METAP selectedT1empo)
                return;

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>();

            CFMBOXResult result = cfmbox.Show("Are you sure you want to delete this metadata position?", CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

            if (result == CFMBOXResult.Yes)
            {
                new TXMETAP().Delete(selectedT1empo.MENAM, selectedT1empo.PONAM);

                GetApp().T1METAH!.GENER = false;
                new TXMETAH().Save(GetApp().T1METAH!);
            }

        }

        protected void EV_BtnSave()
        {
            if (HasViewErrors())
                return;

            if (GetWnd().ViewIndicator.Contains("ED"))
                Exit(true);

            GetWnd().ViewIndicator.Clear();
            GetWnd().ViewIndicator.Add("ED");

            Open(false);
        }

        protected async Task EV_BtnRefresh()
        {
            await BuildDG();
        }

        protected async Task EV_BtnGenerate()
        {
            if (GetApp().T1METAH == null)
                return;

            try
            {
                GetApp().Loader.Begin();

                await Task.Run(() =>
                {
                    CFMetadataClassGenerator classGenerator = new CFMetadataClassGenerator();
                    classGenerator.GenerateFor(GetApp().T1METAH!, AppEnvironment.GetAppConfig().DevGeneratedFilesPath);

                    CFMetadataTableGenerator tableGenerator = new CFMetadataTableGenerator();
                    tableGenerator.EnsureTableFor(GetApp().T1METAH!);
                });

                GetApp().T1METAH!.GENER = true;
                new TXMETAH().Save(GetApp().T1METAH!);

                GetApp().GetApplication<CFMBOX>().Show("Generation completed.", CFMBOXResult.Ok, CFMBOXIcon.Success);
            }
            catch (Exception ex)
            {
                GetApp().GetApplication<CFMBOX>().Show("Generation failed", ex.Message, CFMBOXResult.Ok, CFMBOXIcon.Error);
            }
            finally
            {
                GetApp().Loader.Stop();
            }

            await BuildDG();
        }

        protected async Task EV_BtnAdd()
        {
            MetadataPosEditApp metadataPosEditApp = GetApp().GetApplication<MetadataPosEditApp>();
            metadataPosEditApp.CreateNew(GetApp().T1METAH!);

            await BuildDG();

            GetApp().T1METAH!.GENER = false;
            new TXMETAH().Save(GetApp().T1METAH!);

            Build();
        }

        private MetadataEditApp GetApp()
        {
            return (MetadataEditApp)App;
        }

        private MetadataEditView GetWnd()
        {
            return (MetadataEditView)View;
        }
    }
}
