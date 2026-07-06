using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Playthroughs.ViewModel
{
    public class PlaythroughsViewModel : UIXViewModelBase
    {
        public ObservableCollection<PlaythroughDataGridRow>? PlaythroughDataGridRows { get; set; }
        public PlaythroughDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private PlaythroughDataGridRow? _selectedRow;

        public PlaythroughsViewModel()
        {
            PlaythroughDataGridRows = new ObservableCollection<PlaythroughDataGridRow>();
        }
    }
}
