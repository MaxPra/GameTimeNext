using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.BackgroundProcesses
{
    public class AutoProfileSwitchingProcess : UIXBackgroundProcess
    {
        public override void Logic()
        {

        }

        protected override void InitializeInfos()
        {
            ProcessName = "AutoProfileSwitchingProcess";
        }
    }
}
