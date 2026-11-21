using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXAPRDH : TXAPRDHBasic
    {
        public TXAPRDH()
        {
        }

        public TXAPRDH(SQLiteConnection connection) : base(connection)
        {
        }
    }
}
