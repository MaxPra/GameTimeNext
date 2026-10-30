using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Viewmodels;
using GameTimeNext.Core.Application.Metadata.Views;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataEditViewController : UIXViewControllerBase<MetadataEditApp, MetadataEditView>
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
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override async Task BuildFirstImplAsync()
        {
            await BuildDG();
        }

        protected override void BuildImpl()
        {
            FnControls.SetEnabled(GetView().TxbTableObject, GetView().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetView().TxbDescription, GetView().ViewIndicator.Count > 0);


            FnControls.SetVisible(GetView().tabControl, GetView().ViewIndicator.Contains("ED"));

            FnControls.SetVisible(GetView().lblGenerationRequired, !GetApp().T1METAH!.GENER);
        }


        protected override void CheckImpl()
        {
            string selectedType = GetView().CmbType.SelectedValue?.ToString() ?? string.Empty;
            string controlNameTableObj = "Table object";

            // Table object
            if (FnString.IsNullEmptyOrWhitespace(GetView().TxbTableObject.Text))
                AddViewError(GetView().TxbTableObject, GetErrorMessage("GLO0003", controlNameTableObj));
            else if (!Regex.IsMatch(GetView().TxbTableObject.Text, @"^T1[A-Z0-9]*$"))
                AddViewError(GetView().TxbTableObject, GetErrorMessage("GLO0007", controlNameTableObj));
            else if (GetView().TxbTableObject.Text.Length != 7)
                AddViewError(GetView().TxbTableObject, GetErrorMessage("GLO0005", controlNameTableObj, 7));

            TXMETAH txmetah = new TXMETAH();
            if (GetView().ViewIndicator.Contains("CN") && txmetah.Exists(GetView().TxbTableObject.Text))
            {
                AddViewError(GetView().TxbTableObject, GetErrorMessage("GLO0008", controlNameTableObj, GetView().TxbTableObject.Text));
            }

            // Description
            if (FnString.IsNullEmptyOrWhitespace(GetView().TxbDescription.Text))
                AddViewError(GetView().TxbDescription, GetErrorMessage("GLO0003", "Description"));
            else if (GetView().TxbDescription.Text.Length > 200)
                AddViewError(GetView().TxbDescription, GetErrorMessage("GLO0004", "Description", "200"));

            // Type
            if (FnString.IsNullEmptyOrWhitespace(selectedType))
                AddViewError(GetView().CmbType, GetErrorMessage("GLO0003", "Type"));
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
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

                    GetView().DataContext = _viewModel;
                    GetApp().Loader.Stop();
                });
            });
        }

        private List<MetadataPosDataGridRow> BuildMetadataPosRows()
        {
            List<MetadataPosDataGridRow> rows = new List<MetadataPosDataGridRow>();

            TXMETAP txmetap = new TXMETAP();

            UIXQuery query = BuildQueryMetadataPositions();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    string menam = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.MENAM, string.Empty);
                    string ponam = UIXQuery.GetString(reader, K1METAP.Name, K1METAP.Fields.PONAM, string.Empty);

                    T1METAP? t1metap = txmetap.Read(menam, ponam);
                    if (t1metap is null)
                        continue;

                    MetadataPosDataGridRow row = GetView().DgFields.CreateNewRow<MetadataPosDataGridRow>();
                    row.COPONAM = t1metap.PONAM;
                    row.COPORDE = t1metap.PORDE;
                    row.COMENAM = t1metap.MENAM;
                    row.CODATYP = UIXSqliteDataType.GetByKey(t1metap.DATYP).Name;
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
                if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbFieldSearch.Text))
                {
                    if (!FnString.IsNullEmptyOrWhitespace(GetView().TxbFieldSearch.Text))
                    {
                        string search = GetView().TxbFieldSearch.Text.Trim();
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

        protected void EV_DgFields_CtxtOpening(FrameworkElement target)
        {
            DataGridRow row = target as DataGridRow;

            if (row == null)
                return;

            if (row.DataContext is not MetadataPosDataGridRow metadataRow)
                return;

            if (_viewModel != null)
                _viewModel.SelectedRow = metadataRow;

            GetView().DgFields.SelectedItem = metadataRow;

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

            MetadataPosEditApp metadataEditApp = GetApp().GetApplication<MetadataPosEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
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

            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);

            CFMBOXResult result = cfmbox.Show(GetErrorMessage("GLO0001", "this metadata position"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

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

            if (GetView().ViewIndicator.Contains("ED"))
                Exit(true);

            GetView().ViewIndicator.Clear();
            GetView().ViewIndicator.Add("ED");

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
                    MigrationFactory.Metadata.CodeGenerator.GenerateFor(GetApp().T1METAH!);
                    MigrationFactory.FromCsv.MigrateTables(MigrationFactory.ImportType.MetadataGenerator);
                });

                GetApp().T1METAH!.GENER = true;
                new TXMETAH().Save(GetApp().T1METAH!);

                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("MET0002"), CFMBOXResult.Ok, CFMBOXIcon.Success);
            }
            catch (Exception ex)
            {
                GetApp().GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window).Show(GetErrorMessage("GLO0013", "generating the classes", ex.Message), CFMBOXResult.Ok, CFMBOXIcon.Error);
            }
            finally
            {
                GetApp().Loader.Stop();
            }

            await BuildDG();
        }

        protected async Task EV_BtnAdd()
        {
            MetadataPosEditApp metadataPosEditApp = GetApp().GetApplication<MetadataPosEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            metadataPosEditApp.CreateNew(GetApp().T1METAH!);

            await BuildDG();

            GetApp().T1METAH!.GENER = false;
            new TXMETAH().Save(GetApp().T1METAH!);

            Build();
        }

        protected async Task EV_btnAutoReorder()
        {
            try
            {
                GetApp().Loader.Begin();

                if (TFMETAP.AutoReorder(GetApp().T1METAH!.MENAM))
                {
                    GetApp().T1METAH!.GENER = false;
                    new TXMETAH().Save(GetApp().T1METAH!);
                }
            }
            finally
            {
                GetApp().Loader.Stop();
            }

            await BuildDG();

            Build();
        }

    }
}
