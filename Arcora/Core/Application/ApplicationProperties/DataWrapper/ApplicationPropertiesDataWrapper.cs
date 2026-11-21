using GameTimeNext.Core.Application.ApplicationProperties.Controller;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;

namespace GameTimeNext.Core.Application.ApplicationProperties.DataWrapper
{
    /// <summary>
    /// Connects the definitions list (source) with the combined edit area (target): holds the definition being edited.
    /// </summary>
    public class ApplicationPropertiesDataWrapper : UIXCE_DataWrapperBase<UIXDataGrid, T1APRDH>
    {
        public ApplicationPropertiesDataWrapper(UIXDataGrid dataSource, UIXControllerBase controllerSource, UIXControllerBase controllerTarget)
            : base(dataSource, controllerSource, controllerTarget)
        {
        }

        public override T1APRDH? ResolveDataFromSelectedItem(object? selectedItem)
        {
            return (selectedItem as ApplicationPropertiesDataGridRow)?.RowObject as T1APRDH;
        }
    }
}
