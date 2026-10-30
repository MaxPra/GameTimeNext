using System.Data.SQLite;
using System.Globalization;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.MigrationTasks
{
    /// <summary>
    /// The metadata type (T1METAH.MTYPE) is a key of codetable mT now: the former fixed type "mE" becomes "TO" (Table Object).
    /// Runs on every start (only touches rows that weren't migrated yet), because older import packages still bring "mE".
    /// </summary>
    internal class MigTask_100b_011 : MigTaskBase
    {
        public MigTask_100b_011() : base("1.0.0", requireDb: true, executeAlways: true)
        {

        }

        protected override void ExecuteImpl()
        {
            using SQLiteCommand command = _connection!.CreateCommand();
            command.CommandText = "UPDATE T1METAH SET MTYPE = 'TO' WHERE MTYPE = 'mE';";
            int migratedRows = command.ExecuteNonQuery();

            if (migratedRows > 0)
                FnLog.AddInfo(nameof(MigTask_100b_011), $"Metadata type of {migratedRows.ToString(CultureInfo.InvariantCulture)} table definition(s) changed from mE to TO");
        }
    }
}
