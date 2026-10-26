using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.UserInput;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Playtime monitoring and blackout from the controller.
    /// </summary>
    public partial class BigPictureViewController
    {
        // View pressed: waits this long for Menu, View + Menu switches to the running game instead of the blackout
        private static readonly TimeSpan ViewButtonDelay = TimeSpan.FromMilliseconds(250);

        private bool _isViewButtonPending;

        // View + Menu was used as combination meanwhile: no blackout for that View
        private bool _isGameCombinationUsed;

        #region Monitoring
        /// <summary>
        /// Starts monitoring the game, or stops the running monitoring. Works like the monitoring hotkey: asks for a
        /// playthrough if none is active, selects the game in the profiles view (the session is stored for it) and
        /// blacks out the side monitors if that is set up.
        /// </summary>
        private void ToggleMonitoring(T1PROFI t1profi)
        {
            if (CFGameTimeMonitoring.IsMonitoring)
            {
                T1PROFI? monitored = new TXPROFI().Read(AppEnvironment.CurrentPfid);

                ToggleMonitoringBlackout(monitored);

                CFGameTimeMonitoring.StopMonitoring();

                // ProfilesViewController: stores the session of the current profile
                GetApp().CallDispatcher.Trigger("EXEV_GameTimeMonitoringStopped");

                FnLog.AddInfo(GetApp(), "Monitoring stopped from the Big Picture mode");

                UpdateMonitoringChip();
                EnsureFocus();
                _ = LoadLibraryAsync();
                return;
            }

            // Shown as a question of the mode (CFMBOX.Presenter)
            if (!CFProfilesApp.AskForNewPlaythroughCreationIfNotActive(t1profi.PFID, GetApp()))
                return;

            // ProfilesViewController: the session is stored for the selected profile
            AppEnvironment.CurrentPfid = t1profi.PFID;
            GetApp().CallDispatcher.Trigger("EXEV_SwitchProfile");

            ToggleMonitoringBlackout(t1profi);

            CFGameTimeMonitoring.StartMonitoring(t1profi.PFID);
            GetApp().CallDispatcher.Trigger("EXEV_GameTimeMonitoringStarted");

            FnLog.AddInfo(GetApp(), $"Monitoring of {t1profi.GANA} started from the Big Picture mode");

            UpdateMonitoringChip();
            EnsureFocus();
        }

        /// <summary>
        /// Side monitors (all monitors for an external game) while monitoring, like the monitoring hotkey.
        /// </summary>
        private static void ToggleMonitoringBlackout(T1PROFI? t1profi)
        {
            if (t1profi == null || !CFBlackout.AllowedToToggleBlackout(t1profi))
                return;

            CFBlackout.ToggleSecondaryBlackout(System.Windows.Application.Current.MainWindow, false, TFPROFI.IsExternalGame(t1profi));
        }

        /// <summary>
        /// Top bar: which game is monitored right now (also when started with the hotkey, refreshed with the clock).
        /// </summary>
        private void UpdateMonitoringChip()
        {
            bool isMonitoring = CFGameTimeMonitoring.IsMonitoring;

            FnControls.SetVisible(GetView().pnlMonitoringChip, isMonitoring);

            if (isMonitoring)
                GetView().txMonitoringChip.Text = TFPROFI.GetProfileName(AppEnvironment.CurrentPfid);
        }
        #endregion

        #region Blackout
        /// <summary>
        /// While the blackout covers all screens, View or B ends it; other buttons do nothing. The blackout of the
        /// secondary monitors while monitoring doesn't cover the mode, the buttons work as usual then.
        /// </summary>
        private bool HandleBlackoutButton(GamepadButton button)
        {
            if (!CFBlackout.IsFullBlackout())
                return false;

            if (button is GamepadButton.Back or GamepadButton.B)
                EndBlackout();

            return true;
        }

        private void EndBlackout()
        {
            CFBlackout.Disable(View.Dispatcher);
            EnsureFocus();
        }

        /// <summary>
        /// Takes the focus back if a window of the blackout or another one took it.
        /// </summary>
        private void EnsureFocus()
        {
            Window? window = Window.GetWindow(View);

            if (window != null && !window.IsActive)
                ReturnToFront(window);
        }

        /// <summary>
        /// View: blackout of all monitors, unless Menu follows right away (View + Menu: back to the running game).
        /// </summary>
        private async void OnViewButton()
        {
            if (_isViewButtonPending)
                return;

            _isViewButtonPending = true;
            _isGameCombinationUsed = false;

            await Task.Delay(ViewButtonDelay);

            _isViewButtonPending = false;

            if (_isGameCombinationUsed || _gamepad.IsHeld(GamepadButton.Start))
                return;

            if (CFBlackout.IsActive())
            {
                EndBlackout();
                return;
            }

            // The animation of the blackout shows the current profile: the game selected here
            if (GetSelectedGame() is BigPictureGameTile game)
                AppEnvironment.SetCurrentProfile(game.Profile);

            CFBlackout.Enable(System.Windows.Application.Current.MainWindow);
            EnsureFocus();
        }
        #endregion

        #region Running game
        /// <summary>
        /// Back to the running game (A on it, or View + Menu / the Xbox button while the mode has the focus).
        /// </summary>
        private void SwitchToRunningGame()
        {
            T1PROFI? t1profi = _runningPfid == 0 ? null : new TXPROFI().Read(_runningPfid);

            if (t1profi == null)
                return;

            if (!FnLauncher.SwitchToGame(t1profi))
                new ToastMessage("Game not found", $"The window of {t1profi.GANA} could not be found.\nMinimize the Big Picture mode in its menu to reach it.").Show();
        }

        /// <summary>
        /// Menu "Minimize": shows what is behind the mode (e.g. a game window that can't be found). The Xbox button or
        /// View + Menu bring the mode back.
        /// </summary>
        private void MinimizeBigPicture()
        {
            Window? window = Window.GetWindow(View);

            if (window == null)
                return;

            FnLog.AddInfo(GetApp(), "Big Picture mode minimized");
            window.WindowState = WindowState.Minimized;
        }
        #endregion
    }
}
