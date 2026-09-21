using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using GameTimeNext.Core.Framework.Utils;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataPosEditViewController : UIXViewControllerBase
    {
        private static readonly string[] DATATYPES_WITH_DEFAULT = ["01", "02", "03", "04", "06"]; // OFDOI: IsDefaultAllowed auf SqliteDataType statt hier
        private static readonly string[] DATATYPES_NUMERIC = ["02", "03", "04"]; // OFDOI: IsNumeric auf SqliteDataType statt hier

        public MetadataPosEditViewController(UIXApplication app) : base(app)
        {
        }

        public class MetadataPosEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {

            ViewReturn = new MetadataPosEditViewReturn();

            AddIdentifier("T1METAP", GetApp().T1METAP!);
        }

        protected override void BuildFirstImpl()
        {
            FnControls.SetEnabled(GetWnd().txbOrder, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetWnd().txbField, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().chbPrimaryKey, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().chbAutoIncrement, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetWnd().txbDescription, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetWnd().cmbDataType, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetWnd().txbLength, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetWnd().chbDefault, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().chbDefaultBool, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().txbDefault, GetWnd().ViewIndicator.Contains("CN"));
        }

        protected override async Task BuildFirstImplAsync()
        {
            // Datentypen Combobox füllen
            IReadOnlyList<UIXSQLiteDataTypes.DataTypeDefinition> dataTypes = UIXSQLiteDataTypes.GetDefinitions();

            UIXManualCodetable codetable = new UIXManualCodetable();

            foreach (var dataType in dataTypes)
            {
                codetable.AddEntry(dataType.Key, dataType.Text);
            }

            codetable.ApplyTo(GetWnd().cmbDataType);

            T1METAP? metadataPosition = GetApp().T1METAP;
            if (metadataPosition != null)
            {
                UIXSQLiteDataTypes.DataTypeDefinition? matchingDefinition = dataTypes.FirstOrDefault(x =>
                    string.Equals(x.Key, metadataPosition.DATYP, StringComparison.OrdinalIgnoreCase));

                if (matchingDefinition == null)
                {
                    matchingDefinition = dataTypes.FirstOrDefault(x =>
                        string.Equals(x.Text, metadataPosition.DATYP, StringComparison.OrdinalIgnoreCase));
                }

                if (matchingDefinition != null && !string.Equals(metadataPosition.DATYP, matchingDefinition.Key, StringComparison.OrdinalIgnoreCase))
                {
                    metadataPosition.DATYP = matchingDefinition.Key;
                }
            }
        }

        protected override void BuildImpl()
        {
            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            FnControls.SetVisible(GetWnd().chbAutoIncrement, GetWnd().chbPrimaryKey.IsChecked.Equals(true));

            if (GetWnd().chbAutoIncrement.IsChecked.Equals(true))
            {
                FnControls.SetVisible(GetWnd().lblDataType, false);
                FnControls.SetVisible(GetWnd().cmbDataType, false);
                using (SuppressRunEventPipeline())
                {
                    ComboBox cmbDataType = GetWnd().cmbDataType;

                    ComboBoxItem item = cmbDataType.Items.OfType<ComboBoxItem>().First(i => i.Tag.Equals("03"));
                    cmbDataType.SelectedItem = item;
                }

                FnControls.SetVisible(GetWnd().txbLength, false);
                FnControls.SetVisible(GetWnd().lblLength, false);

                FnControls.SetVisible(GetWnd().lblDefault, false);
                FnControls.SetVisible(GetWnd().chbDefault, false);
                FnControls.SetVisible(GetWnd().chbDefaultBool, false);
                FnControls.SetVisible(GetWnd().txbDefault, false);
            }
            else
            {
                FnControls.SetVisible(GetWnd().lblDataType, true);
                FnControls.SetVisible(GetWnd().cmbDataType, true);

                FnControls.SetVisible(GetWnd().txbLength, "01".Equals(selectedDataType));
                FnControls.SetVisible(GetWnd().lblLength, "01".Equals(selectedDataType));

                BuildVisibilityDefault();
            }
        }

        private void BuildVisibilityDefault()
        {
            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            // OFDOI: BuildVisibilityDefault
            //MigrationFactory.SqliteDataType dataType = MigrationFactory.SqliteDataType.GetByKey(selectedDataType);
            //dataType.IsNumeric

            bool datatypeWithDefault = DATATYPES_WITH_DEFAULT.Contains(selectedDataType);
            bool isActive = GetWnd().chbDefault.IsChecked ?? false;

            FnControls.SetVisible(GetWnd().lblDefault, datatypeWithDefault);
            FnControls.SetVisible(GetWnd().chbDefault, datatypeWithDefault);
            FnControls.SetVisible(GetWnd().chbDefaultBool, datatypeWithDefault && isActive && "06".Equals(selectedDataType));
            FnControls.SetVisible(GetWnd().txbDefault, datatypeWithDefault && isActive && !"06".Equals(selectedDataType));

            if (!datatypeWithDefault)
            {
                using (SuppressRunEventPipeline())
                {
                    GetWnd().chbDefault.IsChecked = false;
                    GetWnd().chbDefaultBool.IsChecked = false;
                    GetWnd().txbDefault.Text = string.Empty;
                }
            }
        }

        protected override void CheckImpl()
        {
            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;
            bool datatypeWithDefault = DATATYPES_WITH_DEFAULT.Contains(selectedDataType);
            bool onlyNumeric = DATATYPES_NUMERIC.Contains(selectedDataType);

            // Order
            if (!FnControls.ContainsOnlyNumericValue(GetWnd().txbOrder))
                AddViewError(GetWnd().txbOrder, FnErrorMessage.ErrorMessage.OnlyNumeric.GetMessage());

            // Field
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbField.Text))
                AddViewError(GetWnd().txbField, FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Field name"));
            else if (!Regex.IsMatch(GetWnd().txbField.Text, @"^[A-Z]+$"))
                AddViewError(GetWnd().txbField, "Field can only be A-Z characters.");
            else if (GetWnd().txbField.Text.Length < 4)
                AddViewError(GetWnd().txbField, FnErrorMessage.ErrorMessage.MustExceedChars.GetMessage("Field", "4"));
            else if (GetWnd().txbField.Text.Length > 5)
                AddViewError(GetWnd().txbField, FnErrorMessage.ErrorMessage.CannotExceedChars.GetMessage("Field", "5"));

            // Description
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbDescription.Text))
                AddViewError(GetWnd().txbDescription, FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Description"));

            // Datatype
            if (FnString.IsNullEmptyOrWhitespace(selectedDataType))
                AddViewError(GetWnd().cmbDataType, FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Data type"));

            // Length
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbLength.Text) && "01".Equals(selectedDataType))
                AddViewError(GetWnd().txbLength, FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Length"));

            // Default
            if (datatypeWithDefault && onlyNumeric && GetWnd().chbDefault.IsChecked.Equals(true))
                if (!FnControls.ContainsOnlyNumericValue(GetWnd().txbDefault))
                    AddViewError(GetWnd().txbDefault, FnErrorMessage.ErrorMessage.OnlyNumeric.GetMessage());
        }

        protected override void FillViewImpl()
        {
            if (GetWnd().ViewIndicator.Contains("CN"))
                GetWnd().txbOrder.Text = TFMETAP.GetNextOrder(GetApp().T1METAP!).ToString();
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

        protected void EV_BtnSave()
        {

            GetViewReturn<MetadataPosEditViewReturn>().HasChanged = GetApp().T1METAP!.HasChanged();

            Exit(true);
        }

        private MetadataPosEditApp GetApp()
        {
            return (MetadataPosEditApp)App;
        }

        private MetadataPosEditView GetWnd()
        {
            return (MetadataPosEditView)View;
        }
    }
}
