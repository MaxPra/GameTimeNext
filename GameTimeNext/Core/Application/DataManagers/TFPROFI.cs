using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFPROFI
    {
        /// <summary>
        /// Löscht das übergebene Profil inkl. der zugehörigen Daten
        /// </summary>
        public static void DeleteProfiAndLinkedData(T1PROFI t1profi)
        {
            TXPROFI txprofi = new TXPROFI();

            // Zugehörige Daten löschen
            TXGRPPO txgrppo = new TXGRPPO();
            List<T1GRPPO> t1grppos = txgrppo.ReadAll();

            t1grppos = t1grppos.Where(g => g.PFID == t1profi.PFID).ToList();

            foreach (T1GRPPO t1grppo in t1grppos)
            {
                txgrppo.Delete(t1grppo.GPID);
            }

            // Profil löschen
            txprofi.Delete(t1profi.PFID);
        }

        public static bool GetIsUnplayed(T1PROFI t1profi)
        {
            return t1profi.FIPL == DateTime.MinValue;
        }
    }
}
