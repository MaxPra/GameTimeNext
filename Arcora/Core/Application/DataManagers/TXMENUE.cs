using System.Data.SQLite;

namespace Arcora.Core.Application.DataManagers
{
    public class TXMENUE : TXMENUEBasic
    {
        public TXMENUE()
        {
        }

        public TXMENUE(SQLiteConnection connection) : base(connection)
        {
        }
    }
}
