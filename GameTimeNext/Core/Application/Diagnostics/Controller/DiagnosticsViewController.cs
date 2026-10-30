using GameTimeNext.Core.Application.Diagnostics.DataWrapper;
using GameTimeNext.Core.Application.Diagnostics.Views;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.FrameworkElements.Selectors;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Diagnostics.Controller
{
    public class DiagnosticsViewController : UIXViewControllerBase<DiagnosticsApp, DiagnosticsView>
    {
        private DiagnosticsLogDataWrapper? _logDataWrapper;

        // Width of the detail column, kept while it is closed so a width set with the splitter survives the next selection
        private GridLength _logDetailWidth = new GridLength(920);

        public DiagnosticsViewController(UIXApplication app) : base(app)
        {
        }

        #region Event-Pipeline-Methods
        protected override void Init()
        {
            _logDataWrapper = GetDataWrapper<DiagnosticsLogDataWrapper>();

            // Set loader texts
            GetApp().Loader.SetRandomTexts(
                                            "Running diagnostics...",
                                            "Checking on the background processes...",
                                            "Counting background cycles...",
                                            "Asking SteamGridDB if it's awake...",
                                            "Knocking on IGDB's door...",
                                            "Requesting a fresh Twitch token...",
                                            "Measuring API response times...",
                                            "Reading the log book...",
                                            "Sorting warnings from errors...",
                                            "Following the stack traces...",
                                            "Looking for clues in the log...",
                                            "Checking the pulse of the app...",
                                            "Inspecting the engine room...",
                                            "Tracing every little hiccup...",
                                            "Interrogating the background workers...",
                                            "Dusting off old log entries...",
                                            "Making sure everything runs smoothly..."
                                            );
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
        }

        protected override void BuildFirstImpl()
        {
            // Default: the last 7 days, open end
            GetView().dtpFrom.SelectedDate = DateTime.Today.AddDays(-6);
            GetView().dtpTo.SelectedDate = null;

            FillComboboxLogType();

            // Nothing is selected yet: no detail area
            SetLogDetailOpen(false);
        }

        protected override async Task BuildFirstImplAsync()
        {
            await GetView().dgBackgroundProcesses.LoadDataAsync();
            await GetView().dgApiStatus.LoadDataAsync();
            await LoadLogAsync();
        }

        protected override void BuildImpl()
        {
        }

        protected override async Task BuildImplAsync()
        {

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
            SetLogDetailOpen(true);
        }
        #endregion

        /// <summary>
        /// Closes the detail area and clears the selection of the log list.
        /// </summary>
        public void CloseLogDetail()
        {
            GetView().dgLog.SelectedItem = null;

            _logDataWrapper!.Set(null);
            SetLogDetailOpen(false);
        }

        /// <summary>
        /// Shows or hides the detail column with its splitter. A hidden column gets the width 0 (a collapsed presenter
        /// would leave the column's width empty); its last width is restored when it opens again.
        /// </summary>
        private void SetLogDetailOpen(bool open)
        {
            DiagnosticsView view = GetView();

            if (!open && view.colLogDetail.Width.Value > 0)
                _logDetailWidth = view.colLogDetail.Width;

            view.colLogSplitter.Width = open ? new GridLength(16) : new GridLength(0);
            view.colLogDetail.Width = open ? _logDetailWidth : new GridLength(0);
            view.colLogDetail.MinWidth = open ? 320 : 0;
            FnControls.SetVisible(view.gspLogDetail, open);
            FnControls.SetVisible(view.cpLogDetail, open);
        }

        private async Task LoadLogAsync()
        {
            if (!await GetView().dgLog.LoadDataAsync())
                return;

            int count = GetView().dgLog.Items.Count;

            GetView().txtLogCount.Text = count >= CFDiagnosticsApp.LogTopX
                ? string.Format(CultureInfo.CurrentCulture, "Newest {0} entries", count)
                : string.Format(CultureInfo.CurrentCulture, "{0} entries", count);

            CloseLogDetail();
        }

        /// <summary>
        /// Log types; the keys are the values of T1SYLOG.LGTY (UIXLogType, GTNLogType).
        /// </summary>
        private void FillComboboxLogType()
        {
            UIXManualCodetable codetable = new UIXManualCodetable();
            codetable.AddEntry(nameof(UIXLogType.Info), "Info");
            codetable.AddEntry(nameof(UIXLogType.Warning), "Warning");
            codetable.AddEntry(nameof(UIXLogType.Error), "Error");
            codetable.AddEntry(nameof(GTNLogType.AppStart), "App start");
            codetable.AddEntry(nameof(GTNLogType.LogCleared), "Log Cleared");
            codetable.ApplyTo(GetView().cmbLogType);

            GetView().cmbLogType.SelectedIndex = 0;
        }

        protected async Task EV_btnRefreshProcesses()
        {
            await GetView().dgBackgroundProcesses.LoadDataAsync();
            await GetView().dgApiStatus.LoadDataAsync();
        }

        protected async Task EV_btnRefreshLog()
        {
            await LoadLogAsync();
        }

        protected async Task EV_btnClearLog()
        {
            CFMBOX cfmbox = GetApp().GetApplication<CFMBOX>(UIXApplicationStartTarget.Window);

            CFMBOXInputResult result = cfmbox.ShowInput(GetErrorMessage("DGN0001"), "", CFMBOXIcon.Warning);

            if (!AppConfig.Root.ApplicationName.Equals(result.Text)) return;

            AppEnvironment.GetLogDataBaseManager().ClearT1sylog();

            // First entry of the new log: who emptied it and when
            FnLog.Add(GTNLogType.LogCleared, GetApp(), "Log cleared.");

            await LoadLogAsync();
        }
    }
}
