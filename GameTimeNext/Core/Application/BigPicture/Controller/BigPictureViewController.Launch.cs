using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Launching and quitting games.
    /// </summary>
    public partial class BigPictureViewController
    {
        private const string QuittingTag = "Quitting";

        // A launched game whose window opens behind the mode is brought to the front for this long
        private static readonly TimeSpan LaunchFrontTimeout = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan LaunchFrontInterval = TimeSpan.FromSeconds(1);

        // Incremented to stop a running BringLaunchedGameToFront (return to the mode, end of the game)
        private int _launchFrontVersion;

        private long _runningPfid;
        private T1PROFI? _launchingProfile;
        private bool _isQuitting;
        private bool _isLaunchOverlayVisible;

        /// <summary>
        /// A on a game: starts it, or asks to quit it while it is running.
        /// </summary>
        private async Task ActivateGameAsync(BigPictureGameTile game)
        {
            T1PROFI t1profi = game.Profile;

            if (!GetGameAction(t1profi).Enabled)
                return;

            if (t1profi.PFID == _runningPfid)
            {
                SwitchToRunningGame();
                return;
            }

            _launchingProfile = t1profi;
            ShowLaunchOverlay(t1profi.GANA, null);
            UpdateRunningState();

            try
            {
                // Not installed or cancelled meanwhile; otherwise the running game detection reports the start (EXEV_BigPictureGameLaunched)
                if (!await FnLauncher.StartGameAsync(t1profi, GetApp()) && _launchingProfile == t1profi)
                    ResetLaunchState();
            }
            catch (Exception ex)
            {
                FnLog.AddError(GetApp(), $"Launching {t1profi.GANA} from the Big Picture mode failed", ex);
                ResetLaunchState();
            }
        }

        /// <summary>
        /// While the launch / quit card is shown, only cancelling the launch is possible.
        /// </summary>
        private void HandleLaunchOverlayInput(BigPictureInput input)
        {
            if (input != BigPictureInput.Back || _launchingProfile == null)
                return;

            FnLauncher.CancelLaunch(_launchingProfile);
            ResetLaunchState();
        }

        private void QuitRunningGame()
        {
            T1PROFI? t1profi = new TXPROFI().Read(_runningPfid);

            if (t1profi == null)
                return;

            _isQuitting = true;
            ShowLaunchOverlay(t1profi.GANA, QuittingTag);
            UpdateRunningState();

            // The game keeps running (e.g. elevated): back to the running state; otherwise the running game detection
            // reports the end (EXEV_BigPictureGameClosed)
            if (!FnLauncher.QuitGame(t1profi))
            {
                _isQuitting = false;
                HideLaunchOverlay();
                UpdateRunningState();
            }
        }

        /// <summary>
        /// Windows doesn't always let a launched game take the focus from the mode (mostly games that start in a window):
        /// its window opens behind the mode and can't be reached with the controller. For a while after the start, the
        /// game window is brought to the front whenever the mode is in front instead of it; this also covers a splash
        /// or launcher window of the game that is followed by the real one. A launcher dialog in front of the mode
        /// (e.g. a cloud save conflict) keeps its focus.
        /// </summary>
        private async void BringLaunchedGameToFront(long pfid)
        {
            int version = ++_launchFrontVersion;

            T1PROFI? t1profi = new TXPROFI().Read(pfid);

            if (t1profi == null)
                return;

            DateTime end = DateTime.Now + LaunchFrontTimeout;

            while (DateTime.Now < end && version == _launchFrontVersion && _runningPfid == pfid && GetApp().IsOpen)
            {
                IntPtr gameWindow = FnLauncher.FindGameWindow(t1profi);
                Window? window = Window.GetWindow(View);

                if (gameWindow != IntPtr.Zero && !FnForeground.IsForeground(gameWindow)
                    && window != null && (window.IsActive || FnForeground.IsVisibleOnTop(window)))
                {
                    FnLog.AddInfo(GetApp(), $"Window of {t1profi.GANA} brought in front of the Big Picture mode");
                    FnForeground.BringToFront(gameWindow);
                }

                await Task.Delay(LaunchFrontInterval);
            }
        }

        private void ResetLaunchState()
        {
            _launchingProfile = null;
            _isQuitting = false;

            HideLaunchOverlay();
            UpdateRunningState();
        }

        /// <summary>
        /// The card of the profiles view (GameStartOverlayHostStyle): Content = game name, Tag = "Quitting" while quitting.
        /// </summary>
        private void ShowLaunchOverlay(string gameName, string? tag)
        {
            GetView().ccLaunchOverlay.Content = gameName;
            GetView().ccLaunchOverlay.Tag = tag;
            GetView().ccLaunchOverlay.Visibility = Visibility.Visible;

            _isLaunchOverlayVisible = true;
        }

        private void HideLaunchOverlay()
        {
            GetView().ccLaunchOverlay.Visibility = Visibility.Collapsed;
            GetView().ccLaunchOverlay.Content = null;
            GetView().ccLaunchOverlay.Tag = null;

            _isLaunchOverlayVisible = false;
        }
    }
}
