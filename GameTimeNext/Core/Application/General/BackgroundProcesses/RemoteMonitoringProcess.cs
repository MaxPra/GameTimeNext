using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Logging;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.General.BackgroundProcesses
{
    public class RemoteMonitoringProcess : UIXBackgroundProcess
    {
        public override void Logic()
        {
            if (!AppEnvironment.GetAppConfig().AppSettings.RemoteMonitoring) return;

            FnLog.AddInfo(this, "Starting...");
        }

        public override void InitializeApplicationOutput()
        {

        }

        protected override void InitializeInfos()
        {

        }
    }
}
