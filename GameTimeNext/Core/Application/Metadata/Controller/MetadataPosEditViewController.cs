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

namespace GameTimeNext.Core.Application.Metadata.Controller
{
    public class MetadataPosEditViewController : UIXViewControllerBase
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
            FnControls.SetEnabled(GetWnd().txbOrder, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetWnd().txbField, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().chbPrimaryKey, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().chbAutoIncrement, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetEnabled(GetWnd().txbDescription, GetWnd().ViewIndicator.Contains("CN") || GetWnd().ViewIndicator.Contains("ED"));

            FnControls.SetEnabled(GetWnd().txbLength, GetWnd().ViewIndicator.Contains("CN"));
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

            codetable.ApplyTo(GetWnd().cmbDataType);

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
            ComboBox cmbDataType = GetWnd().cmbDataType;
            string selectedDataType = cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            FnControls.SetVisible(GetWnd().chbAutoIncrement, GetWnd().chbPrimaryKey.IsChecked.Equals(true));

            if (GetWnd().chbAutoIncrement.IsChecked.Equals(true))
            {
                FnControls.SetEnabled(cmbDataType, false);
                using (SuppressRunEventPipeline())
                {

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
                FnControls.SetEnabled(cmbDataType, GetWnd().ViewIndicator.Contains("CN"));

                FnControls.SetVisible(GetWnd().txbLength, "01".Equals(selectedDataType));
                FnControls.SetVisible(GetWnd().lblLength, "01".Equals(selectedDataType));

                BuildVisibilityDefault();
            }
        }

        private void BuildVisibilityDefault()
        {
            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;
            UIXSqliteDataType dataType = UIXSqliteDataType.GetByKey(selectedDataType);

            bool isActive = GetWnd().chbDefault.IsChecked ?? false;

            FnControls.SetVisible(GetWnd().lblDefault, dataType.IsDefaultAllowed);
            FnControls.SetVisible(GetWnd().chbDefault, dataType.IsDefaultAllowed);
            FnControls.SetVisible(GetWnd().chbDefaultBool, dataType.IsDefaultAllowed && isActive && "06".Equals(selectedDataType));
            FnControls.SetVisible(GetWnd().txbDefault, dataType.IsDefaultAllowed && isActive && !"06".Equals(selectedDataType));

            if (!dataType.IsDefaultAllowed)
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
            UIXSqliteDataType dataType = UIXSqliteDataType.GetByKey(selectedDataType);
            string controlNameField = "Field";
            T1METAH t1metah = new TXMETAH().Read(GetApp().T1METAP!.MENAM)!;

            // Order
            if (!FnControls.ContainsOnlyNumericValue(GetWnd().txbOrder))
                AddViewError(GetWnd().txbOrder, GetErrorMessage("GLO0009"));

            // Field
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbField.Text))
                AddViewError(GetWnd().txbField, GetErrorMessage("GLO0003", controlNameField));
            else if (!Regex.IsMatch(GetWnd().txbField.Text, @"^[A-Z][A-Z0-9]*$"))
                AddViewError(GetWnd().txbField, GetErrorMessage("GLO0007", controlNameField));
            else if (GetWnd().txbField.Text.Length < 4 || GetWnd().txbField.Text.Length > 5)
                AddViewError(GetWnd().txbField, GetErrorMessage("GLO0006", controlNameField, 4, 5));

            TXMETAP txmetap = new TXMETAP();
            if (GetWnd().ViewIndicator.Contains("CN") && txmetap.Exists(t1metah.MENAM, GetWnd().txbField.Text))
            {
                AddViewError(GetWnd().txbField, GetErrorMessage("GLO0008", controlNameField, GetWnd().txbField.Text));
            }

            // Description
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbDescription.Text))
                AddViewError(GetWnd().txbDescription, GetErrorMessage("GLO0003", "Description"));
            else if (GetWnd().txbDescription.Text.Length > 200)
                AddViewError(GetWnd().txbDescription, GetErrorMessage("GLO0004", "Description", "200"));

            // Datatype
            if (FnString.IsNullEmptyOrWhitespace(selectedDataType))
                AddViewError(GetWnd().cmbDataType, GetErrorMessage("GLO0003", "Data type"));

            // Length
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbLength.Text) && "01".Equals(selectedDataType))
                AddViewError(GetWnd().txbLength, GetErrorMessage("GLO0003", "Length"));

            // Default
            if (dataType.IsDefaultAllowed && dataType.IsNumeric && GetWnd().chbDefault.IsChecked.Equals(true))
                if (!FnControls.ContainsOnlyNumericValue(GetWnd().txbDefault, dataType))
                    AddViewError(GetWnd().txbDefault, GetErrorMessage("GLO0009"));
        }

        protected override void FillViewImpl()
        {
            if (GetWnd().ViewIndicator.Contains("CN"))
                GetWnd().txbOrder.Text = TFMETAP.GetNextOrder(GetApp().T1METAP!).ToString(CultureInfo.CurrentCulture);

            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

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
                GetWnd().chbDefaultBool.IsChecked = defaultBool;
            }
            else if (!FnString.IsNullEmptyOrWhitespace(selectedDataType))
            {
                // Textbox (DEFVL is stored in CSV format, the view shows it in the current culture)
                GetWnd().txbDefault.Text = (string)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSV, UIXSqliteDataType.ConversionType.UI, GetApp().T1METAP!.DEFVL, UIXSqliteDataType.GetByKey(selectedDataType))!;
            }
        }

        protected override void FillDBOImpl()
        {
            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            // DEFVL
            if (!GetWnd().chbDefault.IsChecked.Equals(true))
            {
                // No default
                GetApp().T1METAP!.DEFVL = string.Empty;
            }
            else if (selectedDataType.Equals("06"))
            {
                // Checkbox
                GetApp().T1METAP!.DEFVL = (string)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.CSV, GetWnd().chbDefaultBool.IsChecked.Equals(true))!;
            }
            else
            {
                // Textbox (validated in CheckImpl)
                GetApp().T1METAP!.DEFVL = (string)UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.UI, UIXSqliteDataType.ConversionType.CSV, GetWnd().txbDefault.Text, UIXSqliteDataType.GetByKey(selectedDataType))!;
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
            if (GetWnd().chbDefault.IsChecked.Equals(true))
                return;

            GetWnd().txbDefault.Text = string.Empty;
            GetWnd().chbDefaultBool.IsChecked = false;
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
