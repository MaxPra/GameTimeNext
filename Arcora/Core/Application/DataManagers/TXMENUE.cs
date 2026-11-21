using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
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
