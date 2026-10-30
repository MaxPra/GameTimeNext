using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using GameTimeNext.Core.Framework.DataBase.Migration;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataEditViewController : UIXViewControllerBase<MetadataEditApp, MetadataEditView>
    {

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
            await GetView().DgFields.LoadDataAsync();
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

        /// <summary>
        /// The positions changed: the classes have to be generated again.
        /// </summary>
        public void MarkGenerationRequired()
        {
            GetApp().T1METAH!.GENER = false;
            new TXMETAH().Save(GetApp().T1METAH!);

            Build();
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
            await GetView().DgFields.LoadDataAsync();
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

            await GetView().DgFields.LoadDataAsync();
        }

        protected async Task EV_BtnAdd()
        {
            MetadataPosEditApp metadataPosEditApp = GetApp().GetApplication<MetadataPosEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            metadataPosEditApp.CreateNew(GetApp().T1METAH!);

            await GetView().DgFields.LoadDataAsync();

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

            await GetView().DgFields.LoadDataAsync();

            Build();
        }

    }
}
