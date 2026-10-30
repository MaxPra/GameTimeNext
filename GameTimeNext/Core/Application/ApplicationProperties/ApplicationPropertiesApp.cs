using GameTimeNext.Core.Application.ApplicationProperties.Controller;
using GameTimeNext.Core.Application.ApplicationProperties.DataWrapper;
using GameTimeNext.Core.Application.ApplicationProperties.Selectors;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ApplicationProperties
{
    /// <summary>
    /// Application properties definitions: list of the definitions with a combined edit area for the selected one.
    /// </summary>
    [UIXAppIcon(UIXMdlIcons.Tools)]
    public class ApplicationPropertiesApp : UIXApplication, IUIXApplicationStarter
    {

        public ApplicationPropertiesView? ApplicationPropertiesView { get; set; }
        public ApplicationPropertiesDetailView? ApplicationPropertiesDetailView { get; set; }
        public ApplicationPropertiesDataWrapper? DataWrapper { get; set; }

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

            ApplicationPropertiesView!.ViewIndicator.Clear();
            ApplicationPropertiesView!.ViewIndicator.Add("ED");
            ApplicationPropertiesView!.ViewController.Show(options);
        }

        public void StartWindowed()
        {
            ApplicationPropertiesView!.ViewIndicator.Clear();
            ApplicationPropertiesView!.ViewIndicator.Add("ED");

            // Fixed width: the view sizes itself (Width Auto) and the sub window is not resizable; wide enough for list and edit area
            ApplicationPropertiesView!.ViewController.Show(
                new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.Window,
                    WindowWidth = 1400
                });
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ApplicationPropertiesApp));
        }

        public override void InitializeApplicationOutput()
        {
            // -- Overview
            ApplicationPropertiesView = new ApplicationPropertiesView();
            MainView = ApplicationPropertiesView;
            ApplicationPropertiesView.ViewController = new ApplicationPropertiesViewController(this);
            ApplicationPropertiesView.dgApplicationProperties.SelectorAdapter = new ApplicationPropertiesView_ApplicationPropertiesSelectorAdapter(this);
            ApplicationPropertiesView.dgApplicationProperties.SelectorController = new ApplicationPropertiesView_ApplicationPropertiesSelectorController(this);

            // -- Combined edit area
            ApplicationPropertiesDetailView = new ApplicationPropertiesDetailView();
            ApplicationPropertiesDetailView.ViewIndicator.Add("ED");
            ApplicationPropertiesDetailView.ViewController = new ApplicationPropertiesDetailViewController(this);
            ApplicationPropertiesView.cpApplicationPropertiesDetail.Content = ApplicationPropertiesDetailView;

            // -- DataWrapper
            DataWrapper = new ApplicationPropertiesDataWrapper(ApplicationPropertiesView.dgApplicationProperties, ApplicationPropertiesView.ViewController, ApplicationPropertiesDetailView.ViewController);
            ApplicationPropertiesView.ViewController.DataWrapper = DataWrapper;
            ApplicationPropertiesDetailView.ViewController.DataWrapper = DataWrapper;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
