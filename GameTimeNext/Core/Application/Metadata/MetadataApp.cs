using GameTimeNext.Core.Application.Metadata.Controller;
using GameTimeNext.Core.Application.Metadata.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata
{
    public class MetadataApp : UIXApplication, IUIXApplicationStarter
    {
        public MetadataView? MetadataView { get; set; }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            HostApplication = hostApplication;
            MetadataView!.ContentPresenter = presenter;
            MetadataView.ViewController.Show(false);

            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;
        }

        public override void InitializeApplicationOutput()
        {
            MetadataView = new MetadataView();
            MainView = MetadataView;
            MetadataView.ViewController = new MetadataViewController(this);

            Icon = UIXMdlIcons.DataBase;
        }

        public override bool CanClose()
        {
            return true;
        }
    }
}
