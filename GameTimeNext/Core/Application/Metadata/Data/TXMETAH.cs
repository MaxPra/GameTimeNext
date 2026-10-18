using System.Data.SQLite;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public class TXMETAH : TXMETAHBasic
    {
        public TXMETAH()
        {
        }

        public TXMETAH(SQLiteConnection connection) : base(connection)
        {
        }
    }
}
