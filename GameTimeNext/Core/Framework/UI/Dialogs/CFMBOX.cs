using System.Windows;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework.UI.Dialogs
{
    public class CFMBOX : UIXApplication
    {
        private CFMBOXView? _cfmboxView;
        private CFMBOXController? _cfmboxController;

        /// <summary>
        /// While set (Big Picture mode), messages are shown by it instead of the dialog window.
        /// </summary>
        public static ICFMBOXPresenter? Presenter { get; set; }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(CFMBOX));
        }

        public override void InitializeApplicationOutput()
        {
            _cfmboxView = new CFMBOXView();
            MainView = _cfmboxView;

            _cfmboxController = new CFMBOXController(this);
            _cfmboxView.WndController = _cfmboxController;
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, this.WndParent ?? System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string message, CFMBOXResult buttons, CFMBOXIcon icon, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(message, buttons, icon, this.WndParent ?? System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            string title = DeriveTitleFromIcon(icon);
            return Show(title, message, buttons, icon, owner ?? this.WndParent ?? System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(title, message, buttons, icon, System.Windows.Application.Current?.MainWindow, type);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            return Show(title, message, buttons, CFMBOXIcon.Info, owner, type);
        }

        public CFMBOXResult Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner, CFMBOXType type = CFMBOXType.Default)
        {
            CFMBOXResult? presentedResult = Presenter?.Show(title, message, buttons, icon, type);

            if (presentedResult.HasValue)
                return presentedResult.Value;

            CreateFreshDialog();
            return _cfmboxController!.ShowDialog(title, message, buttons, icon, owner, type);
        }

        /// <summary>
        /// Asks for a text (like answering a question): the message with an input field and OK / Cancel.
        /// IsConfirmed of the result is true for OK, Text holds the entered text then. Always the dialog window,
        /// also in the Big Picture mode (its presenter has no input field).
        /// </summary>
        public CFMBOXInputResult ShowInput(string message, string defaultText = "", CFMBOXIcon icon = CFMBOXIcon.Question, string? title = null)
        {
            CreateFreshDialog();
            return _cfmboxController!.ShowInputDialog(title ?? DeriveTitleFromIcon(icon), message, defaultText, icon, this.WndParent ?? System.Windows.Application.Current?.MainWindow);
        }

        /// <summary>
        /// Standard question for UnsavedChangesImpl of a controller: Yes saves (then closes), No discards the changes,
        /// Cancel (or closing the message box) keeps the view open.
        /// </summary>
        public static void AskUnsavedChanges(UIXApplication app, UIXUnsavedChangesArgs args)
        {
            CFMBOXResult result = app.GetApplication<CFMBOX>(UIXApplicationStartTarget.Window)
                .Show(app.GetErrorMessage("GLO0022"), CFMBOXResult.Yes | CFMBOXResult.No | CFMBOXResult.Cancel, CFMBOXIcon.Question);

            args.Save = result == CFMBOXResult.Yes;
            args.Cancel = result != CFMBOXResult.Yes && result != CFMBOXResult.No;
        }

        private void CreateFreshDialog()
        {
            _cfmboxView = new CFMBOXView();
            MainView = _cfmboxView;

            _cfmboxController = new CFMBOXController(this);
            _cfmboxView.WndController = _cfmboxController;
        }

        private static string DeriveTitleFromIcon(CFMBOXIcon icon)
        {
            return icon switch
            {
                CFMBOXIcon.Info => "Information",
                CFMBOXIcon.Question => "Question",
                CFMBOXIcon.Warning => "Warning",
                CFMBOXIcon.Error => "Error",
                CFMBOXIcon.Success => "Success",
                _ => string.Empty,
            };
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Target = UIXApplicationStartTarget.Window;
        }
    }
}
