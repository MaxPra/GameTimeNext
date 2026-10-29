using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles.Types;
using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Application.Profiles.Views;
using GameTimeNext.Core.Application.TableObjects;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Application.Profiles.Controller.ProfilesViewController;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesFilterViewController : UIXViewControllerBase<ProfilesApp, ProfilesFilterView>
    {
        private ProfilesFilterViewModel _profilesFilterViewModel = new ProfilesFilterViewModel();

        public ProfilesFilterViewModel ProfilesFilterViewModel { get => _profilesFilterViewModel; }

        public ProfilesFilterViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfileFilterViewReturn : UIXViewReturn
        {
            public bool Applied { get; set; } = false;
            public FilterCache Filter { get; set; } = new FilterCache();
        }

        protected override void Init()
        {
            ViewReturn = new ProfileFilterViewReturn();
        }

        protected override void BuildImpl()
        {

        }

        protected override void BuildFirstImpl()
        {
            // Build the view model from the currently applied filter
            BuildViewModel(GetApp().FilterCache);
        }

        protected override void CheckImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        /// <summary>
        /// Button Apply
        /// </summary>
        protected void EV_BtnApply()
        {
            GetViewReturn<ProfileFilterViewReturn>().Filter = BuildFilterFromViewModel();
            GetViewReturn<ProfileFilterViewReturn>().Applied = true;

            Exit(true);
        }

        /// <summary>
        /// Button Cancel
        /// </summary>
        protected void EV_BtnCancel()
        {
            GetViewReturn<ProfileFilterViewReturn>().Applied = false;

            Exit(true);
        }

        /// <summary>
        /// Button Reset (everything checked = whole library)
        /// </summary>
        protected void EV_BtnReset()
        {
            GetView().TxtSearchTag.Text = string.Empty;
            BuildViewModel(new FilterCache());
        }

        /// <summary>
        /// Checks all tags matching the current search
        /// </summary>
        protected void EV_BtnSelectAllTags()
        {
            SetVisibleTagsChecked(true);
        }

        /// <summary>
        /// Unchecks all tags matching the current search
        /// </summary>
        protected void EV_BtnSelectNoTags()
        {
            SetVisibleTagsChecked(false);
        }

        protected void EV_TxtSearchTag()
        {
            string searchText = GetView().TxtSearchTag.Text;

            // Only hide entries, so changes that were not applied yet are kept
            foreach (ProfilesFilterItem item in _profilesFilterViewModel.Tags)
            {
                item.IsVisible = FnString.IsNullEmptyOrWhitespace(searchText)
                    || item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void SetVisibleTagsChecked(bool isChecked)
        {
            foreach (ProfilesFilterItem item in _profilesFilterViewModel.Tags.Where(t => t.IsVisible))
                item.IsChecked = isChecked;
        }

        /// <summary>
        /// Builds the view model. Everything that is not explicitly excluded in the filter is checked.
        /// </summary>
        private void BuildViewModel(FilterCache filter)
        {
            HashSet<long> excludedTagIds = filter.ExcludedTagIds.ToHashSet();
            HashSet<string> excludedOptions = filter.ExcludedOptions.ToHashSet();

            List<T1GROUP> tags = new TXGROUP().ReadAll()
                .Where(g => g.GTYP == GroupType.Tag)
                .ToList();

            _profilesFilterViewModel = new ProfilesFilterViewModel();

            // Tags ("Untagged games" first, so games without tags can be filtered as well)
            List<ProfilesFilterItem> tagItems = new List<ProfilesFilterItem>
            {
                new ProfilesFilterItem
                {
                    Name = "Untagged games",
                    IsSpecial = true,
                    ToolTip = "Games without any tag",
                    IsChecked = !filter.ExcludeUntagged
                }
            };

            tagItems.AddRange(tags.Select(t => new ProfilesFilterItem
            {
                TagId = t.GRID,
                Name = t.GRNA,
                IsChecked = !excludedTagIds.Contains(t.GRID)
            }));

            _profilesFilterViewModel.Tags = new ObservableCollection<ProfilesFilterItem>(tagItems);
            _profilesFilterViewModel.Progress = BuildOptionItems(ProfilesFilterOptions.Progress, excludedOptions);
            _profilesFilterViewModel.Availability = BuildOptionItems(ProfilesFilterOptions.Availability, excludedOptions);
            _profilesFilterViewModel.Source = BuildOptionItems(ProfilesFilterOptions.Source, excludedOptions);
            _profilesFilterViewModel.ShowArchived = filter.ShowArchived;
            _profilesFilterViewModel.OnlyRecentlyPlayed = filter.OnlyRecentlyPlayed;

            View.DataContext = _profilesFilterViewModel;

            // Re-apply a search that might still be in the textbox
            EV_TxtSearchTag();
        }

        private static ObservableCollection<ProfilesFilterItem> BuildOptionItems(IReadOnlyList<(string Key, string Name)> options, HashSet<string> excludedOptions)
        {
            return new ObservableCollection<ProfilesFilterItem>(options.Select(o => new ProfilesFilterItem
            {
                OptionKey = o.Key,
                Name = o.Name,
                IsChecked = !excludedOptions.Contains(o.Key)
            }));
        }

        /// <summary>
        /// Builds the filter from the view model. Only unchecked entries are stored,
        /// so tags created later are visible by default.
        /// </summary>
        private FilterCache BuildFilterFromViewModel()
        {
            FilterCache filter = new FilterCache();

            filter.ExcludeUntagged = _profilesFilterViewModel.Tags.Any(t => t.TagId == 0 && !t.IsChecked);

            filter.ExcludedTagIds = _profilesFilterViewModel.Tags
                .Where(t => t.TagId != 0 && !t.IsChecked)
                .Select(t => t.TagId)
                .ToList();

            filter.ExcludedOptions = _profilesFilterViewModel.Progress
                .Concat(_profilesFilterViewModel.Availability)
                .Concat(_profilesFilterViewModel.Source)
                .Where(o => !o.IsChecked)
                .Select(o => o.OptionKey)
                .ToList();

            filter.ShowArchived = _profilesFilterViewModel.ShowArchived;
            filter.OnlyRecentlyPlayed = _profilesFilterViewModel.OnlyRecentlyPlayed;

            return filter;
        }

    }
}
