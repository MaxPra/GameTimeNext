using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataPosEditViewController : UIXViewControllerBase<MetadataPosEditApp, MetadataPosEditView>
    {
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
            FnControls.SetEnabled(GetView().txbOrder, GetView().ViewIndicator.Contains("CN") || GetView().ViewIndicator.Contains("ED"));

            FnControls.SetEnabled(GetView().txbField, GetView().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetView().chbPrimaryKey, GetView().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetView().chbAutoIncrement, GetView().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetView().txbDescription, GetView().ViewIndicator.Contains("CN") || GetView().ViewIndicator.Contains("ED"));

            FnControls.SetEnabled(GetView().txbLength, GetView().ViewIndicator.Contains("CN"));
        }

        protected override async Task BuildFirstImplAsync()
        {
            // Fill data type ComboBox
            IReadOnlyList<UIXSqliteDataType> dataTypes = UIXSqliteDataType.GetAll();

            UIXManualCodetable codetable = new UIXManualCodetable();

            foreach (var dataType in dataTypes)
            {
                codetable.AddEntry(dataType.Key, dataType.Name);
            }

            codetable.ApplyTo(GetView().cmbDataType);

            T1METAP? metadataPosition = GetApp().T1METAP;
            if (metadataPosition != null)
            {
                UIXSqliteDataType? matchingDefinition = dataTypes.FirstOrDefault(x =>
                    string.Equals(x.Key, metadataPosition.DATYP, StringComparison.OrdinalIgnoreCase));

                if (matchingDefinition == null)
                {
                    matchingDefinition = dataTypes.FirstOrDefault(x =>
                        string.Equals(x.Name, metadataPosition.DATYP, StringComparison.OrdinalIgnoreCase));
                }

                if (matchingDefinition != null && !string.Equals(metadataPosition.DATYP, matchingDefinition.Key, StringComparison.OrdinalIgnoreCase))
                {
                    metadataPosition.DATYP = matchingDefinition.Key;
                }
            }
        }

        protected override void BuildImpl()
        {
            ComboBox cmbDataType = GetView().cmbDataType;
            string selectedDataType = cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            FnControls.SetVisible(GetView().chbAutoIncrement, GetView().chbPrimaryKey.IsChecked.Equals(true));

            if (GetView().chbAutoIncrement.IsChecked.Equals(true))
            {
                FnControls.SetEnabled(cmbDataType, false);
                using (SuppressRunEventPipeline())
                {

                    ComboBoxItem item = cmbDataType.Items.OfType<ComboBoxItem>().First(i => i.Tag.Equals("03"));
                    cmbDataType.SelectedItem = item;
                }

                FnControls.SetVisible(GetView().txbLength, false);
                FnControls.SetVisible(GetView().lblLength, false);

                FnControls.SetVisible(GetView().grpDefault, false);
                FnControls.SetVisible(GetView().chbDefault, false);
                FnControls.SetVisible(GetView().chbDefaultBool, false);
                FnControls.SetVisible(GetView().txbDefault, false);
            }
            else
            {
                FnControls.SetEnabled(cmbDataType, GetView().ViewIndicator.Contains("CN"));

                FnControls.SetVisible(GetView().txbLength, "01".Equals(selectedDataType));
                FnControls.SetVisible(GetView().lblLength, "01".Equals(selectedDataType));

                BuildVisibilityDefault();
            }
        }

        private void BuildVisibilityDefault()
        {
            string selectedDataType = GetView().cmbDataType.SelectedValue?.ToString() ?? string.Empty;
            UIXSqliteDataType dataType = UIXSqliteDataType.GetByKey(selectedDataType);

            bool isActive = GetView().chbDefault.IsChecked ?? false;

            FnControls.SetVisible(GetView().grpDefault, dataType.IsDefaultAllowed);
            FnControls.SetVisible(GetView().chbDefault, dataType.IsDefaultAllowed);
            FnControls.SetVisible(GetView().chbDefaultBool, dataType.IsDefaultAllowed && isActive && "06".Equals(selectedDataType));
            FnControls.SetVisible(GetView().txbDefault, dataType.IsDefaultAllowed && isActive && !"06".Equals(selectedDataType));

            if (!dataType.IsDefaultAllowed)
            {
                using (SuppressRunEventPipeline())
                {
                    GetView().chbDefault.IsChecked = false;
                    GetView().chbDefaultBool.IsChecked = false;
                    GetView().txbDefault.Text = string.Empty;
                }
            }
        }

        protected override void CheckImpl()
        {
            string selectedDataType = GetView().cmbDataType.SelectedValue?.ToString() ?? string.Empty;
            UIXSqliteDataType dataType = UIXSqliteDataType.GetByKey(selectedDataType);
            string controlNameField = "Field";
            T1METAH t1metah = new TXMETAH().Read(GetApp().T1METAP!.MENAM)!;

            // Order
            if (!FnControls.ContainsOnlyNumericValue(GetView().txbOrder))
                AddViewError(GetView().txbOrder, GetErrorMessage("GLO0009"));

            // Field
            if (FnString.IsNullEmptyOrWhitespace(GetView().txbField.Text))
                AddViewError(GetView().txbField, GetErrorMessage("GLO0003", controlNameField));
            else if (!Regex.IsMatch(GetView().txbField.Text, @"^[A-Z][A-Z0-9]*$"))
                AddViewError(GetView().txbField, GetErrorMessage("GLO0007", controlNameField));
            else if (GetView().txbField.Text.Length < 4 || GetView().txbField.Text.Length > 5)
                AddViewError(GetView().txbField, GetErrorMessage("GLO0006", controlNameField, 4, 5));

            TXMETAP txmetap = new TXMETAP();
            if (GetView().ViewIndicator.Contains("CN") && txmetap.Exists(t1metah.MENAM, GetView().txbField.Text))
            {
                AddViewError(GetView().txbField, GetErrorMessage("GLO0008", controlNameField, GetView().txbField.Text));
            }

            // Description
            if (FnString.IsNullEmptyOrWhitespace(GetView().txbDescription.Text))
                AddViewError(GetView().txbDescription, GetErrorMessage("GLO0003", "Description"));
            else if (GetView().txbDescription.Text.Length > 200)
                AddViewError(GetView().txbDescription, GetErrorMessage("GLO0004", "Description", "200"));

            // Datatype
            if (FnString.IsNullEmptyOrWhitespace(selectedDataType))
                AddViewError(GetView().cmbDataType, GetErrorMessage("GLO0003", "Data type"));

            // Length
            if (FnString.IsNullEmptyOrWhitespace(GetView().txbLength.Text) && "01".Equals(selectedDataType))
                AddViewError(GetView().txbLength, GetErrorMessage("GLO0003", "Length"));

            // Default
            if (dataType.IsDefaultAllowed && dataType.IsNumeric && GetView().chbDefault.IsChecked.Equals(true))
                if (!FnControls.ContainsOnlyNumericValue(GetView().txbDefault, dataType))
                    AddViewError(GetView().txbDefault, GetErrorMessage("GLO0009"));
        }

        protected override void FillViewImpl()
        {
            if (GetView().ViewIndicator.Contains("CN"))
                GetView().txbOrder.Text = TFMETAP.GetNextOrder(GetApp().T1METAP!).ToString(CultureInfo.CurrentCulture);

            string selectedDataType = GetView().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            // DEFVL
            if (selectedDataType.Equals("06"))
            {
                // Checkbox
                bool defaultBool = false;
                try
                {
                    defaultBool = UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSV, UIXSqliteDataType.ConversionType.CSharp, GetApp().T1METAP!.DEFVL, UIXSqliteDataType.GetByKey(selectedDataType)) is true;
                }
                catch (FormatException)
                {
                    // DEFVL still holds a non-boolean value of a previous datatype
                }
                GetView().chbDefaultBool.IsChecked = defaultBool;
            }
            else if (!FnString.IsNullEmptyOrWhitespace(selectedDataType))
            {
                // Textbox (DEFVL is stored in CSV format, the view shows it in the current culture)
                GetView().txbDefault.Text = (string)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSV, UIXSqliteDataType.ConversionType.UI, GetApp().T1METAP!.DEFVL, UIXSqliteDataType.GetByKey(selectedDataType))!;
            }
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
            CFMBOX.AskUnsavedChanges(GetApp(), args);
        }

        protected override void FillDBOImpl()
        {
            string selectedDataType = GetView().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            // DEFVL
            if (!GetView().chbDefault.IsChecked.Equals(true))
            {
                // No default
                GetApp().T1METAP!.DEFVL = string.Empty;
            }
            else if (selectedDataType.Equals("06"))
            {
                // Checkbox
                GetApp().T1METAP!.DEFVL = (string)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.CSV, GetView().chbDefaultBool.IsChecked.Equals(true))!;
            }
            else
            {
                // Textbox (validated in CheckImpl)
                GetApp().T1METAP!.DEFVL = (string)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.UI, UIXSqliteDataType.ConversionType.CSV, GetView().txbDefault.Text, UIXSqliteDataType.GetByKey(selectedDataType))!;
            }
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

        protected void EV_chbDefault()
        {
            if (GetView().chbDefault.IsChecked.Equals(true))
                return;

            GetView().txbDefault.Text = string.Empty;
            GetView().chbDefaultBool.IsChecked = false;
        }

        protected void EV_btnSave()
        {

            GetViewReturn<MetadataPosEditViewReturn>().HasChanged = GetApp().T1METAP!.HasChanged();

            Exit(true);
        }

    }
}
