using Arcora.Core.Application.Diagnostics.Controller;
using Arcora.Core.Application.Diagnostics.Views;
using Arcora.Core.Application.Profiles;
using Arcora.Core.Framework.DataBase;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;
using static UIX.ViewController.Engine.FrameworkElements.UIXContextMenuFactory;

namespace Arcora.Core.Application.Diagnostics.Selectors
{
    public class DiagnosticsView_LogSelectorController : UIXSelectorControllerBase<DiagnosticsApp, DiagnosticsView, UIXDataGrid>
    {
        public DiagnosticsView_LogSelectorController(UIXApplication app) : base(app)
        {
        }

        private SystemLogRecord? GetSelectedRecord()
        {
            return GetSelectedItem<LogDataGridRow>()?.RowObject as SystemLogRecord;
        }

        private static void BuildContextMenu(DataGridRow dgRow)
        {
            ContextMenuBuilder contextBuilder = UIXContextMenuFactory.Create("DiagnosticsLogContextMenu");
            contextBuilder.SetStyle(ProfilesContextMenuBuilder.contextMenuStyle);

            contextBuilder.AddItem("ctxtCopy", "Copy", icon: UIXContextMenuFactory.CreateMdlIcon(UIXMdlIcons.Copy), itemStyle: ProfilesContextMenuBuilder.contextMenuItemStyle);

            dgRow.ContextMenu = contextBuilder.Build();
        }

        protected void EV_CtxtOpening(FrameworkElement target)
        {
            if (target is not DataGridRow row || row.DataContext is not LogDataGridRow logRow)
                return;

            GetSelector().SelectedItem = logRow;

            BuildContextMenu(row);

            row.ContextMenu!.PlacementTarget = row;
            row.ContextMenu.Placement = PlacementMode.MousePoint;
            row.ContextMenu.IsOpen = true;
        }

        protected void EV_ctxtCopy()
        {
            if (GetSelectedRecord() is not SystemLogRecord record)
                return;

            Clipboard.SetText(CFDiagnosticsApp.FormatLogDetail(record));
        }
    }
}
