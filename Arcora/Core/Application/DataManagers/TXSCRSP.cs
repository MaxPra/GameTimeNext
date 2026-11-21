using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXSCRSP : TXSCRSPBasic
    {
        public TXSCRSP()
        {
        }

        public TXSCRSP(SQLiteConnection connection) : base(connection)
        {
        }

        public void DeleteAllWhereSSID(long ssid)
        {
            if (ssid == 0)
                return;

            using SQLiteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM T1SCRSP WHERE SSID = @SSID;";
            cmd.Parameters.AddWithValue("@SSID", ssid);
            cmd.ExecuteNonQuery();
        }
    }
}
