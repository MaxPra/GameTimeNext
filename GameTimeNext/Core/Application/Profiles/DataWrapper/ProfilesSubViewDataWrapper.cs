using GameTimeNext.Core.Application.TableObjects;
using System.Windows.Controls;
using UIX.ViewController.Engine.Controller;

namespace GameTimeNext.Core.Application.Profiles.DataWrapper
{
    public class ProfilesSubViewDataWrapper : UIXCE_DataWrapperBase<ListBox, T1PROFI>
    {
        public ProfilesSubViewDataWrapper(ListBox dataSource, UIXControllerBase controllerSource, UIXControllerBase controllerTarget)
            : base(dataSource, controllerSource, controllerTarget)
        {
        }
    }
}
