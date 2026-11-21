using System.Data.SQLite;

namespace Arcora.Core.Application.DataManagers
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
