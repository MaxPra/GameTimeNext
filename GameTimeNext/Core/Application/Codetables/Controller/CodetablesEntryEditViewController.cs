using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.ComboBoxes;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Codetables.CodetablesEntryEditApp;
using GameTimeNext.Core.Framework.UI.Dialogs;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesEntryEditViewController : UIXViewControllerBase<CodetablesEntryEditApp, CodetablesEntryEditView>
    {

        private const int MAX_PARAM_COUNT = 2;

        Dictionary<string, ParameterControl> _parmControls = new Dictionary<string, ParameterControl>();

        public CodetablesEntryEditViewController(UIXApplication app) : base(app)
        {
        }

        public class CodetablesEntryEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
            public string Platform { get; set; } = string.Empty;
            public bool Skipped { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new CodetablesEntryEditViewReturn();

            AddIdentifier("T1CTABD", GetApp().T1CTABD!);
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void BuildFirstImpl()
        {

            ApplyRunParameters();

            if (GetView().ViewIndicator.Contains("ED"))
                GetView().TxbDescription.Focus();
            if (GetView().ViewIndicator.Contains("CN"))
                GetView().TxbTextNumber.Focus();

            FnControls.SetEnabled(GetView().TxbTextNumber, GetView().ViewIndicator.Contains("CN"));
            FnControls.SetVisible(GetView().BtnSave, GetView().ViewIndicator.Count != 0);

            _parmControls = ControlParameterVisibility();
            BuildManualCodetableParameters();
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
            CheckParameterRequired();

            CheckTextNumber();
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

        protected void EV_BtnSave()
        {
            GetViewReturn<CodetablesEntryEditViewReturn>().Canceled = false;
            GetViewReturn<CodetablesEntryEditViewReturn>().HasChanged = GetApp().T1CTABD!.HasChanged();
            GetViewReturn<CodetablesEntryEditViewReturn>().Platform = GetView().TxbTextNumber.Text;

            Exit(true);
        }

        protected void EV_BtnSkip()
        {
            GetViewReturn<CodetablesEntryEditViewReturn>().Canceled = false;
            GetViewReturn<CodetablesEntryEditViewReturn>().HasChanged = false;
            GetViewReturn<CodetablesEntryEditViewReturn>().Skipped = true;
            GetViewReturn<CodetablesEntryEditViewReturn>().Platform = GetView().TxbTextNumber.Text;

            Exit(true);
        }

        private void ApplyRunParameters()
        {
            CodetablesEntryEditAppRunParameters runParameters = GetApp().RunParameters;

            // Override Save Button Text
            if (!FnString.IsNullEmptyOrWhitespace(runParameters.OverrideSaveButtonText))
                GetView().BtnSave.Content = runParameters.OverrideSaveButtonText;

            // Show Skipbutton
            FnControls.SetVisible(GetView().BtnSkip, runParameters.ShowSkipButton);
        }

        private void BuildManualCodetableParameters()
        {
            TXCTABD txctabd = new TXCTABD();

            T1CTABH t1ctabh = new TXCTABH().Read(GetApp().T1CTABD!.TXTYP)!;

            for (int i = 1; i <= MAX_PARAM_COUNT; i++)
            {
                string codetableParm = t1ctabh.GetValue<string>($"PACT{i}")!;
                string controlType = t1ctabh.GetValue<string>($"PACO{i}")!;
                string controlTooltip = t1ctabh.GetValue<string>($"PTOL{i}")!;

                if (FnString.IsNullEmptyOrWhitespace(codetableParm) || controlType != ControlType.ComboBox.Code)
                    continue;

                List<T1CTABD> t1ctabds = txctabd.GetEntries(codetableParm);

                UIXManualCodetable manualCodetable = new UIXManualCodetable();

                foreach (T1CTABD entry in t1ctabds)
                {
                    manualCodetable.AddEntry(entry.TXNUM, entry.DESCR);
                }

                UIXComboBox paramCombobox = (UIXComboBox)GetView().FindName($"{ControlType.ByCode[controlType].XamlPrefix}Param{i}");

                // A required parameter offers no "(None)"
                paramCombobox.AdditionalEntry = t1ctabh.GetValue<bool>($"PARF{i}") ? string.Empty : "(None)";

                manualCodetable.ApplyTo(paramCombobox);
                paramCombobox.ToolTip = controlTooltip;
            }
        }

        /// <summary>
        /// Controls the visibility of the additional parameter controls depending on the T1CTABH settings and stores them in a dictionary for later validation
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, ParameterControl> ControlParameterVisibility()
        {

            Dictionary<string, ParameterControl> paramControls = new Dictionary<string, ParameterControl>();

            // Read codetable header
            T1CTABH t1ctabh = new TXCTABH().Read(GetApp().T1CTABD!.TXTYP)!;

            for (int i = 1; i <= MAX_PARAM_COUNT; i++)
            {
                // Check whether the parameter is active at all
                if (t1ctabh.GetValue<bool>($"PAAC{i}") == false)
                    continue;

                string controlType = t1ctabh.GetValue<string>($"PACO{i}")!;
                string tooltip = t1ctabh.GetValue<string>($"PTOL{i}")!;

                FrameworkElement control = (FrameworkElement)GetView().FindName($"{ControlType.ByCode[controlType].XamlPrefix}Param{i}");
                TextBlock label = (TextBlock)GetView().FindName($"LblParam{i}");

                if (control == null)
                    continue;

                FnControls.SetVisible(control, true);

                label.Text = t1ctabh.GetValue<string>($"PADE{i}");

                FnControls.SetVisible(label, true);

                control.ToolTip = tooltip;

                paramControls.Add(control.Name, new ParameterControl
                {
                    ControlName = control.Name,
                    IsRequired = t1ctabh.GetValue<bool>($"PARF{i}"),
                    Control = control
                });
            }

            return paramControls;
        }

        private void CheckTextNumber()
        {
            TextBox control = GetView().TxbTextNumber;
            string value = control.Text;
            T1CTABH t1ctabh = new TXCTABH().Read(GetApp().T1CTABD!.TXTYP)!;

            if (FnString.IsNullEmptyOrWhitespace(value))
            {
                AddViewError(control, GetErrorMessage("GLO0003", "Textnumber"));
            }
            if (value.Split('_').Last().Length > 5)
            {
                AddViewError(control, GetErrorMessage("GLO0004", "Textnumber", "5"));
            }

            if (t1ctabh!.NRANA)
            {
                if (FnSystem.IsDebug() && !FnSystem.RunWithUserPermissions())
                {
                    // Dev-Mode
                    if (!value.StartsWith("D_") && !value.StartsWith("U_"))
                    {
                        AddViewError(control, GetErrorMessage("GLO0010", "Textnumber", "D_ or U_"));
                    }
                }
                else
                {
                    // User-Mode
                    if (value.StartsWith("D_"))
                    {
                        AddViewError(control, GetErrorMessage("CTA0001"));
                    }
                    else if (!value.StartsWith("U_"))
                    {
                        AddViewError(control, GetErrorMessage("GLO0010", "Textnumber", "U_"));
                    }
                }
            }

            TXCTABD txctabd = new TXCTABD();
            if (GetView().ViewIndicator.Contains("CN") && txctabd.Exists(t1ctabh.TXTYP, value))
            {
                AddViewError(control, GetErrorMessage("GLO0008", "Textnumber", value));
            }
        }

        /// <summary>
        /// Checks whether the additional parameters are filled (if required) and shows an error message if not
        /// </summary>
        private void CheckParameterRequired()
        {
            if (_parmControls.Count == 0)
                return;

            // Read codetable header
            T1CTABH t1ctabh = new TXCTABH().Read(GetApp().T1CTABD!.TXTYP)!;

            for (int i = 1; i <= _parmControls.Count; i++)
            {
                ParameterControl paramControl = _parmControls.ElementAt(i - 1).Value;

                // No check for checkboxes (even if required)
                // It makes no sense, because a checkbox can only be true or false
                if (!paramControl.IsRequired || paramControl.Control is CheckBox)
                {
                    continue;
                }

                string value = string.Empty;

                if (paramControl.Control is TextBox textBox)
                {
                    value = textBox.Text;
                }
                else if (paramControl.Control is ComboBox comboBox)
                {
                    value = comboBox.SelectedValue.ToString()!;
                }

                if (FnString.IsNullEmptyOrWhitespace(value))
                {
                    AddViewError(paramControl.Control!, GetErrorMessage("GLO0003", "Parameter"));
                }
            }
        }
    }
}
