using System.Data.SQLite;

namespace Arcora.Core.Application.Metadata.Data
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
