using GameTimeNext.Core.Application.Codetables.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Codetables.Viewmodels
{
    public class CodetablesEntrysViewModel : UIXViewModelBase
    {
        public ObservableCollection<CodetableEntryDataGridRow>? CodetableEntryDataGridRows { get; set; }
        public CodetableEntryDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private CodetableEntryDataGridRow? _selectedRow;

        public CodetablesEntrysViewModel()
        {
            CodetableEntryDataGridRows = new ObservableCollection<CodetableEntryDataGridRow>();
        }
    }
}
