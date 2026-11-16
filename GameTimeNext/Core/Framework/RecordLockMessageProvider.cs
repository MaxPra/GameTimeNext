using GameTimeNext.Core.Application.TimeMonitoring;
using GameTimeNext.Core.Framework.UI.Dialogs;
using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework
{
    /// <summary>
    /// Tells the user that a record is being edited elsewhere (record lock), when the engine doesn't open an edit view
    /// because another application holds the lock of one of its table objects (also used by FnRecordLock). The message
    /// (GLO0023) names the module the record is open in; a record of the monitored game gets GLO0024.
    /// </summary>
    public class RecordLockMessageProvider : IUIXRecordLockMessageProvider
    {
        public void ShowRecordLocked(UIXApplication app, UIXRecordLockInfo recordLock)
        {
            string message = ReferenceEquals(recordLock.Holder, CFGameTimeMonitoring.GetLockOwner())
                ? app.GetErrorMessage("GLO0024")
                : app.GetErrorMessage("GLO0023", GetModuleName(recordLock.Holder));

            app.GetApplication<CFMBOX>(UIXApplicationStartTarget.Window).Show(message, CFMBOXResult.Ok, CFMBOXIcon.Info);
        }

        /// <summary>
        /// Name of the module the holder belongs to: the topmost application below the main application (e.g. the tab
        /// "Playthroughs" for its edit dialog), so the user knows where to look.
        /// </summary>
        public static string GetModuleName(UIXApplication holder)
        {
            UIXApplication module = holder;
            HashSet<UIXApplication> visited = new HashSet<UIXApplication> { module };

            // The main application is its own host (MainApp.Start(mainApp, ...)): stop below it, and never run in a circle
            while (module.HostApplication is UIXApplication host && !IsRoot(host) && visited.Add(host))
                module = host;

            return module.AppProperties.ApplicationName;
        }

        private static bool IsRoot(UIXApplication app)
        {
            return app.HostApplication == null || ReferenceEquals(app.HostApplication, app);
        }
    }
}
