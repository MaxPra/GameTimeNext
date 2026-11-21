using UIX.ViewController.Engine.Utils;

namespace Arcora.Core.Framework
{
    /// <summary>
    /// Log types of arcora in addition to the engine's (Info, Warning, Error); logged with FnLog.Add(ArcoraLogType.X, ...).
    /// The key is the field name (stored in T1SYLOG.LGTY), so nameof(ArcoraLogType.X) serves as constant.
    /// </summary>
    public class ArcoraLogType : UIXLogType
    {
        /// <summary>
        /// Steps of the application start (App.InitializeApp and the checks right after it).
        /// </summary>
        public static readonly ArcoraLogType AppStart = new ArcoraLogType(nameof(AppStart));
        public static readonly ArcoraLogType LogCleared = new ArcoraLogType(nameof(LogCleared));

        private ArcoraLogType(string key) : base(key)
        {
        }
    }
}
