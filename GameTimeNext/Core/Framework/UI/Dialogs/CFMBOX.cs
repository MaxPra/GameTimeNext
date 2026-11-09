using System.Windows;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework.UI.Dialogs
{
    public class CFMBOX : UIXApplication
    {
        private CFMBOXView? _cfmboxView;
        private CFMBOXController? _cfmboxController;

        // Presenter that shows the current message (null: the dialog window), target of the countdown
        private ICFMBOXPresenter? _activePresenter;

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
            return ShowCore(title, message, buttons, icon, owner, type, null);
        }

        /// <summary>
        /// Message that answers itself with timeout.Result if the user doesn't answer within timeout.Duration (countdown below
        /// the message). Closing the window without a button returns CFMBOXResult.None.
        /// </summary>
        public CFMBOXResult Show(string message, CFMBOXResult buttons, CFMBOXIcon icon, CFMBOXTimeout timeout)
        {
            return ShowCore(DeriveTitleFromIcon(icon), message, buttons, icon, this.WndParent ?? System.Windows.Application.Current?.MainWindow, CFMBOXType.Default, timeout);
        }

        private CFMBOXResult ShowCore(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, Window? owner, CFMBOXType type, CFMBOXTimeout? timeout)
        {
            DateTime startedAt = DateTime.Now;
            string shownMessage = timeout == null ? message : timeout.AppendCountdown(message, timeout.GetRemainingSeconds(startedAt));

            // One countdown for both ways of showing: it only ticks while the message waits (nested dispatcher frame)
            DispatcherTimer? timer = timeout == null ? null : StartCountdown(message, timeout, startedAt);

            try
            {
                _activePresenter = Presenter;

                CFMBOXResult? presentedResult = Presenter?.Show(title, shownMessage, buttons, icon, type);

                if (presentedResult.HasValue)
                    return presentedResult.Value;

                // Not shown by the presenter: the dialog window
                _activePresenter = null;

                CreateFreshDialog();
                return _cfmboxController!.ShowDialog(title, shownMessage, buttons, icon, owner, type);
            }
            finally
            {
                timer?.Stop();
                _activePresenter = null;
            }
        }

        private DispatcherTimer StartCountdown(string message, CFMBOXTimeout timeout, DateTime startedAt)
        {
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };

            timer.Tick += (_, _) =>
            {
                int remainingSeconds = timeout.GetRemainingSeconds(startedAt);

                if (remainingSeconds <= 0)
                {
                    timer.Stop();

                    if (_activePresenter != null)
                        _activePresenter.Close(timeout.Result);
                    else
                        _cfmboxController?.CloseWithResult(timeout.Result);

                    return;
                }

                string countdownMessage = timeout.AppendCountdown(message, remainingSeconds);

                if (_activePresenter != null)
                    _activePresenter.UpdateMessage(countdownMessage);
                else
                    _cfmboxController?.UpdateMessage(countdownMessage);
            };

            timer.Start();
            return timer;
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
