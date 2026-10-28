using GameTimeNext.Core.Application.TableObjects;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFAPRDH
    {
        /// <summary>
        /// Application properties definitions that may be called from a menu (APMCP), ordered by ID.
        /// </summary>
        public static List<T1APRDH> GetMenuCallableApplications()
        {
            return new TXAPRDH().ReadAll()
                .Where(a => a.APMCP)
                .OrderBy(a => a.APDID, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
