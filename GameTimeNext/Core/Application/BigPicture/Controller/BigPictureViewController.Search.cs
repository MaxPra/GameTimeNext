using GameTimeNext.Core.Application.BigPicture.ViewModels;
using System.Globalization;
using System.Windows.Input;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Search pivot: the matches are the row of covers, the on-screen keyboard takes the place of the details while it
    /// has the input. Up from the keyboard moves into the results, down / B from the results back to the keyboard.
    /// A real keyboard types into the search field.
    /// </summary>
    public partial class BigPictureViewController
    {
        // Keyboard grid of 10 columns
        private const double KeyColumnWidth = 88;
        private const double KeyRowHeight = 70;
        private const double KeyGap = 10;

        private List<BigPictureKeyTile> _keys = new List<BigPictureKeyTile>();
        private BigPictureKeyTile? _selectedKey;
        private bool _isKeyboardFocused;

        // Programmatic changes of the search field that must not rebuild the row
        private bool _suppressSearchUpdate;

        /// <summary>
        /// Input goes to the on-screen keyboard (search pivot, not moved into the results).
        /// </summary>
        private bool IsKeyboardInput()
        {
            return _pivot == BigPicturePivotKind.Search && _isKeyboardFocused;
        }

        #region Keyboard
        /// <summary>
        /// Digits, letters in the layout of the keyboard (QWERTZ for German, otherwise QWERTY), a few characters of game
        /// names and the function keys, placed on the canvas of the keyboard.
        /// </summary>
        private void BuildKeyboard()
        {
            string language = InputLanguageManager.Current.CurrentInputLanguage?.TwoLetterISOLanguageName
                ?? CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            bool isQwertz = language == "de";

            string[] characterRows =
            {
                "1234567890",
                isQwertz ? "QWERTZUIOP" : "QWERTYUIOP",
                "ASDFGHJKL'",
                isQwertz ? "YXCVBNM-:&" : "ZXCVBNM-:&"
            };

            _keys = new List<BigPictureKeyTile>();

            for (int row = 0; row < characterRows.Length; row++)
            {
                for (int column = 0; column < characterRows[row].Length; column++)
                {
                    string character = characterRows[row][column].ToString(CultureInfo.InvariantCulture);
                    AddKey(new BigPictureKeyTile(BigPictureKeyKind.Character, character, string.Empty), row, column, 1);
                }
            }

            int functionRow = characterRows.Length;

            AddKey(new BigPictureKeyTile(BigPictureKeyKind.Space, "Space", "\uE75D"), functionRow, 0, 4);
            AddKey(new BigPictureKeyTile(BigPictureKeyKind.Backspace, "Delete", "\uE750"), functionRow, 4, 2);
            AddKey(new BigPictureKeyTile(BigPictureKeyKind.Clear, "Clear", "\uE894"), functionRow, 6, 2);
            AddKey(new BigPictureKeyTile(BigPictureKeyKind.Done, "Results", "\uE70E"), functionRow, 8, 2);

            GetView().itcKeys.Width = 10 * KeyColumnWidth - KeyGap;
            GetView().itcKeys.Height = (functionRow + 1) * KeyRowHeight - KeyGap;
            GetView().itcKeys.ItemsSource = _keys;

            SelectKey(_keys.FirstOrDefault(k => k.Text == "Q"));
        }

        private void AddKey(BigPictureKeyTile key, int row, int column, int span)
        {
            key.X = column * KeyColumnWidth;
            key.Y = row * KeyRowHeight;
            key.Width = span * KeyColumnWidth - KeyGap;
            key.Height = KeyRowHeight - KeyGap;

            _keys.Add(key);
        }

        private void SelectKey(BigPictureKeyTile? key)
        {
            if (_selectedKey != null)
                _selectedKey.IsSelected = false;

            _selectedKey = key;

            if (_selectedKey != null)
                _selectedKey.IsSelected = _isKeyboardFocused;
        }

        /// <summary>
        /// Moves to the nearest key in the direction: keys overlapping the current one across the direction first.
        /// Returns false if there is no key in that direction.
        /// </summary>
        private bool MoveKey(int directionX, int directionY)
        {
            if (_selectedKey == null)
                return false;

            BigPictureKeyTile from = _selectedKey;
            BigPictureKeyTile? best = null;
            double bestScore = double.MaxValue;

            foreach (BigPictureKeyTile key in _keys)
            {
                if (ReferenceEquals(key, from))
                    continue;

                double distance = directionX > 0 ? key.X - from.Right
                    : directionX < 0 ? from.X - key.Right
                    : directionY > 0 ? key.Y - from.Bottom
                    : from.Y - key.Bottom;

                if (distance < -1)
                    continue;

                double crossGap = directionX != 0
                    ? Math.Max(0, Math.Max(key.Y - from.Bottom, from.Y - key.Bottom))
                    : Math.Max(0, Math.Max(key.X - from.Right, from.X - key.Right));

                double edgeOffset = directionX != 0 ? Math.Abs(key.Y - from.Y) : Math.Abs(key.X - from.X);
                double score = distance + crossGap * 4 + edgeOffset * 0.01;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = key;
                }
            }

            if (best == null)
                return false;

            SelectKey(best);
            return true;
        }

        /// <summary>
        /// The keyboard gets the input: it replaces the details, a real keyboard types into the field.
        /// </summary>
        private void FocusKeyboard()
        {
            _isKeyboardFocused = true;
            UpdateKeyboardPanel();
        }

        /// <summary>
        /// The row gets the input (in the search: the results), the details are shown.
        /// </summary>
        private void FocusRow()
        {
            _isKeyboardFocused = false;
            UpdateKeyboardPanel();
        }

        private void UpdateKeyboardPanel()
        {
            bool isKeyboard = IsKeyboardInput();

            FnControls.SetVisible(GetView().pnlKeyboard, isKeyboard);

            if (_selectedKey != null)
                _selectedKey.IsSelected = isKeyboard;

            if (GetSelectedGame() is BigPictureGameTile game)
                game.IsSelected = !isKeyboard;

            if (isKeyboard)
            {
                GetView().UpdateLayout();
                GetView().txbSearch.Focus();
                GetView().txbSearch.CaretIndex = GetView().txbSearch.Text.Length;
            }
            else
            {
                View.Focus();
            }

            UpdateDetails();
            UpdateHints();
        }
        #endregion

        #region Input
        private void HandleKeyboardInput(BigPictureInput input)
        {
            switch (input)
            {
                case BigPictureInput.Up:
                    // From the top row up into the results
                    if (!MoveKey(0, -1) && _games.Count > 0)
                        FocusRow();
                    break;

                case BigPictureInput.Down:
                    MoveKey(0, 1);
                    break;

                case BigPictureInput.Left:
                    MoveKey(-1, 0);
                    break;

                case BigPictureInput.Right:
                    MoveKey(1, 0);
                    break;

                case BigPictureInput.Activate:
                    if (_selectedKey != null)
                        PressKey(_selectedKey);
                    break;

                case BigPictureInput.Back:
                    // Deletes like Backspace; on an empty field it leaves the search
                    if (HasSearchText())
                        DeleteLastCharacter();
                    else
                        ShowPivot(_lastBrowsePivot);
                    break;

                case BigPictureInput.Options:
                    InsertText(" ");
                    break;

                case BigPictureInput.Search:
                    ShowPivot(_lastBrowsePivot);
                    break;

                case BigPictureInput.Menu:
                    OpenMainMenu();
                    break;
            }
        }

        /// <summary>
        /// Real keyboard while the on-screen keyboard has the input: arrows move, Enter / Down into the results,
        /// Esc / F3 leave the search, Page Up / Down switch the pivot; everything else is typed into the field (false).
        /// </summary>
        private bool HandleSearchKey(Key key)
        {
            if (key == Key.Enter)
            {
                if (_games.Count > 0)
                    FocusRow();

                return true;
            }

            BigPictureInput? input = key switch
            {
                Key.Up => BigPictureInput.Up,
                Key.Down => BigPictureInput.Down,
                Key.Left => BigPictureInput.Left,
                Key.Right => BigPictureInput.Right,
                Key.PageUp => BigPictureInput.PivotPrevious,
                Key.PageDown => BigPictureInput.PivotNext,
                Key.Escape or Key.F3 => BigPictureInput.Search,
                _ => null
            };

            if (input == null)
                return false;

            HandleInput(input.Value);
            return true;
        }

        private void PressKey(BigPictureKeyTile key)
        {
            switch (key.Kind)
            {
                case BigPictureKeyKind.Character:
                    InsertText(key.Text.ToLower(CultureInfo.CurrentCulture));
                    break;

                case BigPictureKeyKind.Space:
                    InsertText(" ");
                    break;

                case BigPictureKeyKind.Backspace:
                    DeleteLastCharacter();
                    break;

                case BigPictureKeyKind.Clear:
                    GetView().txbSearch.Text = string.Empty;
                    break;

                case BigPictureKeyKind.Done:
                    if (_games.Count > 0)
                        FocusRow();
                    break;
            }
        }
        #endregion

        #region Search text
        /// <summary>
        /// Games whose name contains the text; names starting with it first.
        /// </summary>
        private List<LibraryEntry> FindMatches(string searchText)
        {
            if (FnString.IsNullEmptyOrWhitespace(searchText))
                return new List<LibraryEntry>();

            return _library
                .Where(e => e.Profile.GANA.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(e => e.Profile.GANA.StartsWith(searchText, StringComparison.CurrentCultureIgnoreCase) ? 0 : 1)
                .ThenBy(e => e.Profile.GANA, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string GetResultsTitle(string searchText, int matchCount)
        {
            if (FnString.IsNullEmptyOrWhitespace(searchText))
                return "Type the name of a game";

            return matchCount == 0
                ? string.Format(CultureInfo.CurrentCulture, "No games found for \"{0}\"", searchText)
                : string.Format(CultureInfo.CurrentCulture, "Results for \"{0}\"  ·  {1}", searchText, matchCount);
        }

        /// <summary>
        /// Empty search when the mode is opened.
        /// </summary>
        private void ResetSearch()
        {
            _suppressSearchUpdate = true;
            GetView().txbSearch.Text = string.Empty;
            _suppressSearchUpdate = false;

            _isKeyboardFocused = false;
            FnControls.SetVisible(GetView().pnlKeyboard, false);
        }

        private void InsertText(string text)
        {
            GetView().txbSearch.Text += text;
            GetView().txbSearch.CaretIndex = GetView().txbSearch.Text.Length;
        }

        private void DeleteLastCharacter()
        {
            string text = GetView().txbSearch.Text;

            if (text.Length == 0)
                return;

            GetView().txbSearch.Text = text.Substring(0, text.Length - 1);
            GetView().txbSearch.CaretIndex = GetView().txbSearch.Text.Length;
        }

        private string GetSearchText()
        {
            return GetView().txbSearch.Text.Trim();
        }

        private bool HasSearchText()
        {
            return !FnString.IsNullEmptyOrWhitespace(GetView().txbSearch.Text);
        }

        /// <summary>
        /// The text of the search field changed: the results are built again.
        /// </summary>
        private void ApplySearch()
        {
            if (_suppressSearchUpdate || _pivot != BigPicturePivotKind.Search)
                return;

            RebuildRow(animate: false);
            UpdateHints();
        }
        #endregion
    }
}
