using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Codetables.CodetablesEntryEditApp;

namespace GameTimeNext.Core.Application.Codetables.Controller
{
    public class CodetablesEntryEditViewController : UIXWindowControllerBase
    {

        private const int MAX_PARAM_COUNT = 2;

        bool _hasChanged = false;
        Dictionary<string, ParameterControl> _parmControls = new Dictionary<string, ParameterControl>();

        public CodetablesEntryEditViewController(UIXApplication app) : base(app)
        {
        }

        public class CodetablesEntryEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;
        }

        protected override void Init()
        {
            ViewReturn = new CodetablesEntryEditViewReturn();

            AddIdentifier("T1CTABD", GetApp().T1CTABD!);
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void BuildFirstImpl()
        {

            if (GetWnd().ViewIndicator.Contains("ED"))
                GetWnd().TxbDescription.Focus();
            if (GetWnd().ViewIndicator.Contains("CN"))
                GetWnd().TxbTextNumber.Focus();

            FnControls.SetEnabled(GetWnd().TxbTextNumber, GetWnd().ViewIndicator.Contains("CN"));
            FnControls.SetVisible(GetWnd().BtnSave, GetWnd().ViewIndicator.Count != 0);

            _parmControls = ControlParameterVisibility();
            BuildManuelCodetableParameters();
        }

        protected override void BuildImpl()
        {
        }

        protected override void Check()
        {
            CheckParameterRequired();
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
            GetViewReturn<CodetablesEntryEditViewReturn>().Canceled = true;
        }

        protected override void Event_Minimize()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected void EV_BtnSave()
        {
            GetViewReturn<CodetablesEntryEditViewReturn>().HasChanged = GetApp().T1CTABD!.HasChanged();

            Exit(true);
        }

        private CodetablesEntryEditApp GetApp()
        {
            return (CodetablesEntryEditApp)App;
        }

        private CodetablesEntryEditView GetWnd()
        {
            return (CodetablesEntryEditView)View;
        }

        private void BuildManuelCodetableParameters()
        {
            TXCTABD txctabd = new TXCTABD();

            T1CTABH t1ctabh = new TXCTABH().Read(GetApp().T1CTABD!.TXTYP);

            for (int i = 1; i <= MAX_PARAM_COUNT; i++)
            {
                string codetableParm = t1ctabh.GetValue<string>($"PACT{i}")!;
                string controlType = t1ctabh.GetValue<string>($"PACO{i}")!;

                if (FnString.IsNullEmptyOrWhitespace(codetableParm) || controlType != ControlType.ComboBox.Code)
                    continue;

                List<T1CTABD> t1ctabds = txctabd.GetEntries(codetableParm);

                UIXManualCodetable manualCodetable = new UIXManualCodetable();

                foreach (T1CTABD entry in t1ctabds)
                {
                    manualCodetable.AddEntry(entry.TXNUM, entry.DESCR);
                }

                ComboBox paramCombobox = (ComboBox)GetWnd().FindName($"{ControlType.ByCode[controlType].XamlPrefix}Param{i}");

                if (!t1ctabh.GetValue<bool>($"PARF{i}"))
                {
                    manualCodetable.AddAdditionalEntry("(None)");
                }

                manualCodetable.ApplyTo(paramCombobox);
            }
        }

        /// <summary>
        /// Steuert abhängig der Einstellungen in T1CTABH die Sichtbarkeit der zusätzlichen Parameter Controls und speichert diese in einem Dictionary für die weitere Validierung ab
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, ParameterControl> ControlParameterVisibility()
        {

            Dictionary<string, ParameterControl> paramControls = new Dictionary<string, ParameterControl>();

            // Codetabellen-Kopf lesen
            T1CTABH t1ctabh = new TXCTABH().Read(GetApp().T1CTABD!.TXTYP);

            for (int i = 1; i <= MAX_PARAM_COUNT; i++)
            {
                // Prüfen, ob der Parameter überhaupt aktiv ist
                if (t1ctabh.GetValue<bool>($"PAAC{i}") == false)
                    continue;

                string controlType = t1ctabh.GetValue<string>($"PACO{i}")!;

                FrameworkElement control = (FrameworkElement)GetWnd().FindName($"{ControlType.ByCode[controlType].XamlPrefix}Param{i}");
                TextBlock label = (TextBlock)GetWnd().FindName($"LblParam{i}");

                if (control == null)
                    continue;

                FnControls.SetVisible(control, true);

                label.Text = t1ctabh.GetValue<string>($"PADE{i}");

                FnControls.SetVisible(label, true);

                paramControls.Add(control.Name, new ParameterControl
                {
                    ControlName = control.Name,
                    IsRequired = t1ctabh.GetValue<bool>($"PARF{i}"),
                    Control = control
                });
            }

            return paramControls;
        }

        /// <summary>
        /// Prüft, ob die zusätzlichen Parameter befüllt (falls required) und gib ggf. Fehlermeldung aus
        /// </summary>
        private void CheckParameterRequired()
        {
            if (_parmControls.Count == 0)
                return;

            // Codetabellen-Kopf lesen
            T1CTABH t1ctabh = new TXCTABH().Read(GetApp().T1CTABD!.TXTYP);

            for (int i = 1; i <= _parmControls.Count; i++)
            {
                ParameterControl paramControl = _parmControls.ElementAt(i - 1).Value;

                // Bei Checkbox erfolgt keine Prüfung (auch wenn required)
                // Das macht nämlich keinen Sinn, weil eine Checkbox nur true oder false sein kann
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
                    AddViewError(paramControl.Control!, "This Parameter is required.");
                }
            }
        }
    }
}