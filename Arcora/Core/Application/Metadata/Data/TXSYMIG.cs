using System.Data.SQLite;

namespace Arcora.Core.Application.Metadata.Data
{
    public class TXSYMIG : TXSYMIGBasic
    {
        public TXSYMIG()
        {
        }

        public TXSYMIG(SQLiteConnection connection) : base(connection)
        {
        }
    }
}
