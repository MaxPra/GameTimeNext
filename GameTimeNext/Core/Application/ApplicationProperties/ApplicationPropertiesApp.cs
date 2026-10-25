using GameTimeNext.Core.Application.ApplicationProperties.Controller;
using GameTimeNext.Core.Application.ApplicationProperties.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ApplicationProperties
{
    public class ApplicationPropertiesApp : UIXApplication, IUIXApplicationStarter
    {

        public ApplicationPropertiesView? ApplicationPropertiesView { get; set; }

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

            // Fixed width: the view sizes itself (Width Auto) and the sub window is not resizable
            ApplicationPropertiesView!.ViewController.Show(
                new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.Window,
                    WindowWidth = 1100
                });
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ApplicationPropertiesApp));
        }

        public override void InitializeApplicationOutput()
        {
            ApplicationPropertiesView = new ApplicationPropertiesView();
            MainView = ApplicationPropertiesView;
            ApplicationPropertiesView.ViewController = new ApplicationPropertiesViewController(this);

            Icon = UIXMdlIcons.Tools;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
