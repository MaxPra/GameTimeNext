using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.BigPicture.Views;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.BackgroundProcesses;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using GameTimeNext.Core.Framework.UserInput;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Big Picture mode: pivots (home, library, search) switched with LB / RB, each a row of covers with the selected
    /// game shown large below it (name, times, playthrough, play). Operated with a game controller, the keyboard or the
    /// mouse; the selection is a controller model, not the keyboard focus.
    /// Split by concern: games (pivots, row, details), launch / quit, menu and message boxes, search, filter.
    /// </summary>
    public partial class BigPictureViewController : UIXViewControllerBase, ICFMBOXPresenter
    {
        private readonly GamepadInput _gamepad = new GamepadInput();
        private readonly DispatcherTimer _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };

        // Second attempt to get the focus after a return from a game
        private static readonly TimeSpan ReturnRetryDelay = TimeSpan.FromMilliseconds(400);

        // Brought back from a game, the focus may not have arrived yet (see OnGamepadButtonPressed)
        private bool _isReturning;

        // Init runs on every opening, the view stays the same
        private bool _isIntroHooked;

        // Details of the selected game follow once the selection rests (moving stays smooth while a direction is held)
        private readonly DispatcherTimer _detailsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(110) };

        /// <summary>
        /// What the controller and the keyboard of the mode can do; buttons and keys are mapped to these.
        /// </summary>
        private enum BigPictureInput
        {
            Up,
            Down,
            Left,
            Right,
            PivotPrevious,
            PivotNext,
            JumpPrevious,
            JumpNext,
            First,
            Last,
            Activate,
            Back,
            Menu,
            Search,
            Filter,
            Options
        }

        public BigPictureViewController(UIXApplication app) : base(app)
        {
            _gamepad.ButtonPressed += OnGamepadButtonPressed;
            _gamepad.ConnectionChanged += connected => FnControls.SetVisible(GetView().txControllerConnected, connected);
            _clockTimer.Tick += (_, _) =>
            {
                UpdateClock();
                UpdateMonitoringChip();
            };

            _detailsTimer.Tick += (_, _) =>
            {
                _detailsTimer.Stop();
                UpdateDetails();
                UpdateHints();
            };
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            // GameRunningProcess, only while the mode is open (Deactivate unregisters them)
            GetApp().CallDispatcher.Register(this, nameof(EXEV_BigPictureGameLaunched));
            GetApp().CallDispatcher.Register(this, nameof(EXEV_BigPictureGameClosed));

            // Message boxes of the whole application are shown in the style of the mode while it is open
            CFMBOX.Presenter = this;

            GetView().txApplicationName.Text = AppConfig.Root.ApplicationName;

            // A game may already run when the mode is opened
            _runningPfid = GetGameRunningProcess()?.RunningPfid ?? 0;
            _launchingProfile = null;
            _isQuitting = false;
            _filter = AppEnvironment.GetAppConfig().BigPictureFilterCache ?? new FilterCache();

            HideLaunchOverlay();
            CloseMenu();
            CloseFilterDrawer(apply: false);
            ResetSearch();
            ResetPivots(_runningPfid != 0 ? _runningPfid : AppEnvironment.CurrentPfid);

            FnControls.SetVisible(GetView().txControllerConnected, _gamepad.IsConnected);
            _gamepad.Start();

            UpdateClock();
            UpdateMonitoringChip();
            _clockTimer.Start();

            // Opening animation; the library loads behind it
            if (!_isIntroHooked)
            {
                GetView().ptcIntro.Completed += (_, _) => UpdateHints();
                _isIntroHooked = true;
            }

            GetView().ptcIntro.Title = AppConfig.Root.ApplicationName;
            GetView().ptcIntro.Play();

            // Takes the keyboard focus from the tabs below, so typing doesn't end up in a covered text box
            View.Dispatcher.BeginInvoke(() => View.Focus(), DispatcherPriority.Loaded);
        }

        protected override void BuildFirstImpl()
        {
            BuildKeyboard();
        }

        protected override async Task BuildFirstImplAsync()
        {
            // No busy indicator over the opening animation
            await LoadLibraryAsync(showLoader: !GetView().ptcIntro.IsPlaying);
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
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

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            // Typed with the on-screen keyboard or a real keyboard
            if (eventName == UIXEventNames.TextBox.TextChanged && source.Name == nameof(BigPictureView.txbSearch))
                ApplySearch();
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        /// <summary>
        /// Covers, keys and entries are generated from templates, so their clicks are told apart by their data.
        /// </summary>
        protected override void HandleUIEventImpl(FrameworkElement source, string eventName, RoutedEventArgs args)
        {
            if (eventName != UIXEventNames.Button.Click)
                return;

            bool isBrowsing = _menuKind == MenuKind.None && !_isFilterOpen && !_isLaunchOverlayVisible;

            switch (source.Name)
            {
                case "btnGame" when isBrowsing && source.DataContext is BigPictureGameTile game:
                    ClickGame(game);
                    break;

                case "btnKey" when isBrowsing && source.DataContext is BigPictureKeyTile key:
                    SelectKey(key);
                    PressKey(key);
                    break;

                case "btnPivot" when isBrowsing && source.DataContext is BigPicturePivot pivot:
                    ShowPivot(pivot.Kind);
                    break;

                case "btnMenuOption" when source.DataContext is BigPictureMenuOption option:
                    ExecuteMenuOption(option.Key);
                    break;

                case "btnFilterEntry" when source.DataContext is BigPictureFilterEntry entry:
                    ClickFilterEntry(entry);
                    break;
            }

            UpdateHints();
        }
        #endregion

        #region Public
        /// <summary>
        /// Stops reading the controller and the game notifications, called when the mode is closed.
        /// </summary>
        public void Deactivate()
        {
            _gamepad.Stop();
            _clockTimer.Stop();
            _detailsTimer.Stop();

            // The blackout showed the game selected here: back to the profile selected in the profiles view
            AppEnvironment.SetCurrentProfile(AppEnvironment.CurrentPfid == 0 ? null : new TXPROFI().Read(AppEnvironment.CurrentPfid));

            GetApp().CallDispatcher.Unregister(nameof(EXEV_BigPictureGameLaunched));
            GetApp().CallDispatcher.Unregister(nameof(EXEV_BigPictureGameClosed));

            if (ReferenceEquals(CFMBOX.Presenter, this))
                CFMBOX.Presenter = null;

            EndMessageBox(null);
            CloseMenu();
            CloseFilterDrawer(apply: false);
            HideLaunchOverlay();
        }

        /// <summary>
        /// Keyboard of the main window: arrow keys, Enter (A), Esc / Backspace (B), Page Up / Down (LB / RB), Home / End,
        /// F3 / letters (search), F4 (filter). Keys with modifiers are left to the window (e.g. Alt+F4).
        /// Returns false for keys that are typed into the search field.
        /// </summary>
        public bool HandleKey(KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.None && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
                return false;

            // No focus navigation into the tabs below the mode
            if (e.Key == Key.Tab)
                return true;

            if (GetView().ptcIntro.IsPlaying)
            {
                GetView().ptcIntro.Skip();
                return true;
            }

            // The blackout covers the mode: Esc / Backspace end it, nothing else reaches the hidden mode
            if (CFBlackout.IsFullBlackout())
            {
                if (e.Key is Key.Escape or Key.Back)
                    EndBlackout();

                return true;
            }

            bool isBrowsing = _menuKind == MenuKind.None && !_isFilterOpen && !_isLaunchOverlayVisible;

            if (isBrowsing && IsKeyboardInput())
                return HandleSearchKey(e.Key);

            if (e.KeyboardDevice.Modifiers != ModifierKeys.None)
                return false;

            // Typing a letter or digit opens the search with it (the key itself is typed into the search field)
            if (isBrowsing && IsTypingKey(e.Key))
            {
                ShowPivot(BigPicturePivotKind.Search);
                FocusKeyboard();
                return false;
            }

            BigPictureInput? input = e.Key switch
            {
                Key.Up => BigPictureInput.Up,
                Key.Down => BigPictureInput.Down,
                Key.Left => BigPictureInput.Left,
                Key.Right => BigPictureInput.Right,
                Key.PageUp => BigPictureInput.PivotPrevious,
                Key.PageDown => BigPictureInput.PivotNext,
                Key.Home => BigPictureInput.First,
                Key.End => BigPictureInput.Last,
                Key.Enter or Key.Space => BigPictureInput.Activate,
                Key.Escape or Key.Back => BigPictureInput.Back,
                Key.Apps => BigPictureInput.Menu,
                Key.F3 => BigPictureInput.Search,
                Key.F4 => BigPictureInput.Filter,
                _ => null
            };

            if (input == null)
                return false;

            HandleInput(input.Value);
            return true;
        }
        #endregion

        #region Input
        private void OnGamepadButtonPressed(GamepadButton button)
        {
            Window? window = Window.GetWindow(View);

            if (window == null)
                return;

            // The blackout covers everything: View or B ends it
            if (HandleBlackoutButton(button))
                return;

            if (window.IsActive)
            {
                _isReturning = false;
                HandleActiveGamepadButton(button);
                return;
            }

            // Another window of GameTimeNext above the mode (e.g. the help): the controller works like the keyboard there
            if (System.Windows.Application.Current.Windows.OfType<Window>().Any(w => w.IsActive))
            {
                Key? key = button switch
                {
                    GamepadButton.A => Key.Enter,
                    GamepadButton.B => Key.Escape,
                    GamepadButton.Up => Key.Up,
                    GamepadButton.Down => Key.Down,
                    GamepadButton.Left => Key.Left,
                    GamepadButton.Right => Key.Right,
                    _ => null
                };

                if (key != null)
                    FnKeyboard.PressKeyInActiveWindow(key.Value);

                return;
            }

            // Another application (a game) is in the foreground: the Xbox button or View + Menu brings the mode back
            bool isReturnCombination = button == GamepadButton.Guide
                || (button == GamepadButton.Start && _gamepad.IsHeld(GamepadButton.Back))
                || (button == GamepadButton.Back && _gamepad.IsHeld(GamepadButton.Start));

            if (isReturnCombination)
            {
                FnLog.AddInfo(GetApp(), "Back to the Big Picture mode by controller");
                ReturnFromGame(window);
                return;
            }

            // Back on top, but Windows didn't hand over the focus (e.g. the game runs elevated or the Game Bar took it):
            // the mode is still controlled while it is visibly on top, and asks for the focus again. Only after a
            // return, so a game in a window can't steer the mode by accident.
            if (_isReturning && FnForeground.IsVisibleOnTop(window))
            {
                FnForeground.BringToFront(window);
                HandleActiveGamepadButton(button);
                return;
            }

            _isReturning = false;
        }

        /// <summary>
        /// Buttons while the mode is in front: the Xbox button or View + Menu switch back to the running game,
        /// View alone toggles the blackout, everything else is mapped to the inputs of the mode.
        /// </summary>
        private void HandleActiveGamepadButton(GamepadButton button)
        {
            if (GetView().ptcIntro.IsPlaying)
            {
                GetView().ptcIntro.Skip();
                return;
            }

            bool isBrowsing = _menuKind == MenuKind.None && !_isFilterOpen && !_isLaunchOverlayVisible;

            bool isGameCombination = button == GamepadButton.Guide
                || (button == GamepadButton.Start && _gamepad.IsHeld(GamepadButton.Back))
                || (button == GamepadButton.Back && _gamepad.IsHeld(GamepadButton.Start));

            // Menu pressed first opened the main menu already
            if (isGameCombination && _runningPfid != 0 && (isBrowsing || _menuKind == MenuKind.Main))
            {
                _isGameCombinationUsed = true;
                CloseMenu();
                SwitchToRunningGame();
                return;
            }

            if (button == GamepadButton.Back)
            {
                if (isBrowsing)
                    OnViewButton();

                return;
            }

            HandleGamepadButton(button);
        }

        private void HandleGamepadButton(GamepadButton button)
        {
            BigPictureInput? input = button switch
            {
                GamepadButton.Up => BigPictureInput.Up,
                GamepadButton.Down => BigPictureInput.Down,
                GamepadButton.Left => BigPictureInput.Left,
                GamepadButton.Right => BigPictureInput.Right,
                GamepadButton.LeftShoulder => BigPictureInput.PivotPrevious,
                GamepadButton.RightShoulder => BigPictureInput.PivotNext,
                GamepadButton.LeftTrigger => BigPictureInput.JumpPrevious,
                GamepadButton.RightTrigger => BigPictureInput.JumpNext,
                GamepadButton.A => BigPictureInput.Activate,
                GamepadButton.B => BigPictureInput.Back,
                GamepadButton.X => BigPictureInput.Options,
                GamepadButton.Y => BigPictureInput.Search,
                GamepadButton.Start => BigPictureInput.Menu,
                _ => null
            };

            if (input != null)
                HandleInput(input.Value);
        }

        /// <summary>
        /// Xbox button / View + Menu in a game: the game is minimized first. Otherwise it may keep the focus (Windows
        /// doesn't always hand it over) and still react to the controller while the mode is operated; most games stop
        /// reading the controller once they are minimized. A to "Back to the game" restores it.
        /// </summary>
        private void ReturnFromGame(Window window)
        {
            // The user left the game on purpose: a launched game is not brought to the front anymore
            _launchFrontVersion++;

            T1PROFI? t1profi = _runningPfid == 0 ? null : new TXPROFI().Read(_runningPfid);

            if (t1profi != null && !FnLauncher.MinimizeGame(t1profi))
                FnLog.AddInfo(GetApp(), $"No window of {t1profi.GANA} found to minimize for the Big Picture mode");

            ReturnToFront(window);
        }

        /// <summary>
        /// Brings the mode back to the front (Xbox button, end of a game). Tries once more a moment later, when the
        /// game or the Xbox Game Bar took the focus back meanwhile.
        /// </summary>
        private async void ReturnToFront(Window window)
        {
            _isReturning = true;

            if (FnForeground.BringToFront(window))
                return;

            await Task.Delay(ReturnRetryDelay);

            if (!window.IsActive && FnForeground.IsVisibleOnTop(window))
                FnForeground.BringToFront(window);
        }

        private void HandleInput(BigPictureInput input)
        {
            if (GetView().ptcIntro.IsPlaying)
            {
                GetView().ptcIntro.Skip();
                return;
            }

            if (_menuKind != MenuKind.None)
                HandleMenuInput(input);
            else if (_isLaunchOverlayVisible)
                HandleLaunchOverlayInput(input);
            else if (_isFilterOpen)
                HandleFilterInput(input);
            else if (input is BigPictureInput.PivotPrevious or BigPictureInput.PivotNext)
                ShowAdjacentPivot(input == BigPictureInput.PivotPrevious ? -1 : 1);
            else if (IsKeyboardInput())
                HandleKeyboardInput(input);
            else
                HandleRowInput(input);

            UpdateHints();
        }

        private void HandleRowInput(BigPictureInput input)
        {
            bool isSearch = _pivot == BigPicturePivotKind.Search;

            switch (input)
            {
                case BigPictureInput.Left:
                    SelectGame(_gameIndex - 1);
                    break;

                case BigPictureInput.Right:
                    SelectGame(_gameIndex + 1);
                    break;

                case BigPictureInput.Down:
                    // From the results down into the keyboard
                    if (isSearch)
                        FocusKeyboard();
                    break;

                case BigPictureInput.JumpPrevious:
                    JumpGames(-1);
                    break;

                case BigPictureInput.JumpNext:
                    JumpGames(1);
                    break;

                case BigPictureInput.First:
                    SelectGame(0);
                    break;

                case BigPictureInput.Last:
                    SelectGame(_games.Count - 1);
                    break;

                case BigPictureInput.Activate:
                    if (GetSelectedGame() is BigPictureGameTile game)
                        _ = ActivateGameAsync(game);
                    break;

                case BigPictureInput.Search:
                    ShowPivot(isSearch ? _lastBrowsePivot : BigPicturePivotKind.Search);
                    break;

                case BigPictureInput.Filter:
                    OpenFilterDrawer();
                    break;

                case BigPictureInput.Options:
                    if (GetSelectedGame() is BigPictureGameTile selected)
                        OpenGameMenu(selected.Profile);
                    break;

                case BigPictureInput.Back:
                    if (isSearch)
                        FocusKeyboard();
                    else
                        OpenMainMenu();
                    break;

                case BigPictureInput.Menu:
                    OpenMainMenu();
                    break;
            }
        }

        /// <summary>
        /// What the controller buttons do right now, shown in the hint bar.
        /// </summary>
        private void UpdateHints()
        {
            string hintA = string.Empty;
            string hintB = string.Empty;
            string hintX = string.Empty;
            string hintY = string.Empty;
            string hintView = string.Empty;

            if (GetView().ptcIntro.IsPlaying)
            {
                // Any button skips the opening animation
            }
            else if (_menuKind != MenuKind.None)
            {
                hintA = "Select";
                hintB = _menuKind == MenuKind.MessageBox ? GetMessageBoxEscapeText() : "Back";
            }
            else if (_isLaunchOverlayVisible)
            {
                hintB = _launchingProfile != null ? "Cancel launch" : string.Empty;
            }
            else if (_isFilterOpen)
            {
                hintA = "Switch";
                hintB = "Done";
                hintX = "Reset";
            }
            else if (IsKeyboardInput())
            {
                hintA = "Type";
                hintB = HasSearchText() ? "Delete" : "Back";
                hintX = "Space";
                hintY = "Close";
            }
            else
            {
                hintA = GetSelectedGame() is BigPictureGameTile game && GetGameAction(game.Profile) is { Enabled: true } action
                    ? action.Text
                    : string.Empty;
                hintB = _pivot == BigPicturePivotKind.Search ? "Keyboard" : "Menu";
                hintX = GetSelectedGame() != null ? "Options" : string.Empty;
                hintY = _pivot == BigPicturePivotKind.Search ? "Close" : "Search";
                hintView = "Blackout";
            }

            SetHint(GetView().pnlHintA, GetView().txHintA, hintA);
            SetHint(GetView().pnlHintB, GetView().txHintB, hintB);
            SetHint(GetView().pnlHintX, GetView().txHintX, hintX);
            SetHint(GetView().pnlHintY, GetView().txHintY, hintY);
            SetHint(GetView().pnlHintView, GetView().txHintView, hintView);
        }

        private static void SetHint(FrameworkElement panel, TextBlock text, string hint)
        {
            text.Text = hint;
            FnControls.SetVisible(panel, !FnString.IsNullEmptyOrWhitespace(hint));
        }

        private static bool IsTypingKey(Key key)
        {
            return key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9 or >= Key.NumPad0 and <= Key.NumPad9;
        }
        #endregion

        #region Internal Events
        protected void EV_btnPlay()
        {
            if (GetSelectedGame() is BigPictureGameTile game)
                _ = ActivateGameAsync(game);
        }

        protected void EV_btnSearch()
        {
            ShowPivot(BigPicturePivotKind.Search);
        }

        protected void EV_btnFilter()
        {
            OpenFilterDrawer();
        }

        protected void EV_btnMenu()
        {
            OpenMainMenu();
        }

        protected void EV_btnExitBigPicture()
        {
            ExitBigPicture();
        }
        #endregion

        #region External Events
        protected void EXEV_BigPictureGameLaunched(long pfid)
        {
            _runningPfid = pfid;
            _launchingProfile = null;
            _isQuitting = false;

            HideLaunchOverlay();
            UpdateRunningState();

            BringLaunchedGameToFront(pfid);
        }

        protected async Task EXEV_BigPictureGameClosed(long pfid)
        {
            _launchFrontVersion++;
            _runningPfid = 0;
            _launchingProfile = null;
            _isQuitting = false;

            HideLaunchOverlay();

            if (_menuKind == MenuKind.QuitGame)
                CloseMenu();

            // Back from the game: bring the mode to the front again
            Window? window = Window.GetWindow(View);
            if (window != null)
                ReturnToFront(window);

            // New playtime and order of the recently played games
            await LoadLibraryAsync();
        }
        #endregion

        private void ExitBigPicture()
        {
            // MainWindowController
            GetApp().CallDispatcher.Trigger("EXEV_ExitBigPicture");
        }

        private void UpdateClock()
        {
            GetView().txClock.Text = DateTime.Now.ToString("t", CultureInfo.CurrentCulture);
        }

        private static GameRunningProcess? GetGameRunningProcess()
        {
            return AppEnvironment.StartedBackgroundProcesses.TryGetValue(typeof(GameRunningProcess).FullName!, out var process)
                ? process as GameRunningProcess
                : null;
        }

        private BigPictureApp GetApp()
        {
            return (BigPictureApp)App;
        }

        private BigPictureView GetView()
        {
            return (BigPictureView)View;
        }
    }
}
