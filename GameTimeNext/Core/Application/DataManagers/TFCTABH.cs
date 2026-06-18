using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFCTABH
    {
        public static void DeleteCodetableAndEntries(T1CTABH t1ctabh)
        {
            // Einträge
            new TXCTABD().DeleteAllEntries(t1ctabh.TXTYP);

            // Codetabelle
            new TXCTABH().Delete(t1ctabh.TXTYP);
        }
    }
}
