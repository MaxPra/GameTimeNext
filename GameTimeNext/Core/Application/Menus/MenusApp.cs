using GameTimeNext.Core.Application.Menus.Controller;
using GameTimeNext.Core.Application.Menus.DataWrapper;
using GameTimeNext.Core.Application.Menus.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Menus
{
    /// <summary>
    /// Menu management: tree of folders and application calls with a combined edit area for the selected entry.
    /// </summary>
    [UIXAppIcon(UIXMdlIcons.Menu)]
    public class MenusApp : UIXApplication, IUIXApplicationStarter
    {
        public MenusView? MenusView { get; set; }
        public MenusDetailView? MenusDetailView { get; set; }
        public MenusDataWrapper? DataWrapper { get; set; }

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

            MenusView!.ViewController.Show(options);
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(MenusApp));
        }

        public override void InitializeApplicationOutput()
        {
            // -- Overview
            MenusView = new MenusView();
            // Edit mode: otherwise the overview controller protects the mapped fields of the embedded edit area too
            MenusView.ViewIndicator.Add("ED");
            MainView = MenusView;
            MenusView.ViewController = new MenusViewController(this);

            // -- Combined edit area
            MenusDetailView = new MenusDetailView();
            MenusDetailView.ViewIndicator.Add("ED");
            MenusDetailView.ViewController = new MenusDetailViewController(this);
            MenusView.cpMenuDetail.Content = MenusDetailView;

            // -- DataWrapper
            DataWrapper = new MenusDataWrapper(MenusView.dgMenus, MenusView.ViewController, MenusDetailView.ViewController);
            MenusView.ViewController.DataWrapper = DataWrapper;
            MenusDetailView.ViewController.DataWrapper = DataWrapper;
        }

        public override bool CanClose()
        {
            return true;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
        }
    }
}
