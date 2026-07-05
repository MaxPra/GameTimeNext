using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables
{
    public class CodetablesEntryEditApp : UIXApplication
    {
        public CodetablesEntryEditView? CodetablesEntryEditView { get; set; }
        public CodetablesEntryEditViewController? CodetablesEntryEditViewController { get; set; }
        public T1CTABD? T1CTABD { get; set; }

        public CodetablesEntryEditAppRunParameters RunParameters { get; set; } = new CodetablesEntryEditAppRunParameters();

        public override void InitializeApplicationOutput()
        {
            CodetablesEntryEditView = new CodetablesEntryEditView();
            MainView = CodetablesEntryEditView;

            CodetablesEntryEditViewController = new CodetablesEntryEditViewController(this);
            CodetablesEntryEditView.ViewController = CodetablesEntryEditViewController;
        }

        public void CreateNew(Action<CodetablesEntryEditViewController.CodetablesEntryEditViewReturn> callback, string txtyp)
        {
            T1CTABD = new TXCTABD().CreateNew();
            T1CTABD.TXTYP = txtyp;

            CodetablesEntryEditView!.ViewIndicator.Clear();
            CodetablesEntryEditView!.ViewIndicator.Add("CN");

            if (FnString.IsNullEmptyOrWhitespace(OverrideTitle))
                CodetablesEntryEditView.Title = "Add codetable entry";
            else
                CodetablesEntryEditView.Title = OverrideTitle;

            CodetablesEntryEditViewController!.SetResultCallback(callback);

            CodetablesEntryEditViewController.Show();
        }

        public void Edit(Action<CodetablesEntryEditViewController.CodetablesEntryEditViewReturn> callback, T1CTABD t1ctabd)
        {
            T1CTABD = t1ctabd;

            CodetablesEntryEditView!.ViewIndicator.Clear();
            CodetablesEntryEditView!.ViewIndicator.Add("ED");

            if (FnString.IsNullEmptyOrWhitespace(OverrideTitle))
                CodetablesEntryEditView.Title = "Edit codetable entry";
            else
                CodetablesEntryEditView.Title = OverrideTitle;

            CodetablesEntryEditViewController!.SetResultCallback(callback);
            CodetablesEntryEditViewController.Show();
        }

        public void View(T1CTABD t1ctabd)
        {
            T1CTABD = t1ctabd;

            CodetablesEntryEditView!.ViewIndicator.Clear();

            if (FnString.IsNullEmptyOrWhitespace(OverrideTitle))
                CodetablesEntryEditView.Title = "View codetable entry";
            else
                CodetablesEntryEditView.Title = OverrideTitle;

            CodetablesEntryEditViewController!.Show();
        }

        public sealed class ParameterControl
        {
            public bool IsRequired { get; set; } = false;
            public string ControlName { get; set; } = string.Empty;

            public FrameworkElement? Control { get; set; }
        }

        public sealed class ControlType
        {
            public string Code { get; }
            public string XamlPrefix { get; }

            private ControlType(string code, string xamlPrefix)
            {
                Code = code;
                XamlPrefix = xamlPrefix;
            }

            public static readonly ControlType ComboBox = new("01", "Cmb");
            public static readonly ControlType TextBox = new("02", "Txt");
            public static readonly ControlType CheckBox = new("03", "Cb");

            public static readonly IReadOnlyDictionary<string, ControlType> ByCode =
                new Dictionary<string, ControlType>(StringComparer.Ordinal)
                {
                    [ComboBox.Code] = ComboBox,
                    [TextBox.Code] = TextBox,
                    [CheckBox.Code] = CheckBox
                };
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}