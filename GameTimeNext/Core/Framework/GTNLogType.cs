using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework
{
    /// <summary>
    /// Log types of GameTimeNext in addition to the engine's (Info, Warning, Error); logged with FnLog.Add(GTNLogType.X, ...).
    /// The key is the field name (stored in T1SYLOG.LGTY), so nameof(GTNLogType.X) serves as constant.
    /// </summary>
    public class GTNLogType : UIXLogType
    {
        /// <summary>
        /// Steps of the application start (App.InitializeApp and the checks right after it).
        /// </summary>
        public static readonly GTNLogType AppStart = new GTNLogType(nameof(AppStart));
        public static readonly GTNLogType LogCleared = new GTNLogType(nameof(LogCleared));

        private GTNLogType(string key) : base(key)
        {
        }
    }
}
