using GameTimeNext.Core.Framework.DataBase.Migration;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    /// <summary>
    /// Removes the legacy foreign keys (ON DELETE CASCADE) of databases created by the old DataBaseManager.CreateTables():
    /// T1SESSI.PFID -> T1PROFI, T1GRPPO.PFID -> T1PROFI, T1GRPPO.GRID -> T1GROUP.
    /// The metadata-driven schema defines no foreign keys; linked data is deleted in code (TFPROFI.DeleteT1PROFIAndLinkedData).
    /// </summary>
    internal class MigTask_100b_009 : MigTaskBase
    {
        public MigTask_100b_009() : base("1.0.0", requireDb: true)
        {

        }

        protected override void ExecuteImpl()
        {
            RemoveLegacyForeignKeys("T1SESSI");
            RemoveLegacyForeignKeys("T1GRPPO");
        }

        private void RemoveLegacyForeignKeys(string tableName)
        {
            if (_connection is null || !HasForeignKeys(tableName)) return;

            MigrationFactory.RebuildTableFromMetadata(tableName, _connection);
        }

        private bool HasForeignKeys(string tableName)
        {
            using var reader = UIXQuery.QueryCustom($"PRAGMA foreign_key_list({tableName});", _connection!);
            return reader.Read();
        }
    }
}
