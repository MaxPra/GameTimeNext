using GameTimeNext.Core.Application.Codetables.Controller;
using GameTimeNext.Core.Application.Codetables.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables
{
    public class CodetablesApp : UIXApplication, IUIXApplicationStarter
    {
        // **************************************************
        // ToDo: T1CTABD (Codetabellen Einträge)
        //       Ferner Combobox auf T1CTABD Einträge binden
        //       ...
        // **************************************************
        public CodetablesView? CodetablesView { get; set; }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            HostApplication = hostApplication;
            CodetablesView!.ContentPresenter = presenter;

            CodetablesView.ViewIndicator.Clear();
            CodetablesView.ViewIndicator.Add("ED");
            CodetablesView.ViewController.Show(false);

            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;
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
    }
}
