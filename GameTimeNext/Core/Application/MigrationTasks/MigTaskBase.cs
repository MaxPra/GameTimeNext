using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Versioning;
using System.Data.SQLite;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    internal abstract class MigTaskBase
    {
        #region Properties
        private readonly string _version;
        protected readonly bool _requireDb, _executeAlways;

        protected SQLiteConnection? _connection;
        #endregion

        public MigTaskBase(string version, bool requireDb = false, bool executeAlways = false)
        {
            _version = version;
            _requireDb = requireDb;
            _executeAlways = executeAlways;
        }

        #region Methods PUBLIC
        public void Execute()
        {
            string versionOldRaw = AppEnvironment.GetAppConfig().AppVersion;
            AppVersion currentVersion = AppEnvironment.AppVersion;
            if ((FnString.IsNullEmptyOrWhitespace(versionOldRaw) || !currentVersion.NeedsMigrationFrom(versionOldRaw, _version)) && !_executeAlways) return;

            EnsureDbOpen();

            ExecuteImpl();
        }
        #endregion

        #region Methods PROTECTED
        protected abstract void ExecuteImpl();
        #endregion

        #region Methods PRIVATE
        private void EnsureDbOpen()
        {
            if (!_requireDb) return;

            if (_connection is null)
                _connection = AppEnvironment.GetDataBaseManager().GetConnection();

            if (_connection.State != System.Data.ConnectionState.Open)
                _connection.Open();
        }
        #endregion
    }
}
