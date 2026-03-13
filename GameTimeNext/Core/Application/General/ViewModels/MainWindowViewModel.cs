using GameTimeNext.Core.Framework;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.ViewModels
{
    public class MainWindowViewModel : UIXViewModelBase
    {
        private ObservableCollection<SearchableApplication> _availableApplications;
        private SearchableApplication _selectedApplication;

        public ObservableCollection<SearchableApplication> AvailableApplications
        {
            get => _availableApplications;
            set
            {
                _availableApplications = value;
                OnPropertyChanged(nameof(AvailableApplications));
            }
        }

        public SearchableApplication SelectedApplication
        {
            get => _selectedApplication;
            set
            {
                _selectedApplication = value;
                OnPropertyChanged(nameof(SelectedApplication));
            }
        }

        public MainWindowViewModel()
        {
            AvailableApplications = new ObservableCollection<SearchableApplication>();
        }
    }
}