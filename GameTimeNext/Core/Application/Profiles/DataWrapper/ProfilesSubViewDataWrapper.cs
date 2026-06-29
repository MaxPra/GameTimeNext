using GameTimeNext.Core.Application.Profiles.Viewmodel;
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

        public override T1PROFI? ResolveTableObjectFromSelectedItem(object? selectedItem)
        {
            if (selectedItem is ProfilesListBoxItem listBoxItem)
                return listBoxItem.ItemObject as T1PROFI;

            return selectedItem as T1PROFI;
        }
    }
}
