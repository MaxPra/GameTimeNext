using System.Collections.ObjectModel;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesExecutablesEditViewModel : UIXViewModelBase
    {
        public ObservableCollection<Executable> Executables { get; set; }

        public ProfilesExecutablesEditViewModel()
        {
            Executables = new ObservableCollection<Executable>();

        }
    }

    public class Executable
    {
        public string Name { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = false;
    }
}
