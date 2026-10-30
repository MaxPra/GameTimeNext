using GameTimeNext.Core.Application.ErrorMessages.Controller;
using GameTimeNext.Core.Application.ErrorMessages.Selectors;
using GameTimeNext.Core.Application.ErrorMessages.Views;
using System.Windows.Controls;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.ErrorMessages
{
    [UIXAppIcon(UIXMdlIcons.Error)]
    public class ErrorMessagesApp : UIXApplication, IUIXApplicationStarter
    {

        public ErrorMessagesView? ErrorMessagesView { get; set; }

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

            ErrorMessagesView!.ViewIndicator.Clear();
            ErrorMessagesView!.ViewIndicator.Add("ED");
            ErrorMessagesView!.ViewController.Show(options);
        }

        public void StartWindowed()
        {
            ErrorMessagesView!.ViewIndicator.Clear();
            ErrorMessagesView!.ViewIndicator.Add("ED");

            // Fixed width: the view sizes itself (Width Auto) and the sub window is not resizable
            ErrorMessagesView!.ViewController.Show(
                new UIXApplicationStartOptions
                {
                    Target = UIXApplicationStartTarget.Window,
                    WindowWidth = 1100
                });
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(ErrorMessagesApp));
        }

        public override void InitializeApplicationOutput()
        {
            ErrorMessagesView = new ErrorMessagesView();
            MainView = ErrorMessagesView;
            ErrorMessagesView.ViewController = new ErrorMessagesViewController(this);
            ErrorMessagesView.dgErrorMessages.SelectorAdapter = new ErrorMessagesView_ErrorMessagesSelectorAdapter(this);
            ErrorMessagesView.dgErrorMessages.SelectorController = new ErrorMessagesView_ErrorMessagesSelectorController(this);
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
        }
    }
}
