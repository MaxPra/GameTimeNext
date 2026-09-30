using System.Data.SQLite;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXGRPPO : TXGRPPOBasic
    {
        public void DeleteAllWherePFID(long pfid)
        {
            if (pfid == 0)
                return;

            using (SQLiteCommand cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM T1GRPPO WHERE PFID = @PFID;";
                cmd.Parameters.AddWithValue("@PFID", pfid);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
