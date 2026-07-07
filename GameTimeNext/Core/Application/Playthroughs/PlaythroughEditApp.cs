using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Playthroughs.Controller;
using GameTimeNext.Core.Application.Playthroughs.Views;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Playthroughs
{
    public class PlaythroughEditApp : UIXApplication
    {
        private PlaythroughEditViewController? _playthroughEditViewController;
        private PlaythroughEditView? _playthroughEditView;

        public T1PLTHR T1plthr { get; set; } = new T1PLTHR();
        public T1PROFI T1profi { get; set; } = new T1PROFI();

        public void Edit(Action<PlaythroughEditViewController.PlaythroughEditViewReturn> callback, T1PLTHR t1plthr)
        {
            T1plthr = t1plthr;

            _playthroughEditView!.ViewIndicator.Add("ED");
            _playthroughEditViewController!.SetResultCallback(callback);

            UIXApplicationStartOptions options = new UIXApplicationStartOptions();
            options.Target = UIXApplicationStartTarget.Window;

            _playthroughEditViewController!.Show(options);
        }

        public void CreateNew(Action<PlaythroughEditViewController.PlaythroughEditViewReturn> callback, T1PROFI t1profi)
        {
            T1profi = t1profi;
            T1plthr = new TXPLTHR().CreateNew();
            T1plthr.PFID = T1profi.PFID;
            T1plthr.AcceptChanges();

            _playthroughEditView!.ViewIndicator.Add("CN");
            _playthroughEditViewController!.SetResultCallback(callback);

            UIXApplicationStartOptions options = new UIXApplicationStartOptions();
            options.Target = UIXApplicationStartTarget.Window;

            _playthroughEditViewController!.Show(options);
        }

        public override void InitializeApplicationOutput()
        {
            _playthroughEditView = new PlaythroughEditView();
            MainView = _playthroughEditView;

            _playthroughEditViewController = new PlaythroughEditViewController(this);
            _playthroughEditView.ViewController = _playthroughEditViewController;
        }

        public override void SetWindowProperties(UIXApplicationStartOptions options)
        {
            options.Dialog = true;
            options.ResizeMode = System.Windows.ResizeMode.NoResize;
        }
    }
}
