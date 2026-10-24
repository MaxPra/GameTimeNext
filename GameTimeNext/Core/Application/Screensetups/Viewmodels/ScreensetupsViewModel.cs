using GameTimeNext.Core.Application.Screensetups.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Screensetups.Viewmodels
{
    public class ScreensetupsViewModel : UIXViewModelBase
    {
        public ObservableCollection<ScreensetupDataGridRow>? ScreensetupDataGridRows { get; set; }
        public ScreensetupDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private ScreensetupDataGridRow? _selectedRow;

        public ScreensetupsViewModel()
        {
            ScreensetupDataGridRows = new ObservableCollection<ScreensetupDataGridRow>();
        }
    }
}
