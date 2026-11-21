using Arcora.Core.Application.Metadata.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace Arcora.Core.Application.Metadata.Viewmodels
{
    public class MetadataPosViewModel : UIXViewModelBase
    {
        public ObservableCollection<MetadataPosDataGridRow>? MetadataPosDataGridRows { get; set; }
        public MetadataPosDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private MetadataPosDataGridRow? _selectedRow;

        public MetadataPosViewModel()
        {
            MetadataPosDataGridRows = new ObservableCollection<MetadataPosDataGridRow>();
        }
    }
}
