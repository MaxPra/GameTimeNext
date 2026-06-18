using GameTimeNext.Core.Application.Codetables.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Codetables.Viewmodels
{
    public class CodetablesViewModel : UIXViewModelBase
    {
        public ObservableCollection<CodetableDataGridRow>? CodetableDataGridRows { get; set; }
        public CodetableDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private CodetableDataGridRow? _selectedRow;

        public CodetablesViewModel()
        {
            CodetableDataGridRows = new ObservableCollection<CodetableDataGridRow>();
        }
    }
}
