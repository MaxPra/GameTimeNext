using GameTimeNext.Core.Application.General.UserSettings;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.FavAppsReorder.ViewModels
{
    public class FavAppsReorderViewModel : UIXViewModelBase
    {
        public ObservableCollection<FavoriteApplication> FavoriteApplications { get; set; }
        private FavoriteApplication _selectedApplication;

        public FavoriteApplication SelectedApplication
        {
            get => _selectedApplication;
            set => SetProperty(ref _selectedApplication, value);
        }

        public FavAppsReorderViewModel()
        {
            FavoriteApplications = new ObservableCollection<FavoriteApplication>();
        }
    }
}
