using GameTimeNext.Core.Application.Playthroughs.Controller;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Playthroughs
{
    /// <summary>
    /// Optional rating dialog, shown after a playthrough has been completed or canceled.
    /// </summary>
    public class PlaythroughRatingApp : UIXApplication
    {
        private PlaythroughRatingViewController? _playthroughRatingViewController;
        private PlaythroughRatingView? _playthroughRatingView;

        public T1PLTHR T1plthr { get; set; } = new T1PLTHR();

        public void Rate(Action<PlaythroughRatingViewController.PlaythroughRatingViewReturn>? callback, T1PLTHR t1plthr)
        {
            T1plthr = t1plthr;

            _playthroughRatingView!.ViewIndicator.Add("ED");

            if (callback is not null)
                _playthroughRatingViewController!.SetResultCallback(callback);

            UIXApplicationStartOptions options = new UIXApplicationStartOptions();
            options.Target = UIXApplicationStartTarget.Window;

            _playthroughRatingViewController!.Show(options);
        }

        protected override void InitializeAppInfo()
        {
            ReadFromAppProperties(nameof(PlaythroughRatingApp));
        }

        public override void InitializeApplicationOutput()
        {
            _playthroughRatingView = new PlaythroughRatingView();
            MainView = _playthroughRatingView;

            _playthroughRatingViewController = new PlaythroughRatingViewController(this);
            _playthroughRatingView.ViewController = _playthroughRatingViewController;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
            options.ResizeMode = System.Windows.ResizeMode.NoResize;
        }
    }
}
