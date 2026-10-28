using GameTimeNext.Core.Application.Menus.ViewModel;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;

namespace GameTimeNext.Core.Application.Menus.DataWrapper
{
    /// <summary>
    /// Connects the menu tree (source) with the combined edit area (target): holds the menu entry being edited.
    /// </summary>
    public class MenusDataWrapper : UIXCE_DataWrapperBase<UIXTreeGrid, T1MENUE>
    {
        public MenusDataWrapper(UIXTreeGrid dataSource, UIXControllerBase controllerSource, UIXControllerBase controllerTarget)
            : base(dataSource, controllerSource, controllerTarget)
        {
        }

        public override T1MENUE? ResolveDataFromSelectedItem(object? selectedItem)
        {
            return (selectedItem as MenusTreeGridRow)?.RowObject as T1MENUE;
        }
    }
}
