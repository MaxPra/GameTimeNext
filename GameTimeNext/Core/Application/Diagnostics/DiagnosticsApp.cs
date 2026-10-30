using GameTimeNext.Core.Application.Diagnostics.Controller;
using GameTimeNext.Core.Application.Diagnostics.DataWrapper;
using GameTimeNext.Core.Application.Diagnostics.Selectors;
using GameTimeNext.Core.Application.Diagnostics.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Diagnostics
{
    [UIXAppIcon(UIXMdlIcons.Tools)]
    public class DiagnosticsApp : UIXApplication, IUIXApplicationStarter
    {

        public DiagnosticsView? DiagnosticsView { get; set; }
        public DiagnosticsLogDetailView? DiagnosticsLogDetailView { get; set; }
        public DiagnosticsLogDataWrapper? LogDataWrapper { get; set; }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            Start(hostApplication, new UIXApplicationStartOptions
            {
                Target = UIXApplicationStartTarget.ContentPresenter,
                Presenter = presenter
            });
        }

        public void Start(UIXApplication hostApplication, UIXApplicationStartOptions options)
        {
            HostApplication = hostApplication;
            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;

            DiagnosticsView!.ViewIndicator.Clear();
            DiagnosticsView!.ViewIndicator.Add("ED");
            DiagnosticsView!.ViewController.Show(options);
        }

        public void StartWindowed()
        {
            DiagnosticsView!.ViewIndicator.Clear();
            DiagnosticsView!.ViewIndicator.Add("ED");

            // Fixed width: the view sizes itself (Width Auto) and the sub window is not resizable; wide enough for list and detail
            DiagnosticsView!.ViewController.Show(
                new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.Window,
                    WindowWidth = 1500
                });
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(DiagnosticsApp));
        }

        public override void InitializeApplicationOutput()
        {
            DiagnosticsView = new DiagnosticsView();
            MainView = DiagnosticsView;
            DiagnosticsView.ViewController = new DiagnosticsViewController(this);

            // -- Selectors
            DiagnosticsView.dgBackgroundProcesses.SelectorAdapter = new DiagnosticsView_BackgroundProcessesSelectorAdapter(this);
            DiagnosticsView.dgBackgroundProcesses.SelectorController = new DiagnosticsView_BackgroundProcessesSelectorController(this);
            DiagnosticsView.dgApiStatus.SelectorAdapter = new DiagnosticsView_ApiStatusSelectorAdapter(this);
            DiagnosticsView.dgApiStatus.SelectorController = new DiagnosticsView_ApiStatusSelectorController(this);
            DiagnosticsView.dgLog.SelectorAdapter = new DiagnosticsView_LogSelectorAdapter(this);
            DiagnosticsView.dgLog.SelectorController = new DiagnosticsView_LogSelectorController(this);

            // -- Detail area of the log list
            DiagnosticsLogDetailView = new DiagnosticsLogDetailView();
            DiagnosticsLogDetailView.ViewIndicator.Add("ED");
            DiagnosticsLogDetailView.ViewController = new DiagnosticsLogDetailViewController(this);
            DiagnosticsView.cpLogDetail.Content = DiagnosticsLogDetailView;

            // -- DataWrapper
            LogDataWrapper = new DiagnosticsLogDataWrapper(DiagnosticsView.dgLog, DiagnosticsView.ViewController, DiagnosticsLogDetailView.ViewController);
            DiagnosticsView.ViewController.DataWrapper = LogDataWrapper;
            DiagnosticsLogDetailView.ViewController.DataWrapper = LogDataWrapper;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
