using GameTimeNext.Core.Framework.Config;
using System.Diagnostics;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework
{
    /// <summary>
    /// Decides where the log entries of FnLog (AddInfo, AddWarning, AddError) are written.
    /// Target is table T1SYLOG of the log database (LogDataBaseManager). The text file of AppConfig.LogFilePath is the fallback:
    /// until the startup is completed every entry is also written there (if the app doesn't start, the module "Diagnostics" can't be opened),
    /// afterwards only when the log database can't be written.
    /// </summary>
    public class LogProvider : IUIXLogProvider
    {
        private readonly UIXFileLogProvider _fileLogProvider = new UIXFileLogProvider(AppConfig.LogFilePath);
        private bool _databaseFailureReported;

        /// <summary>
        /// Set by App once the main window is started; from then on entries only go to the text file if the log database fails.
        /// </summary>
        public bool StartupCompleted { get; set; }

        public void Write(UIXLogEntry entry)
        {
            bool writtenToFile = false;

            if (!StartupCompleted)
                writtenToFile = WriteToFile(entry);

            try
            {
                AppEnvironment.GetLogDataBaseManager().InsertT1sylog(entry);
            }
            catch (Exception ex)
            {
                // Not through FnLog: that would end up here again
                ReportDatabaseFailure(ex);

                if (!writtenToFile)
                    WriteToFile(entry);
            }
        }

        // [------------------------------------------------]
        // [------------------ PRIVATE ---------------------]
        // [------------------------------------------------]

        /// <summary>
        /// Writes the entry into the text file; a failure must not keep the entry out of the log database, so it only goes to the debug output.
        /// </summary>
        private bool WriteToFile(UIXLogEntry entry)
        {
            try
            {
                _fileLogProvider.Write(entry);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(entry);
                Debug.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Writes the reason once into the text file, so it is clear why the following entries are there and not in the log database.
        /// </summary>
        private void ReportDatabaseFailure(Exception exception)
        {
            if (_databaseFailureReported)
                return;

            _databaseFailureReported = true;

            UIXLogEntry failureEntry = new UIXLogEntry
            {
                Timestamp = DateTime.Now,
                LogType = UIXLogType.Error,
                Source = nameof(LogProvider),
                Message = "The log database can't be written, entries are written to this file instead.",
                Exception = exception
            };

            WriteToFile(failureEntry);
        }
    }
}
