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
            FnControls.SetEnabled(GetWnd().TxbField, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().ChbPrimaryKey, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().ChbAutoIncrement, GetWnd().ViewIndicator.Contains("CN"));
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

            codetable.ApplyTo(GetWnd().CmbDataType);

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
            string selectedDataType = GetWnd().CmbDataType.SelectedValue?.ToString() ?? string.Empty;

            FnControls.SetVisible(GetWnd().TxbLength, "01".Equals(selectedDataType));
            FnControls.SetVisible(GetWnd().LblLength, "01".Equals(selectedDataType));
            FnControls.SetVisible(GetWnd().ChbAutoIncrement, GetWnd().ChbPrimaryKey.IsChecked == true);
        }

        protected override void Check()
        {
            string selectedDataType = GetWnd().CmbDataType.SelectedValue?.ToString() ?? string.Empty;

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbField.Text))
                AddViewError(GetWnd().TxbField, "Field name cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbDescription.Text))
                AddViewError(GetWnd().TxbDescription, "Description cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbLength.Text) && "01".Equals(selectedDataType))
                AddViewError(GetWnd().TxbLength, "Length cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(selectedDataType))
                AddViewError(GetWnd().CmbDataType, "Data type cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbOrder.Text))
                AddViewError(GetWnd().TxbOrder, "Order cannot be empty.");
        }

        protected override void FillViewImpl()
        {
            if (GetWnd().ViewIndicator.Contains("CN"))
                GetWnd().TxbOrder.Text = TFMETAP.GetNextOrder(GetApp().T1METAP!).ToString();
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
