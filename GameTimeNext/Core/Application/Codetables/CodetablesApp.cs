using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables
{
    public class CodetablesApp : UIXApplication, IUIXApplicationStarter
    {

        public CodetablesView? CodetablesView { get; set; }

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

            CodetablesView.ViewIndicator.Clear();
            CodetablesView.ViewIndicator.Add("ED");
            CodetablesView.ViewController.Show(options);
        }

        public void StartWindowed()
        {
            CodetablesView.ViewIndicator.Clear();
            CodetablesView.ViewIndicator.Add("ED");

            CodetablesView.ViewController.Show(
                new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.Window
                });
        }


        public override void InitializeApplicationOutput()
        {
            CodetablesView = new CodetablesView();
            MainView = CodetablesView;
            CodetablesView.ViewController = new CodetablesViewController(this);

            Icon = UIXMdlIcons.BulletList;
        }

        public override bool CanClose()
        {
            return base.CanClose();
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
