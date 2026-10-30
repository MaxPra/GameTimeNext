using GameTimeNext.Core.Application.Screensetups.Controller;
using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Screensetups.Viewmodels
{
    public class ScreensetupsEditViewModel : UIXViewModelBase
    {
        public ObservableCollection<ScreensetupMonitorDataGridRow>? MonitorDataGridRows { get; set; }

        public ScreensetupsEditViewModel()
        {
            MonitorDataGridRows = new ObservableCollection<ScreensetupMonitorDataGridRow>();
        }
    }
}
