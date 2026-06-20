using GameTimeNext.Core.Application.Profiles.Viewmodel;
using GameTimeNext.Core.Framework.ViewModelsBase;
using System.Collections.ObjectModel;

namespace GameTimeNext.Core.Application.Dashboard.ViewModels
{
    public class DashboardViewModel : GTNViewModelBase
    {
        public ObservableCollection<ProfilesListBoxItem> T1Profis { get; set; }

        public DashboardViewModel()
        {
            T1Profis = new ObservableCollection<ProfilesListBoxItem>();
        }
    }
}
