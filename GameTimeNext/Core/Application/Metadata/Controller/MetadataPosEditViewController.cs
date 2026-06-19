using GameTimeNext.Core.Application.DataManagers;
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
    public class MetadataPosEditViewController : UIXWindowControllerBase
    {
        public MetadataPosEditViewController(UIXApplication app) : base(app)
        {
        }

        public class MetadataPosEditViewReturn : UIXViewReturn
        {
        }

        protected override void Init()
        {
            AddSource("T1CTABD", new TXCTABD());
            AddIdentifier("T1METAP", GetApp().T1METAP!);
        }

        protected override void BuildFirstImpl()
        {
            FnControls.SetEnabled(GetWnd().TxbField, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().ChbPrimaryKey, GetWnd().ViewIndicator.Contains("CN"));
        }

        protected override async Task BuildFirstImplAsync()
        {
        }

        protected override void BuildImpl()
        {
            FnControls.SetVisible(GetWnd().TxbLength, "01".Equals(GetWnd().CmbDataType.SelectedValue.ToString()!));
            FnControls.SetVisible(GetWnd().LblLength, "01".Equals(GetWnd().CmbDataType.SelectedValue.ToString()!));
        }

        protected override void Check()
        {
            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbField.Text))
                AddViewError(GetWnd().TxbField, "Field name cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbDescription.Text))
                AddViewError(GetWnd().TxbDescription, "Description cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbLength.Text) && "01".Equals(GetWnd().CmbDataType.SelectedValue.ToString()!))
                AddViewError(GetWnd().TxbLength, "Length cannot be empty.");

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().CmbDataType.SelectedValue.ToString()!))
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
