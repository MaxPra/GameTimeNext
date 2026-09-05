namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        // OFDOI: MigrationFactory
        // Store current schema in a file (maybe in DevSync directory)
        // Methods for:
        //   - Autogenerating new CreateDB-SQL
        //   - Autogenerating new Migration-SQLs
        //   - Applying changes from DevSync the same way, it gets applied while migrating (use same methods -> centralized)
        // Attention:
        //   - Maybe add default values in Metadata (true/false required for bools)
        //   - When done, move into UIX Library (Utils)

        private const string _SQL_TRANSACTION_BEGIN = "BEGIN TRANSACTION;";
        private const string _SQL_TRANSACTION_COMMIT = "COMMIT;";
        private const string _SQL_TRANSACTION_ROLLBACK = "ROLLBACK;";
        private const string _SQL_PRIMARYKEY_OFF = "PRAGMA foreign_keys = OFF;";
        private const string _SQL_PRIMARYKEY_ON = "PRAGMA foreign_keys = ON;";
    }
}
