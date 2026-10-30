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
            /// <summary>
            /// True if anything was saved while the view was open (header, fields, order, generation state).
            /// </summary>
            public bool HasChanged { get; set; } = false;
        }

        /// <summary>
        /// Result of the closed view, see MetadataEditApp.
        /// </summary>
        public MetadataEditViewReturn GetEditReturn()
        {
            return GetViewReturn<MetadataEditViewReturn>();
        }

        protected override void Init()
        {
            // Created once: Init runs again on every Open (e.g. after saving a new table), the result must survive it
            if (ViewReturn is not MetadataEditViewReturn)
                ViewReturn = new MetadataEditViewReturn();

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
            SaveGenerated(false);

            Build();
        }

        /// <summary>
        /// Saves the generation state of the header directly; it is no unsaved change of the view.
        /// </summary>
        private void SaveGenerated(bool generated)
        {
            GetApp().T1METAH!.GENER = generated;
            new TXMETAH().Save(GetApp().T1METAH!);

            // Fields, order and generation change the overview too (generation state column)
            GetEditReturn().HasChanged = true;

            AcceptTableObjectsAsSaved();
        }

        protected void EV_BtnSave()
        {
            if (HasViewErrors())
                return;

            GetEditReturn().HasChanged = true;

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
                    MigrationFactory.Metadata.CodeGenerator.GenerateFor(GetApp(), GetApp().T1METAH!);
                    MigrationFactory.FromCsv.MigrateTables(MigrationFactory.ImportType.MetadataGenerator);
                });

                SaveGenerated(true);

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

        protected void EV_BtnAdd()
        {
            MetadataPosEditApp metadataPosEditApp = GetApp().GetApplication<MetadataPosEditApp>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
            metadataPosEditApp.CreateNew(async (result) =>
            {
                if (!result.HasChanged)
                    return;

                await GetView().DgFields.LoadDataAsync();

                MarkGenerationRequired();
            }, GetApp().T1METAH!);
        }

        protected async Task EV_btnAutoReorder()
        {
            try
            {
                GetApp().Loader.Begin();

                if (TFMETAP.AutoReorder(GetApp().T1METAH!.MENAM))
                    SaveGenerated(false);
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
