using GameTimeNext.Core.Application.Diagnostics.DataWrapper;
using GameTimeNext.Core.Application.Diagnostics.Views;
using GameTimeNext.Core.Framework.DataBase;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Diagnostics.Controller
{
    /// <summary>
    /// Detail area of the log list: shows the log entry held by the data wrapper (read only).
    /// </summary>
    public class DiagnosticsLogDetailViewController : UIXViewControllerBase<DiagnosticsApp, DiagnosticsLogDetailView>
    {
        private DiagnosticsLogDataWrapper? _dataWrapper;

        public DiagnosticsLogDetailViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _dataWrapper = GetDataWrapper<DiagnosticsLogDataWrapper>();
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
        }

        protected override void BuildImpl()
        {
            if (!_dataWrapper!.HasData)
                return;

            SystemLogRecord record = _dataWrapper.Get();

            GetView().txIcon.Text = CFDiagnosticsApp.GetLogTypeIcon(record.LGTY);
            GetView().txIcon.SetResourceReference(TextBlock.ForegroundProperty, CFDiagnosticsApp.GetLogTypeBrushKey(record.LGTY));
            GetView().txTitle.Text = $"{record.LGTY} · {CFDiagnosticsApp.FormatTimestamp(record.LGTS)}";
            GetView().txSubtitle.Text = CFDiagnosticsApp.FormatLogOrigin(record);
            FnControls.SetVisible(GetView().txSubtitle, !FnString.IsNullEmptyOrWhitespace(GetView().txSubtitle.Text));
            GetView().txbText.Text = CFDiagnosticsApp.FormatLogText(record);
        }

        protected override void CheckImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }
        #endregion

        protected void EV_btnCopy()
        {
            if (!_dataWrapper!.HasData)
                return;

            Clipboard.SetText(CFDiagnosticsApp.FormatLogDetail(_dataWrapper.Get()));
        }
    }
}
