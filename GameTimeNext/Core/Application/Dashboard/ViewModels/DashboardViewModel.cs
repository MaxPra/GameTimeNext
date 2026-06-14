using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Dashboard.ViewModels
{
    public class DashboardViewModel : GTNViewModelBase
    {
        public ObservableCollection<T1PROFI> T1Profis { get; set; }

        public DashboardViewModel()
        {
            T1Profis = new ObservableCollection<T1PROFI>();
        }
    }
}
