using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXLNGAM : TXLNGAMBasic
    {
        public TXLNGAM()
        {
        }

        public TXLNGAM(SQLiteConnection connection) : base(connection)
        {
        }
    }
}
