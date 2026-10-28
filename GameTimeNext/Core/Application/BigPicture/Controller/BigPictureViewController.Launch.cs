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

        // A launched game whose window opens behind the mode is brought to the front for this long after its window
        // appeared (slow computers show it long after the process started); without any window, it is given up after
        // LaunchFrontWindowTimeout
        private static readonly TimeSpan LaunchFrontTimeout = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan LaunchFrontWindowTimeout = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan LaunchFrontInterval = TimeSpan.FromSeconds(1);

        // Incremented to stop a running BringLaunchedGameToFront (return to the mode, end of the game)
        private int _launchFrontVersion;

        // While a game is launched the mode stays on top of all windows (launch cover), so the windows that come up
        // before the game (launcher, its progress dialogs, splash or console windows) don't pop up in front of it.
        // Lifted as soon as the game has a window, at the latest after the timeout.
        private static readonly TimeSpan LaunchCoverTimeout = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan LaunchCoverInterval = TimeSpan.FromMilliseconds(250);

        // A window of another application that keeps the foreground this long during the launch probably waits for
        // input (e.g. a cloud save conflict or the login of the launcher): the cover is lifted for it
        private static readonly TimeSpan LaunchCoverDialogDelay = TimeSpan.FromSeconds(4);

        // Incremented to stop a running CoverLaunch
        private int _launchCoverVersion;

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
            CoverLaunch(t1profi);

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

            DateTime end = DateTime.Now + LaunchFrontWindowTimeout;
            bool hasWindow = false;

            while (DateTime.Now < end && version == _launchFrontVersion && _runningPfid == pfid && GetApp().IsOpen)
            {
                // Reads the processes of the system, not on the UI thread
                IntPtr gameWindow = await Task.Run(() => FnLauncher.FindGameWindow(t1profi));

                if (version != _launchFrontVersion)
                    return;

                if (gameWindow != IntPtr.Zero && !hasWindow)
                {
                    hasWindow = true;
                    end = DateTime.Now + LaunchFrontTimeout;
                }

                Window? window = Window.GetWindow(View);

                if (gameWindow != IntPtr.Zero && window != null && FnForeground.IsForeground(gameWindow) && FnForeground.IsVisibleOnTop(window))
                {
                    // The game has the focus but lies behind the mode (seen with Source games switching to full screen):
                    // only its place in the z-order is wrong
                    FnForeground.Raise(gameWindow);

                    if (FnForeground.IsVisibleOnTop(window))
                    {
                        // Still covered: the mode goes behind it instead (the Xbox button brings it back as usual)
                        FnLog.AddInfo(GetApp(), $"Window of {t1profi.GANA} has the focus but stays behind the Big Picture mode (topmost: {window.Topmost}), the mode is moved behind it");
                        window.Topmost = false;
                        FnForeground.Lower(window);
                    }
                    else
                    {
                        FnLog.AddInfo(GetApp(), $"Window of {t1profi.GANA} had the focus behind the Big Picture mode, raised in front of it");
                    }
                }
                else if (gameWindow != IntPtr.Zero && !FnForeground.IsForeground(gameWindow)
                    && window != null && (window.IsActive || FnForeground.IsVisibleOnTop(window)))
                {
                    bool isForeground = FnForeground.BringToFront(gameWindow);
                    FnLog.AddInfo(GetApp(), isForeground
                        ? $"Window of {t1profi.GANA} brought in front of the Big Picture mode"
                        : $"Window of {t1profi.GANA} could not be brought in front of the Big Picture mode, foreground: {FnForeground.DescribeForegroundWindow()}");
                }

                await Task.Delay(LaunchFrontInterval);
            }

            if (version == _launchFrontVersion && _runningPfid == pfid && !hasWindow)
                FnLog.AddInfo(GetApp(), $"No window of {t1profi.GANA} found within {LaunchFrontWindowTimeout.TotalMinutes} min to bring in front of the Big Picture mode");
        }

        /// <summary>
        /// Keeps the mode with its launch card on top of all windows until the game has a window, then brings the game
        /// to the front: the launch goes straight from the mode to the game. A dialog of another application that
        /// keeps the foreground (see LaunchCoverDialogDelay) and the timeout lift the cover too.
        /// </summary>
        private async void CoverLaunch(T1PROFI t1profi)
        {
            int version = ++_launchCoverVersion;
            Window? window = Window.GetWindow(View);

            if (window == null)
                return;

            window.Topmost = true;

            DateTime end = DateTime.Now + LaunchCoverTimeout;
            IntPtr dialog = IntPtr.Zero;
            DateTime dialogSince = DateTime.MinValue;

            while (true)
            {
                await Task.Delay(LaunchCoverInterval);

                if (version != _launchCoverVersion)
                    return;

                // Reads the processes of the system, not on the UI thread
                IntPtr gameWindow = await Task.Run(() => FnLauncher.FindGameWindow(t1profi));

                if (version != _launchCoverVersion)
                    return;

                if (gameWindow != IntPtr.Zero)
                {
                    LiftLaunchCover();

                    bool isForeground = FnForeground.BringToFront(gameWindow);
                    FnLog.AddInfo(GetApp(), isForeground
                        ? $"Launch cover of the Big Picture mode lifted, window of {t1profi.GANA} brought to the front"
                        : $"Launch cover of the Big Picture mode lifted, window of {t1profi.GANA} could not be brought to the front, foreground: {FnForeground.DescribeForegroundWindow()}");
                    return;
                }

                IntPtr foreground = FnForeground.GetForeignForegroundWindow();

                if (foreground == IntPtr.Zero || foreground != dialog)
                {
                    dialog = foreground;
                    dialogSince = DateTime.Now;
                }
                else if (DateTime.Now - dialogSince >= LaunchCoverDialogDelay)
                {
                    FnLog.AddInfo(GetApp(), $"Launch cover of the Big Picture mode lifted for a window of another application while launching {t1profi.GANA}");
                    LiftLaunchCover();
                    FnForeground.Raise(foreground);
                    return;
                }

                if (DateTime.Now >= end)
                {
                    FnLog.AddInfo(GetApp(), $"Launch cover of the Big Picture mode lifted: no window of {t1profi.GANA} after {LaunchCoverTimeout.TotalSeconds} s");
                    LiftLaunchCover();
                    FnForeground.Raise(foreground);
                    return;
                }
            }
        }

        private void LiftLaunchCover()
        {
            _launchCoverVersion++;

            if (Window.GetWindow(View) is Window window)
                window.Topmost = false;
        }

        private void ResetLaunchState()
        {
            LiftLaunchCover();

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
