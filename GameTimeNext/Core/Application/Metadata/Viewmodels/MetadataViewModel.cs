using GameTimeNext.Core.Application.Metadata.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Metadata.Viewmodels
{
    public class MetadataViewModel : UIXViewModelBase
    {
        public ObservableCollection<MetadataDataGridRow>? MetadataDataGridRows { get; set; }
        public MetadataDataGridRow? SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }

        private MetadataDataGridRow? _selectedRow;

        public MetadataViewModel()
        {
            MetadataDataGridRows = new ObservableCollection<MetadataDataGridRow>();
        }
    }
}
