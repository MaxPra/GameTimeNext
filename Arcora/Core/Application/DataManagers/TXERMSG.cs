using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXERMSG : TXERMSGBasic
    {
        public TXERMSG()
        {
        }

        public TXERMSG(SQLiteConnection connection) : base(connection)
        {
        }
    }
}
