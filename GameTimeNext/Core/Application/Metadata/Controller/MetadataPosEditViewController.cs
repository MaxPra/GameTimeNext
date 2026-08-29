using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Application.Metadata.Views;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
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
            FnControls.SetEnabled(GetWnd().txbField, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().chbPrimaryKey, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().chbAutoIncrement, GetWnd().ViewIndicator.Contains("CN"));
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

            FnControls.SetVisible(GetWnd().txbLength, "01".Equals(selectedDataType));
            FnControls.SetVisible(GetWnd().lblLength, "01".Equals(selectedDataType));
            FnControls.SetVisible(GetWnd().chbAutoIncrement, GetWnd().chbPrimaryKey.IsChecked == true);

            BuildVisibilityDefault();
        }

        private void BuildVisibilityDefault()
        {
            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            string[] allowedDatatypes = ["01", "06"];
            bool showForDatatype = allowedDatatypes.Contains(selectedDataType);
            bool isActive = GetWnd().chbDefault.IsChecked ?? false;

            FnControls.SetVisible(GetWnd().lblDefault, showForDatatype);
            FnControls.SetVisible(GetWnd().chbDefault, showForDatatype);
            FnControls.SetVisible(GetWnd().chbDefaultBool, showForDatatype && isActive && "06".Equals(selectedDataType));
            FnControls.SetVisible(GetWnd().txbDefault, showForDatatype && isActive && !"06".Equals(selectedDataType));

            if (!showForDatatype)
            {
                GetWnd().chbDefault.IsChecked = false;
                GetWnd().chbDefaultBool.IsChecked = false;
                GetWnd().txbDefault.Text = string.Empty;
            }
        }

        protected override void Check()
        {
            string selectedDataType = GetWnd().cmbDataType.SelectedValue?.ToString() ?? string.Empty;

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbField.Text))
                AddViewError(GetWnd().txbField, "Field name cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbDescription.Text))
                AddViewError(GetWnd().txbDescription, "Description cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbLength.Text) && "01".Equals(selectedDataType))
                AddViewError(GetWnd().txbLength, "Length cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(selectedDataType))
                AddViewError(GetWnd().cmbDataType, "Data type cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().txbOrder.Text))
                AddViewError(GetWnd().txbOrder, "Order cannot be empty.");
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
