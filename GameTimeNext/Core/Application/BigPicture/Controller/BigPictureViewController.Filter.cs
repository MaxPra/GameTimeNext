using GameTimeNext.Core.Application.BigPicture.ViewModels;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Globalization;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Application.BigPicture.Controller
{
    /// <summary>
    /// Filter of the mode: the options of the profiles filter as a list of switches in a drawer, made for the controller
    /// (up / down, A switches, X resets, B applies). Stored separately from the filter of the profiles view.
    /// </summary>
    public partial class BigPictureViewController
    {
        private FilterCache _filter = new FilterCache();

        private List<BigPictureFilterEntry> _filterEntries = new List<BigPictureFilterEntry>();
        private int _filterIndex;
        private bool _isFilterOpen;
        private bool _filterChanged;

        private void OpenFilterDrawer()
        {
            _isFilterOpen = true;
            _filterChanged = false;

            ShowFilterEntries(_filter);
            FnControls.SetVisible(GetView().grdFilter, true);
        }

        /// <summary>
        /// Closes the drawer; with apply a changed filter is stored and the library is read again.
        /// </summary>
        private void CloseFilterDrawer(bool apply)
        {
            FnControls.SetVisible(GetView().grdFilter, false);

            if (!_isFilterOpen)
                return;

            _isFilterOpen = false;

            if (!apply || !_filterChanged)
                return;

            _filter = BuildFilterFromEntries();

            AppEnvironment.GetAppConfig().BigPictureFilterCache = _filter;
            AppEnvironment.SaveAppConfig();

            _ = LoadLibraryAsync();
        }

        private void HandleFilterInput(BigPictureInput input)
        {
            switch (input)
            {
                case BigPictureInput.Up:
                    MoveFilterSelection(-1);
                    break;

                case BigPictureInput.Down:
                    MoveFilterSelection(1);
                    break;

                case BigPictureInput.Activate:
                    ToggleFilterEntry(_filterEntries[_filterIndex]);
                    break;

                case BigPictureInput.Options:
                case BigPictureInput.Filter:
                    // Reset: everything checked = whole library
                    ShowFilterEntries(new FilterCache());
                    _filterChanged = true;
                    break;

                case BigPictureInput.Back:
                case BigPictureInput.Menu:
                case BigPictureInput.Search:
                    CloseFilterDrawer(apply: true);
                    break;
            }
        }

        /// <summary>
        /// Mouse: selects and switches the entry.
        /// </summary>
        private void ClickFilterEntry(BigPictureFilterEntry entry)
        {
            int index = _filterEntries.IndexOf(entry);

            if (index < 0 || entry.IsHeader)
                return;

            SelectFilterEntry(index);
            ToggleFilterEntry(entry);
        }

        private void ToggleFilterEntry(BigPictureFilterEntry entry)
        {
            if (entry.IsHeader)
                return;

            entry.IsChecked = !entry.IsChecked;
            _filterChanged = true;
        }

        private void MoveFilterSelection(int delta)
        {
            int index = _filterIndex + delta;

            // Headers are skipped
            while (index >= 0 && index < _filterEntries.Count && _filterEntries[index].IsHeader)
                index += delta;

            if (index >= 0 && index < _filterEntries.Count)
                SelectFilterEntry(index);
        }

        private void SelectFilterEntry(int index)
        {
            _filterIndex = index;

            for (int i = 0; i < _filterEntries.Count; i++)
                _filterEntries[i].IsSelected = i == _filterIndex;
        }

        /// <summary>
        /// Builds the switches from a filter, grouped like the profiles filter. Everything that is not excluded is checked.
        /// </summary>
        private void ShowFilterEntries(FilterCache filter)
        {
            HashSet<string> excludedOptions = filter.ExcludedOptions.ToHashSet();
            HashSet<long> excludedTagIds = filter.ExcludedTagIds.ToHashSet();

            List<BigPictureFilterEntry> entries = new List<BigPictureFilterEntry>();

            AddOptionEntries(entries, "Progress", ProfilesFilterOptions.Progress, excludedOptions);
            AddOptionEntries(entries, "Availability", ProfilesFilterOptions.Availability, excludedOptions);
            AddOptionEntries(entries, "Source", ProfilesFilterOptions.Source, excludedOptions);

            entries.Add(new BigPictureFilterEntry(BigPictureFilterEntryKind.Header, "Tags"));
            entries.Add(new BigPictureFilterEntry(BigPictureFilterEntryKind.Untagged, "Untagged games", !filter.ExcludeUntagged));

            IEnumerable<T1GROUP> tags = new TXGROUP().ReadAll()
                .Where(g => g.GTYP == GroupType.Tag)
                .OrderBy(g => g.GRNA, StringComparer.CurrentCultureIgnoreCase);

            entries.AddRange(tags.Select(t => new BigPictureFilterEntry(BigPictureFilterEntryKind.Tag, t.GRNA, !excludedTagIds.Contains(t.GRID), tagId: t.GRID)));

            entries.Add(new BigPictureFilterEntry(BigPictureFilterEntryKind.Header, "More"));
            entries.Add(new BigPictureFilterEntry(BigPictureFilterEntryKind.ShowArchived, "Show archived games", filter.ShowArchived));
            entries.Add(new BigPictureFilterEntry(
                BigPictureFilterEntryKind.OnlyRecentlyPlayed,
                string.Format(CultureInfo.CurrentCulture, "Only played in the last {0} days", ProfilesFilterOptions.RECENTLY_PLAYED_DAYS),
                filter.OnlyRecentlyPlayed));

            _filterEntries = entries;
            GetView().itcFilter.ItemsSource = _filterEntries;

            SelectFilterEntry(_filterEntries.FindIndex(e => !e.IsHeader));
        }

        private static void AddOptionEntries(List<BigPictureFilterEntry> entries, string header, IReadOnlyList<(string Key, string Name)> options, HashSet<string> excludedOptions)
        {
            entries.Add(new BigPictureFilterEntry(BigPictureFilterEntryKind.Header, header));
            entries.AddRange(options.Select(o => new BigPictureFilterEntry(BigPictureFilterEntryKind.Option, o.Name, !excludedOptions.Contains(o.Key), optionKey: o.Key)));
        }

        /// <summary>
        /// Like the profiles filter, only unchecked entries are stored, so tags created later are shown by default.
        /// </summary>
        private FilterCache BuildFilterFromEntries()
        {
            return new FilterCache
            {
                ExcludedOptions = _filterEntries
                    .Where(e => e.Kind == BigPictureFilterEntryKind.Option && !e.IsChecked)
                    .Select(e => e.OptionKey)
                    .ToList(),
                ExcludedTagIds = _filterEntries
                    .Where(e => e.Kind == BigPictureFilterEntryKind.Tag && !e.IsChecked)
                    .Select(e => e.TagId)
                    .ToList(),
                ExcludeUntagged = _filterEntries.Any(e => e.Kind == BigPictureFilterEntryKind.Untagged && !e.IsChecked),
                ShowArchived = _filterEntries.Any(e => e.Kind == BigPictureFilterEntryKind.ShowArchived && e.IsChecked),
                OnlyRecentlyPlayed = _filterEntries.Any(e => e.Kind == BigPictureFilterEntryKind.OnlyRecentlyPlayed && e.IsChecked)
            };
        }
    }
}
