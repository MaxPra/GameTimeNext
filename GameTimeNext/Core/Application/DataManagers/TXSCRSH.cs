using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSCRSH : TXSCRSHBasic
    {
        public TXSCRSH()
        {
        }

        public TXSCRSH(SQLiteConnection connection) : base(connection)
        {
        }
    }
}
