using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Codetables
{
    public class CodetablesEntryEditApp : UIXApplication
    {
        public CodetablesEntryEditView? CodetablesEntryEditView { get; set; }
        public CodetablesEntryEditViewController? CodetablesEntryEditViewController { get; set; }
        public T1CTABD? T1CTABD { get; set; }

        public override void InitializeApplicationOutput()
        {
            CodetablesEntryEditView = new CodetablesEntryEditView();
            MainView = CodetablesEntryEditView;

            CodetablesEntryEditViewController = new CodetablesEntryEditViewController(this);
            CodetablesEntryEditView.WndController = CodetablesEntryEditViewController;
        }

        public void CreateNew(Action<CodetablesEntryEditViewController.CodetablesEntryEditViewReturn> callback, string txtyp)
        {
            T1CTABD = new TXCTABD().CreateNew();
            T1CTABD.TXTYP = txtyp;

            CodetablesEntryEditView!.ViewIndicator.Clear();
            CodetablesEntryEditView!.ViewIndicator.Add("CN");

            CodetablesEntryEditView.Title = "Add codetable entry";
            CodetablesEntryEditViewController!.SetResultCallback(callback);
            CodetablesEntryEditViewController.Show(true);
        }

        public void Edit(Action<CodetablesEntryEditViewController.CodetablesEntryEditViewReturn> callback, T1CTABD t1ctabd)
        {
            T1CTABD = t1ctabd;

            CodetablesEntryEditView!.ViewIndicator.Clear();
            CodetablesEntryEditView!.ViewIndicator.Add("ED");

            CodetablesEntryEditView.Title = "Edit codetable entry";
            CodetablesEntryEditViewController!.SetResultCallback(callback);
            CodetablesEntryEditViewController.Show(true);
        }

        public void View(T1CTABD t1ctabd)
        {
            T1CTABD = t1ctabd;

            CodetablesEntryEditView!.ViewIndicator.Clear();

            CodetablesEntryEditView.Title = "View codetable entry";
            CodetablesEntryEditViewController!.Show(true);
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
    }
}