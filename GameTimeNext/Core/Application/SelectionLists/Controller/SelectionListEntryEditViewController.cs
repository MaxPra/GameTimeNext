using GameTimeNext.Core.Application.Codetables.Api;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.SelectionLists.Views;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.ComboBoxes;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.SelectionLists.Controller
{
    public class SelectionListEntryEditViewController : UIXViewControllerBase<SelectionListEntryEditApp, SelectionListEntryEditView>
    {
        public SelectionListEntryEditViewController(UIXApplication app) : base(app)
        {
        }

        public class SelectionListEntryEditViewReturn : UIXViewReturn
        {
            public bool HasChanged { get; set; } = false;

            /// <summary>Key of the saved entry (generated, never shown).</summary>
            public string Key { get; set; } = string.Empty;
        }

        protected override void Init()
        {
            ViewReturn = new SelectionListEntryEditViewReturn();

            AddIdentifier("T1CTABD", GetApp().T1CTABD!);
            AddSource("T1CTABD", new TXCTABD());
        }

        protected override void BuildFirstImpl()
        {
            // A delivered entry keeps its name; only its parameters can be changed (see CFSelectionListsApp.CanOpenEntry)
            bool nameLocked = !GetView().ViewIndicator.Contains("CN") && !CFSelectionListsApp.IsEntryEditable(GetApp().List!, GetApp().T1CTABD!);

            GetView().txbName.IsEnabled = !nameLocked;

            if (!nameLocked)
                GetView().txbName.Focus();

            BuildParameters();
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
            CheckName();
            CheckParameters();
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

        protected void EV_btnSave()
        {
            GetViewReturn<SelectionListEntryEditViewReturn>().Canceled = false;
            GetViewReturn<SelectionListEntryEditViewReturn>().HasChanged = GetApp().T1CTABD!.HasChanged();
            GetViewReturn<SelectionListEntryEditViewReturn>().Key = GetApp().T1CTABD!.TXNUM;

            Exit(true);
        }

        private (UIXComboBox Combo, TextBox Text, TextBlock Label, TextBlock Hint, StackPanel Panel, Button Button) GetParameterControls(int index)
        {
            return index == 1
                ? (GetView().cmbParam1, GetView().txbParam1, GetView().lblParam1, GetView().txHintParam1, GetView().pnlParam1, GetView().btnParam1)
                : (GetView().cmbParam2, GetView().txbParam2, GetView().lblParam2, GetView().txHintParam2, GetView().pnlParam2, GetView().btnParam2);
        }

        /// <summary>
        /// Shows the parameters of the list as fields, named by the list (e.g. "Symbol"), with the hint of the list below.
        /// </summary>
        private void BuildParameters()
        {
            T1CTABH list = GetApp().List!;

            for (int i = 1; i <= CFSelectionListsApp.MaxParameterCount; i++)
            {
                if (!CFSelectionListsApp.IsParameterSupported(list, i))
                    continue;

                var (combo, text, label, hint, panel, button) = GetParameterControls(i);

                bool isRequired = list.GetValue<bool>($"PARF{i}");
                string hintText = CFSelectionListsApp.GetParameterHint(list, i);

                label.Text = CFSelectionListsApp.GetParameterLabel(list, i);
                FnControls.SetVisible(panel, true);

                if (CFSelectionListsApp.IsParameterComboBox(list, i))
                {
                    FillParameterCombo(combo, list, i, isRequired);
                    FnControls.SetVisible(combo, true);
                }
                else
                {
                    FnControls.SetVisible(text, true);
                }

                hint.Text = hintText;
                FnControls.SetVisible(hint, !FnString.IsNullEmptyOrWhitespace(hintText));

                // The list may name an API that fills the parameter (e.g. choosing an image)
                if (CFCodetableApi.CreateApi(list, i) is CTAPIBase api)
                {
                    button.Content = api.ButtonText;
                    button.ToolTip = FnString.IsNullEmptyOrWhitespace(api.ButtonToolTip) ? null : api.ButtonToolTip;
                    FnControls.SetVisible(button, true);
                }
            }
        }

        protected void EV_cmbParam1_NewEntry(UIXNewEntryArgs args)
        {
            AddEntryToSourceList(1, args);
        }

        protected void EV_cmbParam2_NewEntry(UIXNewEntryArgs args)
        {
            AddEntryToSourceList(2, args);
        }

        /// <summary>
        /// Opens the dialog for a new entry in the list the parameter takes its choices from. A saved entry is offered in the
        /// ComboBox and selected.
        /// </summary>
        private void AddEntryToSourceList(int index, UIXNewEntryArgs args)
        {
            T1CTABH list = GetApp().List!;

            if (GetExtendableSourceList(list, index) is not T1CTABH sourceList)
                return;

            CFSelectionListsApp.CreateEntry(GetApp(), sourceList, key =>
            {
                using (SuppressRunEventPipeline())
                {
                    FillParameterCombo(GetParameterControls(index).Combo, list, index, list.GetValue<bool>($"PARF{index}"));
                }

                args.SelectKey(key);
            });
        }

        protected async Task EV_btnParam1()
        {
            await RunParameterApiAsync(1);
        }

        protected async Task EV_btnParam2()
        {
            await RunParameterApiAsync(2);
        }

        /// <summary>
        /// Runs the button API of the parameter and writes the returned value into its field.
        /// </summary>
        private async Task RunParameterApiAsync(int index)
        {
            T1CTABH list = GetApp().List!;
            var (combo, text, _, _, _, _) = GetParameterControls(index);

            bool isCombo = CFSelectionListsApp.IsParameterComboBox(list, index);
            string currentValue = isCombo ? combo.SelectedValue?.ToString() ?? string.Empty : text.Text;

            string? value = await CFCodetableApi.RunAsync(GetApp(), Window.GetWindow(GetView()), list, GetApp().T1CTABD!, index, currentValue);

            if (value == null)
                return;

            if (isCombo)
                combo.SelectedValue = value;
            else
                text.Text = value;
        }

        /// <summary>
        /// The list the parameter takes its choices from, or null if the user may not add entries to it.
        /// </summary>
        private static T1CTABH? GetExtendableSourceList(T1CTABH list, int index)
        {
            return CFSelectionListsApp.GetExtendableList(list.GetValue<string>($"PACT{index}") ?? string.Empty);
        }

        private static void FillParameterCombo(UIXComboBox combo, T1CTABH list, int index, bool isRequired)
        {
            string sourceList = list.GetValue<string>($"PACT{index}") ?? string.Empty;

            // The first item lets the user create a missing choice right here (e.g. a new symbol while creating a platform)
            combo.NewEntryText = GetExtendableSourceList(list, index) != null ? "Add new entry" : string.Empty;

            UIXManualCodetable manualCodetable = new UIXManualCodetable();

            foreach (T1CTABD entry in new TXCTABD().GetEntries(sourceList))
                manualCodetable.AddEntry(entry.TXNUM, entry.DESCR);

            // A required parameter offers no "(None)"
            combo.AdditionalEntry = isRequired ? string.Empty : "(None)";

            manualCodetable.ApplyTo(combo);
        }

        private void CheckName()
        {
            TextBox control = GetView().txbName;
            string name = control.Text.Trim();

            if (FnString.IsNullEmptyOrWhitespace(name))
            {
                AddViewError(control, GetErrorMessage("GLO0003", "Name"));
                return;
            }

            // Two entries with the same name can't be told apart in a dropdown
            T1CTABD current = GetApp().T1CTABD!;

            bool isDuplicate = new TXCTABD().GetEntries(current.TXTYP)
                .Any(e => e.TXNUM != current.TXNUM && string.Equals(e.DESCR.Trim(), name, StringComparison.CurrentCultureIgnoreCase));

            if (isDuplicate)
                AddViewError(control, GetErrorMessage("GLO0008", "Name", name));
        }

        /// <summary>
        /// Checks that every required parameter is filled.
        /// </summary>
        private void CheckParameters()
        {
            T1CTABH list = GetApp().List!;

            for (int i = 1; i <= CFSelectionListsApp.MaxParameterCount; i++)
            {
                if (!CFSelectionListsApp.IsParameterSupported(list, i) || !list.GetValue<bool>($"PARF{i}"))
                    continue;

                var (combo, text, _, _, _, _) = GetParameterControls(i);

                string value = CFSelectionListsApp.IsParameterComboBox(list, i)
                    ? combo.SelectedValue?.ToString() ?? string.Empty
                    : text.Text;

                if (FnString.IsNullEmptyOrWhitespace(value))
                    AddViewError(CFSelectionListsApp.IsParameterComboBox(list, i) ? combo : text, GetErrorMessage("GLO0003", CFSelectionListsApp.GetParameterLabel(list, i)));
            }
        }
    }
}
