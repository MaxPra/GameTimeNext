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
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesEditViewController : UIXViewControllerBase<CodetablesEditApp, CodetablesEditView>
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
                FnControls.LoadTabContent(GetView().tabControl);
            }


            if (GetView().ViewIndicator.Contains("CN"))
                GetView().TxbTextType.Focus();
            else if (GetView().ViewIndicator.Contains("ED"))
                GetView().TxbDescription.Focus();

            FnControls.SetEnabled(GetView().CmbPermission, GetView().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetView().TxbTextType, GetView().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetView().ChbExportIntoImportPackage, GetView().ViewIndicator.Contains("CN"));
            FnControls.SetEnabled(GetView().ChbNumberRangeActive, GetView().ViewIndicator.Contains("CN"));

            FnControls.SetVisible(GetView().BtnSave, GetView().ViewIndicator.Count != 0);
        }

        protected override void BuildImpl()
        {
            if (GetApp().T1CTABH!.State == UIXTableObjectState.Available)
                FnControls.SetEnabled(GetView().TxbTextType, false);

            ControlParameterProtection();
            ControlCodetableVisibility();
        }

        protected override void CheckImpl()
        {
            CheckTextType();

            if (FnString.IsNullEmptyOrWhitespace(GetView().TxbDescription.Text))
                AddViewError(GetView().TxbDescription, GetErrorMessage("GLO0003", "Description"));

            if (FnString.IsNullEmptyOrWhitespace(GetView().CmbPermission.SelectedValue.ToString()!))
                AddViewError(GetView().CmbPermission, GetErrorMessage("GLO0003", "Permission"));

            if (GetView().TxbDescription.Text.Length > 200)
                AddViewError(GetView().TxbDescription, GetErrorMessage("GLO0004", "Description", "200"));
        }

        private void CheckTextType()
        {
            TextBox control = GetView().TxbTextType;
            string value = control.Text;
            string controlName = "Text type";

            if (FnString.IsNullEmptyOrWhitespace(value))
            {
                AddViewError(control, GetErrorMessage("GLO0003", controlName));
            }
            if (value.Length > 2)
            {
                AddViewError(control, GetErrorMessage("GLO0004", controlName, "2"));
            }
            else if (!Regex.IsMatch(value, @"^[a-zA-Z]*$"))
            {
                AddViewError(control, GetErrorMessage("GLO0007", controlName));
            }

            TXCTABH txctabh = new TXCTABH();
            if (GetView().ViewIndicator.Contains("CN") && txctabh.Exists(value))
            {
                AddViewError(control, GetErrorMessage("GLO0008", controlName, value));
            }
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
            GetViewReturn<CodetablesEditViewReturn>().Canceled = true;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        private void ControlParameterProtection()
        {
            for (int i = 1; i <= 2; i++)
            {
                // Active-Checkbox
                CheckBox checkBox = (CheckBox)GetView().FindName($"ChbParam{i}Active");

                bool isActive = checkBox.IsChecked == true;

                // Description
                TextBox textBoxDescription = (TextBox)GetView().FindName($"TxbParam{i}Description");
                FnControls.SetEnabled(textBoxDescription, isActive);

                // Required-Checkbox
                CheckBox checkBoxRequired = (CheckBox)GetView().FindName($"ChbParam{i}Required");
                FnControls.SetEnabled(checkBoxRequired, isActive);

                // Control Type
                ComboBox comboBoxControlType = (ComboBox)GetView().FindName($"CmbParam{i}ControlType");
                FnControls.SetEnabled(comboBoxControlType, isActive);

                // Type
                ComboBox comboBoxCodetable = (ComboBox)GetView().FindName($"CmbParam{i}Codetable");
                FnControls.SetEnabled(comboBoxCodetable, isActive);
            }
        }

        private void ControlCodetableVisibility()
        {

            bool isControlTypeComboboxOnce = false;

            for (int i = 1; i <= 2; i++)
            {
                // Control Type
                ComboBox comboBoxControlType = (ComboBox)GetView().FindName($"CmbParam{i}ControlType");

                string selectedControlType =
                    (comboBoxControlType.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                    ?? comboBoxControlType.SelectedValue?.ToString()
                    ?? string.Empty;

                bool isControlTypeCombobox = "01".Equals(selectedControlType, StringComparison.Ordinal);
                isControlTypeComboboxOnce |= isControlTypeCombobox;

                // ComboBox for codetable selection
                ComboBox comboBoxCodetable = (ComboBox)GetView().FindName($"CmbParam{i}Codetable");
                FnControls.SetVisible(comboBoxCodetable, isControlTypeCombobox);

                // Heading label
                TextBlock lblCodetable = (TextBlock)GetView().FindName($"LblParamCodetable");
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
