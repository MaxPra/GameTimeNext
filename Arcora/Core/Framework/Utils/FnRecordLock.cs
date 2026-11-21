using UIX.ViewController.Engine.DataBaseObjects;
using UIX.ViewController.Engine.Runnables;

namespace Arcora.Core.Framework.Utils
{
    /// <summary>
    /// Record lock for actions that change records without an edit view (e.g. context menu actions such as completing a
    /// playthrough or archiving a profile). Edit views lock their identifiers themselves (engine).
    /// </summary>
    public static class FnRecordLock
    {
        /// <summary>
        /// Locks the records for their application (T1....App, set by the data manager that read them; the calling application
        /// for records without one), runs the action and
        /// releases the locks it set. If another application holds one of them (open for editing, game being monitored),
        /// nothing runs, the user is told where it is open and false is returned; also when the action itself hits a lock
        /// (UIXRecordLockedException, e.g. a linked record).
        /// </summary>
        public static bool Run(UIXApplication app, Action action, params UIXTableObjectBase[] records)
        {
            return Run(app, () =>
            {
                action();
                return true;
            }, records);
        }

        /// <summary>
        /// Like Run(app, action, records), for an action that can be dropped (e.g. a deletion the user doesn't confirm):
        /// returns the result of the action, false if a record is locked by another application.
        /// </summary>
        public static bool Run(UIXApplication app, Func<bool> action, params UIXTableObjectBase[] records)
        {
            List<UIXTableObjectBase> acquired = new List<UIXTableObjectBase>();

            try
            {
                foreach (UIXTableObjectBase record in records)
                {
                    // Read without a data manager of an application (e.g. new TX...): changed by the calling application
                    record.App ??= app;

                    // Already held by its own application (e.g. still locked by a caller): not released here
                    bool alreadyHeld = record.App != null && ReferenceEquals(record.GetLockHolder()?.Holder, record.App);

                    if (!record.Lock())
                    {
                        ShowLocked(app, record.GetLockHolder());
                        return false;
                    }

                    if (!alreadyHeld)
                        acquired.Add(record);
                }

                return action();
            }
            catch (UIXRecordLockedException ex)
            {
                ShowLocked(app, ex.Lock);
                return false;
            }
            finally
            {
                foreach (UIXTableObjectBase record in acquired)
                    record.Unlock();
            }
        }

        /// <summary>
        /// Tells the user that the record is held by another application (GLO0023 / GLO0024), on the UI thread.
        /// </summary>
        public static void ShowLocked(UIXApplication app, UIXRecordLockInfo? recordLock)
        {
            if (recordLock == null)
                return;

            app.CallDispatcher.Invoke(() => UIXApplication.RecordLockMessageProvider?.ShowRecordLocked(app, recordLock));
        }
    }
}
