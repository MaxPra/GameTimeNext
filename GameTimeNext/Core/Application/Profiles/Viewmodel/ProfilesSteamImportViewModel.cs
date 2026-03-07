using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesSteamImportViewModel : GTNViewModelBase
    {
        public ObservableCollection<SteamGame> SteamGames { get; set; }

        private SteamGame _selectedSteamGame;

        public SteamGame SelectedSteamGame
        {
            get => _selectedSteamGame;
            set => SetProperty(ref _selectedSteamGame, value);
        }

        public ProfilesSteamImportViewModel()
        {
            SteamGames = new ObservableCollection<SteamGame>();

        }
    }
}
