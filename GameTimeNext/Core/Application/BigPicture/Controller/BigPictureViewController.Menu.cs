using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Threading;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Menu, questions and the message boxes of the application (CFMBOX), all in one dialog of the mode:
    /// up / down chooses an option, A confirms, B goes back (cancels a message box).
    /// </summary>
    public partial class BigPictureViewController
    {
        // Menu option keys
        private const string OptionResume = "resume";
        private const string OptionRefresh = "refresh";
        private const string OptionExit = "exit";
        private const string OptionQuitApplication = "quitapp";
        private const string OptionQuitGame = "quitgame";
        private const string OptionKeepPlaying = "keepplaying";
        private const string OptionFilter = "filter";

        // Game menu (X) option keys
        private const string OptionGamePlay = "game.play";
        private const string OptionGameResume = "game.resume";
        private const string OptionGameQuit = "game.quit";
        private const string OptionGameMonitoring = "game.monitoring";
        private const string OptionGameLauncher = "game.launcher";

        // Message box option keys: "mbox." + CFMBOXResult
        private const string MessageBoxOptionPrefix = "mbox.";

        private MenuKind _menuKind = MenuKind.None;

        // Game the game menu was opened for
        private T1PROFI? _menuGame;
        private List<BigPictureMenuOption> _menuOptions = new List<BigPictureMenuOption>();
        private int _menuIndex;

        // Open message box: the frame that keeps CFMBOX.Show waiting, the chosen button and the button of B
        private DispatcherFrame? _messageBoxFrame;
        private CFMBOXResult? _messageBoxResult;
        private CFMBOXResult _messageBoxEscapeResult = CFMBOXResult.None;

        private enum MenuKind
        {
            None,
            Main,
            Game,
            QuitGame,
            MessageBox
        }

        /// <summary>A dialog that was open when a message box came up; shown again afterwards.</summary>
        private record MenuState(MenuKind Kind, string Icon, string Title, string Message, List<BigPictureMenuOption> Options, int Index);

        #region Menu
        private void OpenMainMenu()
        {
            OpenMenu(MenuKind.Main, "\uE700", AppConfig.Root.ApplicationName, string.Empty, new List<BigPictureMenuOption>
            {
                new BigPictureMenuOption(OptionResume, "Resume", "\uE768"),
                new BigPictureMenuOption(OptionFilter, "Filter", "\uE71C"),
                new BigPictureMenuOption(OptionRefresh, "Refresh library", "\uE72C"),
                new BigPictureMenuOption(OptionExit, "Exit Big Picture", "\uE73F"),
                new BigPictureMenuOption(OptionQuitApplication, "Quit " + AppConfig.Root.ApplicationName, "\uE7E8")
            });
        }

        /// <summary>
        /// X on a game: what can be done with it right now (play or back to it, monitoring, quit, launcher).
        /// </summary>
        private void OpenGameMenu(T1PROFI t1profi)
        {
            List<BigPictureMenuOption> options = new List<BigPictureMenuOption>();

            if (t1profi.PFID == _runningPfid)
            {
                options.Add(new BigPictureMenuOption(OptionGameResume, "Back to the game", "\uE768"));
                options.Add(new BigPictureMenuOption(OptionGameQuit, "Quit game", "\uE7E8"));
            }
            else if (GetGameAction(t1profi).Enabled)
            {
                options.Add(new BigPictureMenuOption(OptionGamePlay, "Play", "\uE768"));
            }

            options.Add(CFGameTimeMonitoring.IsMonitoring
                ? new BigPictureMenuOption(OptionGameMonitoring, "Stop monitoring", "\uE71A")
                : new BigPictureMenuOption(OptionGameMonitoring, "Start monitoring", "\uE916"));

            if (FnLauncher.GetLauncher(t1profi) is { SupportsOpenInLauncher: true } && FnLauncher.IsLinked(t1profi))
                options.Add(new BigPictureMenuOption(OptionGameLauncher, "Show in " + FnLauncher.GetLauncherName(t1profi.LNTY), "\uE8A7"));

            _menuGame = t1profi;

            string message = CFGameTimeMonitoring.IsMonitoring
                ? "Monitoring: " + TFPROFI.GetProfileName(AppEnvironment.CurrentPfid)
                : string.Empty;

            OpenMenu(MenuKind.Game, "\uE7FC", t1profi.GANA, message, options);
        }

        private void OpenQuitGameQuestion(T1PROFI t1profi)
        {
            // Keep playing first, so pressing A twice by accident doesn't end the game
            OpenMenu(MenuKind.QuitGame, "\uE7E8", "Quit game", GetErrorMessage("PRO0008", t1profi.GANA), new List<BigPictureMenuOption>
            {
                new BigPictureMenuOption(OptionKeepPlaying, "Keep playing", "\uE768"),
                new BigPictureMenuOption(OptionQuitGame, "Quit game", "\uE7E8")
            });
        }

        private void OpenMenu(MenuKind kind, string icon, string title, string message, List<BigPictureMenuOption> options, int selectedIndex = 0)
        {
            _menuKind = kind;
            _menuOptions = options;

            GetView().txMenuIcon.Text = icon;
            GetView().txMenuTitle.Text = title;
            GetView().txMenuMessage.Text = message;
            GetView().svMenuMessage.ScrollToTop();
            FnControls.SetVisible(GetView().svMenuMessage, !FnString.IsNullEmptyOrWhitespace(message));

            GetView().itcMenuOptions.ItemsSource = _menuOptions;
            FnControls.SetVisible(GetView().grdMenu, true);

            SelectMenuOption(selectedIndex);
            UpdateHints();
        }

        private void CloseMenu()
        {
            _menuKind = MenuKind.None;
            _menuGame = null;
            FnControls.SetVisible(GetView().grdMenu, false);
        }

        private void SelectMenuOption(int index)
        {
            if (_menuOptions.Count == 0)
                return;

            _menuIndex = Math.Clamp(index, 0, _menuOptions.Count - 1);

            for (int i = 0; i < _menuOptions.Count; i++)
                _menuOptions[i].IsSelected = i == _menuIndex;
        }

        private void HandleMenuInput(BigPictureInput input)
        {
            switch (input)
            {
                case BigPictureInput.Up:
                case BigPictureInput.Left:
                    SelectMenuOption(_menuIndex - 1);
                    break;

                case BigPictureInput.Down:
                case BigPictureInput.Right:
                    SelectMenuOption(_menuIndex + 1);
                    break;

                case BigPictureInput.Activate:
                    if (_menuIndex >= 0 && _menuIndex < _menuOptions.Count)
                        ExecuteMenuOption(_menuOptions[_menuIndex].Key);
                    break;

                case BigPictureInput.Back:
                    if (_menuKind == MenuKind.MessageBox)
                        EndMessageBox(_messageBoxEscapeResult);
                    else
                        CloseMenu();
                    break;

                case BigPictureInput.Menu:
                    if (_menuKind == MenuKind.Main)
                        CloseMenu();
                    break;
            }
        }

        private void ExecuteMenuOption(string key)
        {
            if (_menuKind == MenuKind.MessageBox)
            {
                if (Enum.TryParse(key.Substring(MessageBoxOptionPrefix.Length), out CFMBOXResult result))
                    EndMessageBox(result);

                return;
            }

            T1PROFI? menuGame = _menuGame;

            CloseMenu();

            switch (key)
            {
                case OptionFilter:
                    OpenFilterDrawer();
                    break;

                case OptionGamePlay:
                    if (GetSelectedGame() is BigPictureGameTile game)
                        _ = ActivateGameAsync(game);
                    break;

                case OptionGameResume:
                    SwitchToRunningGame();
                    break;

                case OptionGameQuit:
                    if (menuGame != null)
                        OpenQuitGameQuestion(menuGame);
                    break;

                case OptionGameMonitoring:
                    if (menuGame != null)
                        ToggleMonitoring(menuGame);
                    break;

                case OptionGameLauncher:
                    if (menuGame != null)
                        FnLauncher.OpenInLauncher(menuGame);
                    break;

                case OptionRefresh:
                    _ = LoadLibraryAsync();
                    break;

                case OptionExit:
                    ExitBigPicture();
                    break;

                case OptionQuitApplication:
                    System.Windows.Application.Current.MainWindow?.Close();
                    break;

                case OptionQuitGame:
                    QuitRunningGame();
                    break;
            }

            UpdateHints();
        }
        #endregion

        #region Message box (ICFMBOXPresenter)
        /// <summary>
        /// Shows a CFMBOX message in the dialog of the mode and waits for the answer like the dialog window does.
        /// Markdown messages (help, changelog) keep their window, it is made for long formatted texts.
        /// </summary>
        public CFMBOXResult? Show(string title, string message, CFMBOXResult buttons, CFMBOXIcon icon, CFMBOXType type)
        {
            if (type is CFMBOXType.Markdown or CFMBOXType.MarkdownLarge)
                return null;

            if (!View.Dispatcher.CheckAccess())
                return View.Dispatcher.Invoke(() => Show(title, message, buttons, icon, type));

            // Not open, or a message box is already waiting (a second one gets the window)
            if (!GetApp().IsOpen || _messageBoxFrame != null)
                return null;

            MenuState? previousMenu = _menuKind == MenuKind.None
                ? null
                : new MenuState(_menuKind, GetView().txMenuIcon.Text, GetView().txMenuTitle.Text, GetView().txMenuMessage.Text, _menuOptions, _menuIndex);

            List<BigPictureMenuOption> options = BuildMessageBoxOptions(buttons);

            _messageBoxEscapeResult = GetEscapeResult(buttons);
            _messageBoxResult = null;

            OpenMenu(MenuKind.MessageBox, GetMessageBoxIcon(icon), title, message, options);

            // e.g. a question of the monitoring hotkey while a game is in the foreground
            Window? window = Window.GetWindow(View);
            if (window != null && !window.IsActive)
                FnForeground.BringToFront(window);

            _messageBoxFrame = new DispatcherFrame();
            Dispatcher.PushFrame(_messageBoxFrame);
            _messageBoxFrame = null;

            CloseMenu();

            if (previousMenu != null)
                OpenMenu(previousMenu.Kind, previousMenu.Icon, previousMenu.Title, previousMenu.Message, previousMenu.Options, previousMenu.Index);

            UpdateHints();

            return _messageBoxResult ?? _messageBoxEscapeResult;
        }

        /// <summary>
        /// Lets the waiting CFMBOX.Show return (null: with the button of B).
        /// </summary>
        private void EndMessageBox(CFMBOXResult? result)
        {
            if (_messageBoxFrame == null)
                return;

            _messageBoxResult = result ?? _messageBoxEscapeResult;
            _messageBoxFrame.Continue = false;
        }

        private static List<BigPictureMenuOption> BuildMessageBoxOptions(CFMBOXResult buttons)
        {
            List<BigPictureMenuOption> options = new List<BigPictureMenuOption>();

            AddMessageBoxOption(options, buttons, CFMBOXResult.Yes, "Yes", "\uE73E");
            AddMessageBoxOption(options, buttons, CFMBOXResult.No, "No", "\uE711");
            AddMessageBoxOption(options, buttons, CFMBOXResult.Ok, "OK", "\uE73E");
            AddMessageBoxOption(options, buttons, CFMBOXResult.Cancel, "Cancel", "\uE711");

            // A message without buttons can still be closed
            if (options.Count == 0)
                options.Add(new BigPictureMenuOption(MessageBoxOptionPrefix + CFMBOXResult.Ok, "OK", "\uE73E"));

            return options;
        }

        private static void AddMessageBoxOption(List<BigPictureMenuOption> options, CFMBOXResult buttons, CFMBOXResult button, string text, string glyph)
        {
            if (buttons.HasFlag(button))
                options.Add(new BigPictureMenuOption(MessageBoxOptionPrefix + button, text, glyph));
        }

        /// <summary>
        /// B: Cancel, else No, else OK (a pure information is confirmed by B as well).
        /// </summary>
        private static CFMBOXResult GetEscapeResult(CFMBOXResult buttons)
        {
            if (buttons.HasFlag(CFMBOXResult.Cancel))
                return CFMBOXResult.Cancel;

            if (buttons.HasFlag(CFMBOXResult.No))
                return CFMBOXResult.No;

            return CFMBOXResult.Ok;
        }

        private string GetMessageBoxEscapeText()
        {
            return _messageBoxEscapeResult switch
            {
                CFMBOXResult.Cancel => "Cancel",
                CFMBOXResult.No => "No",
                _ => "OK"
            };
        }

        private static string GetMessageBoxIcon(CFMBOXIcon icon)
        {
            return icon switch
            {
                CFMBOXIcon.Info => "\uE946",
                CFMBOXIcon.Question => "\uE9CE",
                CFMBOXIcon.Warning => "\uE7BA",
                CFMBOXIcon.Error => "\uEA39",
                CFMBOXIcon.Success => "\uE73E",
                _ => string.Empty
            };
        }
        #endregion
    }
}
