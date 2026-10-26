using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.Utils;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Library, pivots, the row of covers and the large details of the selected game.
    /// </summary>
    public partial class BigPictureViewController
    {
        // LT / RT outside of the library: covers skipped
        private const int JumpSize = 5;

        // Covers by file path, kept across reloads (after a game, a filter change) so they are only decoded once
        private readonly ConcurrentDictionary<string, BitmapImage?> _coverCache = new ConcurrentDictionary<string, BitmapImage?>();

        // Whether a game can really be started (scans its game folder), checked on its first use per load
        private readonly Dictionary<long, bool> _canLaunchCache = new Dictionary<long, bool>();

        // Filtered library, last played first; the pivots are built from it without reading the database again
        private List<LibraryEntry> _library = new List<LibraryEntry>();
        private bool _isFilterActive;

        private List<BigPicturePivot> _pivots = new List<BigPicturePivot>();
        private BigPicturePivotKind _pivot = BigPicturePivotKind.Home;

        // Pivot to return to from the search
        private BigPicturePivotKind _lastBrowsePivot = BigPicturePivotKind.Home;

        // Selected game per pivot, restored when the pivot is shown again
        private readonly Dictionary<BigPicturePivotKind, long> _selectedPfids = new Dictionary<BigPicturePivotKind, long>();

        private List<BigPictureGameTile> _games = new List<BigPictureGameTile>();
        private int _gameIndex;
        private long _detailsPfid;

        private record LibraryEntry(T1PROFI Profile, BitmapImage? Cover);

        /// <summary>
        /// What A does with a game: play, quit while it runs, or nothing (Status tells why).
        /// </summary>
        private record GameAction(string Text, string Glyph, bool Enabled, bool IsQuit, string Status);

        #region Loading
        /// <summary>
        /// Reads the library with the filter of the mode and builds the current pivot again (the selection is kept).
        /// </summary>
        private async Task LoadLibraryAsync(bool showLoader = true)
        {
            if (showLoader)
                GetApp().Loader.Begin("Loading your library...");

            try
            {
                FilterCache filter = _filter;
                (List<LibraryEntry> library, bool filterActive) = await Task.Run(() => ReadLibrary(filter));

                _library = library;
                _isFilterActive = filterActive;
                _canLaunchCache.Clear();
                _detailsPfid = 0;

                FnControls.SetVisible(GetView().pnlFilterChip, _isFilterActive);

                RebuildRow(animate: false);
            }
            catch (Exception ex)
            {
                FnLog.AddError(GetApp(), "Error while loading the library of the Big Picture mode", ex);
            }
            finally
            {
                if (showLoader)
                    GetApp().Loader.Stop();
            }
        }

        private (List<LibraryEntry> Library, bool FilterActive) ReadLibrary(FilterCache filter)
        {
            (List<T1PROFI> profiles, bool filterActive) = TFPROFI.GetFilteredProfiles(filter, string.Empty);

            return (profiles.Select(p => new LibraryEntry(p, LoadCover(p))).ToList(), filterActive);
        }

        private BitmapImage? LoadCover(T1PROFI t1profi)
        {
            if (FnString.IsNullEmptyOrWhitespace(t1profi.PPFN))
                return null;

            string coverPath = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, t1profi.PPFN);

            return _coverCache.GetOrAdd(coverPath, path => File.Exists(path) ? FnImage.LoadImageWithoutLock(path, 300, 450) : null);
        }
        #endregion

        #region Pivots
        /// <summary>
        /// Back to home (when the mode is opened); the given game is selected there if it is in it.
        /// </summary>
        private void ResetPivots(long pfidToSelect)
        {
            _pivots = new List<BigPicturePivot>
            {
                new BigPicturePivot(BigPicturePivotKind.Home, "Home"),
                new BigPicturePivot(BigPicturePivotKind.Library, "Library"),
                new BigPicturePivot(BigPicturePivotKind.Search, "Search")
            };

            GetView().itcPivots.ItemsSource = _pivots;

            _selectedPfids.Clear();
            _selectedPfids[BigPicturePivotKind.Home] = pfidToSelect;

            _pivot = BigPicturePivotKind.Home;
            _lastBrowsePivot = BigPicturePivotKind.Home;
            _games = new List<BigPictureGameTile>();
            _gameIndex = 0;

            UpdatePivotHeaders();
        }

        private void ShowAdjacentPivot(int delta)
        {
            int index = _pivots.FindIndex(p => p.Kind == _pivot);
            int next = (index + delta + _pivots.Count) % _pivots.Count;

            ShowPivot(_pivots[next].Kind);
        }

        private void ShowPivot(BigPicturePivotKind pivot)
        {
            if (pivot == _pivot)
                return;

            if (_pivot != BigPicturePivotKind.Search)
                _lastBrowsePivot = _pivot;

            _pivot = pivot;
            UpdatePivotHeaders();

            // The search starts on its keyboard, the other pivots in their row
            if (_pivot == BigPicturePivotKind.Search)
                FocusKeyboard();
            else
                FocusRow();

            RebuildRow(animate: true);
        }

        private void UpdatePivotHeaders()
        {
            foreach (BigPicturePivot pivot in _pivots)
                pivot.IsSelected = pivot.Kind == _pivot;
        }
        #endregion

        #region Row
        /// <summary>
        /// Builds the covers of the current pivot: home the played games by last played, library all games by name,
        /// search the matches. Keeps the selected game of the pivot.
        /// </summary>
        private void RebuildRow(bool animate)
        {
            List<LibraryEntry> entries;
            string title;

            switch (_pivot)
            {
                case BigPicturePivotKind.Library:
                    entries = _library.OrderBy(e => e.Profile.GANA, StringComparer.CurrentCultureIgnoreCase).ToList();
                    title = string.Format(CultureInfo.CurrentCulture, "All games  ·  {0}", entries.Count);
                    break;

                case BigPicturePivotKind.Search:
                    string searchText = GetSearchText();
                    entries = FindMatches(searchText);
                    title = GetResultsTitle(searchText, entries.Count);
                    break;

                default:
                    entries = _library.Where(e => e.Profile.LAPL != DateTime.MinValue).ToList();
                    title = "Recently played";

                    // Nothing played yet: the library instead
                    if (entries.Count == 0)
                    {
                        entries = _library.OrderBy(e => e.Profile.GANA, StringComparer.CurrentCultureIgnoreCase).ToList();
                        title = "Your games";
                    }
                    break;
            }

            _games = entries.Select(e => new BigPictureGameTile(e.Profile, e.Cover) { IsRunning = e.Profile.PFID == _runningPfid }).ToList();
            GetView().itcGames.ItemsSource = _games;
            GetView().txRowTitle.Text = title;

            UpdateEmptyMessage();

            _selectedPfids.TryGetValue(_pivot, out long pfid);
            int index = _games.FindIndex(g => g.Profile.PFID == pfid);

            _gameIndex = -1;
            SelectGame(Math.Max(index, 0), immediate: true);

            if (!animate)
                return;

            GetView().svGames.ScrollToHome();
            AnimatePivotChange();
        }

        /// <summary>
        /// Slides the covers and details of the new pivot in from the right.
        /// </summary>
        private void AnimatePivotChange()
        {
            Duration duration = TimeSpan.FromMilliseconds(240);
            IEasingFunction easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            foreach (FrameworkElement element in new FrameworkElement[] { GetView().pnlRow, GetView().pnlContent })
            {
                if (element.RenderTransform is TranslateTransform translate)
                    translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(60, 0, duration) { EasingFunction = easing });

                element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = easing });
            }
        }

        private void UpdateEmptyMessage()
        {
            bool isEmpty = _library.Count == 0 && _pivot != BigPicturePivotKind.Search;

            FnControls.SetVisible(GetView().pnlEmpty, isEmpty);

            if (!isEmpty)
                return;

            if (_isFilterActive)
            {
                GetView().txEmptyTitle.Text = "No games match the filter";
                GetView().txEmptyHint.Text = "Press X to change the filter.";
            }
            else
            {
                GetView().txEmptyTitle.Text = "Your library is empty";
                GetView().txEmptyHint.Text = "Exit Big Picture and add your games in Profiles.";
            }
        }
        #endregion

        #region Selection
        private BigPictureGameTile? GetSelectedGame()
        {
            return _gameIndex >= 0 && _gameIndex < _games.Count ? _games[_gameIndex] : null;
        }

        /// <summary>
        /// Selects the cover at once; its details follow when the selection rests (immediate: right away, e.g. after a
        /// rebuild), so holding a direction stays smooth.
        /// </summary>
        private void SelectGame(int index, bool immediate = false)
        {
            if (_games.Count == 0)
            {
                _gameIndex = -1;
                UpdateDetails();
                UpdateHints();
                return;
            }

            index = Math.Clamp(index, 0, _games.Count - 1);

            if (GetSelectedGame() is BigPictureGameTile previous)
                previous.IsSelected = false;

            _gameIndex = index;
            BigPictureGameTile game = _games[index];

            // In the search the cover is only highlighted while the results have the input
            game.IsSelected = !IsKeyboardInput();
            _selectedPfids[_pivot] = game.Profile.PFID;

            _detailsTimer.Stop();

            if (immediate)
            {
                UpdateDetails();
                UpdateHints();
            }
            else
            {
                _detailsTimer.Start();
            }
        }

        /// <summary>
        /// LT / RT: in the library to the previous / next starting letter, elsewhere a few covers.
        /// </summary>
        private void JumpGames(int direction)
        {
            if (_games.Count == 0)
                return;

            if (_pivot != BigPicturePivotKind.Library)
            {
                SelectGame(_gameIndex + direction * JumpSize);
                return;
            }

            string letter = GetLetter(_games[_gameIndex]);
            int index = _gameIndex;

            if (direction > 0)
            {
                while (index < _games.Count - 1 && GetLetter(_games[index]) == letter)
                    index++;
            }
            else
            {
                // To the start of the letter, from there to the start of the previous one
                if (index > 0 && GetLetter(_games[index - 1]) != letter)
                    letter = GetLetter(_games[--index]);

                while (index > 0 && GetLetter(_games[index - 1]) == letter)
                    index--;
            }

            SelectGame(index);
        }

        private static string GetLetter(BigPictureGameTile game)
        {
            return game.Name.Length == 0 ? string.Empty : game.Name.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Mouse: the first click selects a cover, a click on the selected cover starts the game.
        /// </summary>
        private void ClickGame(BigPictureGameTile game)
        {
            int index = _games.IndexOf(game);

            if (index < 0)
                return;

            if (index == _gameIndex && !IsKeyboardInput())
            {
                _ = ActivateGameAsync(game);
                return;
            }

            FocusRow();
            SelectGame(index);
        }
        #endregion

        #region Details
        /// <summary>
        /// The selected game large below the row: name, platform / launcher / last played, the times, the current
        /// playthrough and what A does. Hidden while the keyboard of the search has the input.
        /// </summary>
        private void UpdateDetails()
        {
            BigPictureGameTile? game = GetSelectedGame();

            FnControls.SetVisible(GetView().pnlDetails, game != null && !IsKeyboardInput());

            if (game == null)
                return;

            T1PROFI t1profi = game.Profile;

            // Backdrop and accent only for another game (no flicker, no needless theme switch)
            if (t1profi.PFID != _detailsPfid)
            {
                _detailsPfid = t1profi.PFID;

                GetView().pnlDetails.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(160))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });

                GetView().imgBackdrop.Source = game.Cover;
                GetView().imgKeyArt.Source = game.Cover;
                FnTheme.ApplyProfileAccentColors(t1profi);

                GetView().txTitle.Text = t1profi.GANA;
                GetView().txSubtitle.Text = BuildSubtitle(t1profi);
                GetView().txTotalPlaytime.Text = FnTime.FormatMinutesAsHoursAndMinutes(TFPROFI.GetTotalGameTimeInMinutes(t1profi.PFID), FnTime.NoValue);
                GetView().txLastSession.Text = FnTime.FormatMinutesAsHoursAndMinutes(TFSESSI.GetLastSessionGameTime(t1profi.PFID), FnTime.NoValue);
                GetView().txPlayedToday.Text = FnTime.FormatMinutesAsHoursAndMinutes(TFPROFI.GetTodaysGameTimeInMinutes(t1profi.PFID));

                FillPlaythrough(t1profi);
            }

            FillAction(t1profi);
        }

        private static string BuildSubtitle(T1PROFI t1profi)
        {
            List<string> parts = new List<string>();

            string platform = TFCTABD.GetDescription("pF", t1profi.PLAFO);
            if (!FnString.IsNullEmptyOrWhitespace(platform))
                parts.Add(platform);

            if (FnLauncher.IsLinked(t1profi))
                parts.Add(FnLauncher.GetLauncherName(t1profi.LNTY));

            string lastPlayed = CFBigPictureApp.FormatLastPlayed(t1profi.LAPL);
            parts.Add(FnString.IsNullEmptyOrWhitespace(lastPlayed) ? "Not played yet" : lastPlayed);

            return string.Join("   ·   ", parts);
        }

        /// <summary>
        /// Current playthrough, only if it is not the initial one (like the profiles view), with its progress against
        /// the estimate chosen in the profile.
        /// </summary>
        private void FillPlaythrough(T1PROFI t1profi)
        {
            T1PLTHR? t1plthr = TFPLTHR.HasCurrentPlaythrough(t1profi.PFID) ? TFPLTHR.GetCurrentPlaythrough(t1profi.PFID) : null;
            bool hasPlaythrough = t1plthr != null && t1plthr.PTTY != PlaythroughType.INITIAL_PLAYTHROUGH;

            FnControls.SetVisible(GetView().pnlPlaythrough, hasPlaythrough);

            if (!hasPlaythrough)
                return;

            GetView().txPlaythrough.Text = t1plthr!.PTDE;
            GetView().txPlaythroughTime.Text = FnTime.FormatMinutesAsHoursAndMinutes(TFPLTHR.GetPlaytimeInMinutes(t1plthr.PTID), FnTime.NoValue);

            double estimate = t1profi.ETTY switch
            {
                EstimatedTimeTypes.EST_TIME_MAIN => t1profi.ETMA,
                EstimatedTimeTypes.EST_TIME_MAIN_EXTRA => t1profi.ETME,
                EstimatedTimeTypes.EST_TIME_COMPLETIONIST => t1profi.ETCO,
                _ => 0
            };

            bool hasProgress = estimate > 0;
            FnControls.SetVisible(GetView().pnlProgress, hasProgress);

            if (!hasProgress)
                return;

            double progress = Math.Clamp(TFPLTHR.GetPlaytimeInMinutes(t1plthr.PTID) / estimate, 0, 1);

            GetView().txProgress.Text = string.Format(CultureInfo.CurrentCulture, "{0:0} %", progress * 100);

            // Relative scale instead of a fixed width, like the progress bar of the profiles view
            GetView().pnlProgressFill.RenderTransform = new ScaleTransform(progress, 1);
        }

        private void FillAction(T1PROFI t1profi)
        {
            GameAction action = GetGameAction(t1profi);

            GetView().txPlay.Text = action.Text;
            GetView().txPlayGlyph.Text = action.Glyph;
            GetView().btnPlay.IsEnabled = action.Enabled;
            GetView().btnPlay.Tag = action.IsQuit ? "Quit" : null;
            GetView().txStatus.Text = action.Status;

            FnControls.SetVisible(GetView().txStatus, !FnString.IsNullEmptyOrWhitespace(action.Status));
        }
        #endregion

        #region Game state
        private GameAction GetGameAction(T1PROFI t1profi)
        {
            if (t1profi.PFID == _runningPfid)
                return new GameAction(_isQuitting ? "Quitting..." : "Back to the game", "\uE768", !_isQuitting, false, "Playing now  ·  X for options");

            if (_runningPfid != 0)
                return new GameAction("Play", "\uE768", false, false, TFPROFI.GetProfileName(_runningPfid) + " is running.");

            if (_launchingProfile != null)
                return new GameAction("Launching...", "\uE768", false, false, string.Empty);

            if (!FnLauncher.IsLinked(t1profi))
                return new GameAction("Play", "\uE768", false, false, "Link this game with a launcher in its profile to start it from here.");

            if (!TFPROFI.HasExecutables(t1profi))
                return new GameAction("Play", "\uE768", false, false, "Select the executables of this game in its profile to start it from here.");

            if (!CanLaunch(t1profi))
                return new GameAction("Play", "\uE768", false, false, "Not installed: its executable was not found in the game folder.");

            return new GameAction("Play", "\uE768", true, false, string.Empty);
        }

        private bool CanLaunch(T1PROFI t1profi)
        {
            if (!_canLaunchCache.TryGetValue(t1profi.PFID, out bool canLaunch))
            {
                canLaunch = FnLauncher.CanLaunch(t1profi);
                _canLaunchCache[t1profi.PFID] = canLaunch;
            }

            return canLaunch;
        }

        /// <summary>
        /// A game was started, quit or is being launched: running badges, action of the selected game and hints.
        /// </summary>
        private void UpdateRunningState()
        {
            foreach (BigPictureGameTile game in _games)
                game.IsRunning = game.Profile.PFID == _runningPfid;

            if (GetSelectedGame() is BigPictureGameTile selected)
                FillAction(selected.Profile);

            UpdateHints();
        }
        #endregion
    }
}
