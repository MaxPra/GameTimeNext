using GameTimeNext.Core.Application.ErrorMessages.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.ErrorMessages.Viewmodels
{
    public class ErrorMessagesViewModel : UIXViewModelBase
    {
        public ObservableCollection<ErrorMessagesDataGridRow>? ErrorMessagesDataGridRows { get; set; }
        public ErrorMessagesDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private ErrorMessagesDataGridRow? _selectedRow;

        public ErrorMessagesViewModel()
        {
            ErrorMessagesDataGridRows = new ObservableCollection<ErrorMessagesDataGridRow>();
        }
    }
}
