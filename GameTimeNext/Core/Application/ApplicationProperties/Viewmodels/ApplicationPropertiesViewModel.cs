using GameTimeNext.Core.Application.ApplicationProperties.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.ApplicationProperties.Viewmodels
{
    public class ApplicationPropertiesViewModel : UIXViewModelBase
    {
        public ObservableCollection<ApplicationPropertiesDataGridRow>? ApplicationPropertiesDataGridRows { get; set; }
        public ApplicationPropertiesDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private ApplicationPropertiesDataGridRow? _selectedRow;

        public ApplicationPropertiesViewModel()
        {
            ApplicationPropertiesDataGridRows = new ObservableCollection<ApplicationPropertiesDataGridRow>();
        }
    }
}
