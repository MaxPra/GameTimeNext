using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesFilterViewModel : GTNViewModelBase
    {
        private bool _showArchived;
        private bool _onlyRecentlyPlayed;

        /// <summary>
        /// All tags incl. the "(No tags)" entry at the top
        /// </summary>
        public ObservableCollection<ProfilesFilterItem> Tags { get; set; }
        public ObservableCollection<ProfilesFilterItem> Progress { get; set; }
        public ObservableCollection<ProfilesFilterItem> Availability { get; set; }
        public ObservableCollection<ProfilesFilterItem> Source { get; set; }

        public bool ShowArchived
        {
            get => _showArchived;
            set => SetProperty(ref _showArchived, value);
        }

        public bool OnlyRecentlyPlayed
        {
            get => _onlyRecentlyPlayed;
            set => SetProperty(ref _onlyRecentlyPlayed, value);
        }

        public ProfilesFilterViewModel()
        {
            Tags = new ObservableCollection<ProfilesFilterItem>();
            Progress = new ObservableCollection<ProfilesFilterItem>();
            Availability = new ObservableCollection<ProfilesFilterItem>();
            Source = new ObservableCollection<ProfilesFilterItem>();
        }
    }
}
