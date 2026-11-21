using System.Data.SQLite;

namespace Arcora.Core.Application.DataManagers
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
