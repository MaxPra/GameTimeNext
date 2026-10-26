using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.LauncherIntegration;
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
