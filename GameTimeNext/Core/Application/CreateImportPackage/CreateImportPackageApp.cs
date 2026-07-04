using GameTimeNext.Core.Application.CreateImportPackage.Controller;
using GameTimeNext.Core.Application.CreateImportPackage.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.CreateImportPackage
{
    public class CreateImportPackageApp : UIXApplication, IUIXApplicationStarter
    {
        public CreateImportPackageView? CreateImportPackageView { get; set; }

        public CreateImportPackageViewController? CreateImportPackageViewController { get; set; }

        public override void InitializeApplicationOutput()
        {
            CreateImportPackageView = new CreateImportPackageView();
            MainView = CreateImportPackageView;

            CreateImportPackageViewController = new CreateImportPackageViewController(this);
            CreateImportPackageView.ViewController = CreateImportPackageViewController;

            Icon = UIXMdlIcons.Folder;
        }

        public void Start(UIXApplication hostApplication, ContentPresenter presenter)
        {
            HostApplication = hostApplication;
            CreateImportPackageView!.ContentPresenter = presenter;
            CreateImportPackageView.ViewController.Show(false);

            Loader = hostApplication.Loader;
            CallDispatcher = hostApplication.CallDispatcher;
        }

        public override bool CanClose()
        {
            return true;
        }
    }
}
