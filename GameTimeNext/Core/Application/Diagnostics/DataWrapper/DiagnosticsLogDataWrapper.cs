using GameTimeNext.Core.Application.Diagnostics.Controller;
using GameTimeNext.Core.Framework.DataBase;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;

namespace GameTimeNext.Core.Application.Diagnostics.DataWrapper
{
    /// <summary>
    /// Connects the log list (source) with the detail area below it (target): holds the log entry shown there.
    /// </summary>
    public class DiagnosticsLogDataWrapper : UIXCE_DataWrapperBase<UIXDataGrid, SystemLogRecord>
    {
        public DiagnosticsLogDataWrapper(UIXDataGrid dataSource, UIXControllerBase controllerSource, UIXControllerBase controllerTarget)
            : base(dataSource, controllerSource, controllerTarget)
        {
        }

        public override SystemLogRecord? ResolveDataFromSelectedItem(object? selectedItem)
        {
            return (selectedItem as LogDataGridRow)?.RowObject as SystemLogRecord;
        }
    }
}
