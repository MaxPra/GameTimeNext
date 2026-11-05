using GameTimeNext.Core.Application.SelectionLists.Controller;
using GameTimeNext.Core.Application.SelectionLists.Selectors;
using GameTimeNext.Core.Application.SelectionLists.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.SelectionLists
{
    /// <summary>
    /// The user friendly view on the codetables: the selection lists the user may extend (e.g. platforms and their symbols),
    /// without text types and keys. Works on the same data as the module "Codetables".
    /// </summary>
    [UIXAppIcon(UIXMdlIcons.BulletList)]
    public class SelectionListsApp : UIXApplication, IUIXApplicationStarter
    {
        public SelectionListsView? SelectionListsView { get; set; }

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

            SelectionListsView!.ViewIndicator.Clear();
            SelectionListsView.ViewIndicator.Add("ED");
            SelectionListsView.ViewController.Show(options);
        }

        public void StartWindowed()
        {
            SelectionListsView!.ViewIndicator.Clear();
            SelectionListsView.ViewIndicator.Add("ED");

            // Fixed width: the view sizes itself (Width Auto) and the sub window is not resizable
            SelectionListsView.ViewController.Show(
                new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.Window,
                    WindowWidth = 1100
                });
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(SelectionListsApp));
        }

        public override void InitializeApplicationOutput()
        {
            SelectionListsView = new SelectionListsView();
            MainView = SelectionListsView;
            SelectionListsView.ViewController = new SelectionListsViewController(this);

            // -- Selectors
            SelectionListsView.dgLists.SelectorAdapter = new SelectionListsView_ListsSelectorAdapter(this);
            SelectionListsView.dgLists.SelectorController = new SelectionListsView_ListsSelectorController(this);
            SelectionListsView.dgEntries.SelectorAdapter = new SelectionListsView_EntriesSelectorAdapter(this);
            SelectionListsView.dgEntries.SelectorController = new SelectionListsView_EntriesSelectorController(this);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
