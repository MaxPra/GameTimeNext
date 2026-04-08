using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.General.AppSearch.ViewModels
{
    public class AppSearchViewModel : UIXViewModelBase
    {
        public ObservableCollection<SearchableApplication> SearchableApplications { get; set; }
        private SearchableApplication _searchableApplication;

        public SearchableApplication SelectedSearchableApplication
        {
            get => _searchableApplication;
            set => SetProperty(ref _searchableApplication, value);
        }

        public AppSearchViewModel()
        {
            SearchableApplications = new ObservableCollection<SearchableApplication>();
        }
    }
}
