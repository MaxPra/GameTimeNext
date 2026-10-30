using GameTimeNext.Core.Framework.LauncherIntegration;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesLauncherImportViewModel : GTNViewModelBase
    {
        public ObservableCollection<LauncherGame> Games { get; set; }

        private LauncherGame? _selectedGame;

        public LauncherGame? SelectedGame
        {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
        }

        public ProfilesLauncherImportViewModel()
        {
            Games = new ObservableCollection<LauncherGame>();

        }
    }
}
