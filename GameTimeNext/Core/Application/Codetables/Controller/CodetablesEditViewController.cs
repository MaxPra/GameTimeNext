using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesEditViewController : UIXViewControllerBase
    {
        public CodetablesEditViewController(UIXApplication app) : base(app)
        {
        }

        public class CodetablesEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new CodetablesEditViewReturn();


            AddIdentifier("T1CTABH", GetApp().T1CTABH!);
            AddSource("T1CTABD", new TXCTABD());
            AddSource("T1CTABH", new TXCTABH());

            Console.WriteLine(GetApp().T1CTABH!.PTOL1);
        }

        protected override void BuildFirstImpl()
        {
            using (SuppressRunEventPipeline())
            {
                FnControls.LoadTabContent(GetWnd().tabControl);
            }


            if (GetWnd().ViewIndicator.Contains("CN"))
                GetWnd().TxbTextType.Focus();
            else if (GetWnd().ViewIndicator.Contains("ED"))
                GetWnd().TxbDescription.Focus();

            FnControls.SetEnabled(GetWnd().CmbPermission, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().TxbTextType, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().ChbExportIntoImportPackage, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetWnd().ChbNumberRangeActive, GetWnd().ViewIndicator.Contains("CN"));

            FnControls.SetVisible(GetWnd().BtnSave, GetWnd().ViewIndicator.Count != 0);
        }

        protected override void BuildImpl()
        {
            if (GetApp().T1CTABH!.State == UIXTableObjectState.Available)
                FnControls.SetEnabled(GetWnd().TxbTextType, false);

            ControlParameterProtection();
            ControlCodetableVisibility();
        }

        protected override void CheckImpl()
        {
            CheckTextType();

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().TxbDescription.Text))
                AddViewError(GetWnd().TxbDescription, FnErrorMessage.ErrorMessage.CannotBeEmpty.GetMessage("Description"));

            if (FnString.IsNullEmptyOrWhitespace(GetWnd().CmbPermission.SelectedValue.ToString()!))
                AddViewError(GetWnd().CmbPermission, FnErrorMessage.ErrorMessage.MustBeSelected.GetMessage("Permission"));

            if (GetWnd().TxbDescription.Text.Length > 200)
                AddViewError(GetWnd().TxbDescription, FnErrorMessage.ErrorMessage.CannotExceedChars.GetMessage("Description", "200"));
        }

        private void CheckTextType()
        {
            TextBox control = GetWnd().TxbTextType;
            string value = control.Text;
            string controlName = "Text type";

            if (FnString.IsNullEmptyOrWhitespace(value))
            {
                AddViewError(control, FnErrorMessage.ErrorMessage.IsRequired.GetMessage(controlName));
            }
            if (value.Length > 2)
            {
                AddViewError(control, FnErrorMessage.ErrorMessage.CannotExceedChars.GetMessage(controlName, "2"));
            }
            else if (!Regex.IsMatch(value, @"^[a-zA-Z]*$"))
            {
                AddViewError(control, FnErrorMessage.ErrorMessage.InvalidFormat.GetMessage(controlName));
            }

            TXCTABH txctabh = new TXCTABH();
            if (GetWnd().ViewIndicator.Contains("CN") && txctabh.Exists(value))
            {
                AddViewError(control, FnErrorMessage.ErrorMessage.AlreadyExists.GetMessage(controlName, value));
            }
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
            GetViewReturn<CodetablesEditViewReturn>().Canceled = true;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private CodetablesEditApp GetApp()
        {
            return (CodetablesEditApp)App;
        }

        private CodetablesEditView GetWnd()
        {
            return (CodetablesEditView)View;
        }

        private void ControlParameterProtection()
        {
            for (int i = 1; i <= 2; i++)
            {
                // Active-Checkbox
                CheckBox checkBox = (CheckBox)GetWnd().FindName($"ChbParam{i}Active");

                bool isActive = checkBox.IsChecked == true;

                // Description
                TextBox textBoxDescription = (TextBox)GetWnd().FindName($"TxbParam{i}Description");
                FnControls.SetEnabled(textBoxDescription, isActive);

                // Required-Checkbox
                CheckBox checkBoxRequired = (CheckBox)GetWnd().FindName($"ChbParam{i}Required");
                FnControls.SetEnabled(checkBoxRequired, isActive);

                // Control Type
                ComboBox comboBoxControlType = (ComboBox)GetWnd().FindName($"CmbParam{i}ControlType");
                FnControls.SetEnabled(comboBoxControlType, isActive);

                // Type
                ComboBox comboBoxCodetable = (ComboBox)GetWnd().FindName($"CmbParam{i}Codetable");
                FnControls.SetEnabled(comboBoxCodetable, isActive);
            }
        }

        private void ControlCodetableVisibility()
        {

            bool isControlTypeComboboxOnce = false;

            for (int i = 1; i <= 2; i++)
            {
                // Control Type
                ComboBox comboBoxControlType = (ComboBox)GetWnd().FindName($"CmbParam{i}ControlType");

                string selectedControlType =
                    (comboBoxControlType.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                    ?? comboBoxControlType.SelectedValue?.ToString()
                    ?? string.Empty;

                bool isControlTypeCombobox = "01".Equals(selectedControlType, StringComparison.Ordinal);
                isControlTypeComboboxOnce |= isControlTypeCombobox;

                // ComboBox for codetable selection
                ComboBox comboBoxCodetable = (ComboBox)GetWnd().FindName($"CmbParam{i}Codetable");
                FnControls.SetVisible(comboBoxCodetable, isControlTypeCombobox);

                // Heading label
                TextBlock lblCodetable = (TextBlock)GetWnd().FindName($"LblParamCodetable");
                FnControls.SetVisible(lblCodetable, isControlTypeCombobox || isControlTypeComboboxOnce);
            }
        }

        protected void EV_BtnSave()
        {
            GetViewReturn<CodetablesEditViewReturn>().HasChanged = GetApp().T1CTABH!.HasChanged();

            Exit(true);
        }
    }
}
