using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using UIX.ViewController.Engine.Querying;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TFMENUE
    {
        /// <summary>
        /// All menu entries, ordered by parent and sort order.
        /// </summary>
        public static List<T1MENUE> GetAllMenuEntries()
        {
            UIXQuery query = new UIXQuery(K1MENUE.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEID);
            query.AddOrderBy(K1MENUE.Name, K1MENUE.Fields.MEPA, OrderDirection.ASC);
            query.AddOrderBy(K1MENUE.Name, K1MENUE.Fields.MEOR, OrderDirection.ASC);
            query.AddOrderBy(K1MENUE.Name, K1MENUE.Fields.MEID, OrderDirection.ASC);

            return ReadEntries(query);
        }

        /// <summary>
        /// Direct children of a menu entry (0 = root), ordered by sort order.
        /// </summary>
        public static List<T1MENUE> GetChildren(long mepa)
        {
            UIXQuery query = new UIXQuery(K1MENUE.Name, AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField(K1MENUE.Name, K1MENUE.Fields.MEID);
            query.AddWhere(K1MENUE.Name, K1MENUE.Fields.MEPA, QueryCompareType.EQUALS, mepa);
            query.AddOrderBy(K1MENUE.Name, K1MENUE.Fields.MEOR, OrderDirection.ASC);
            query.AddOrderBy(K1MENUE.Name, K1MENUE.Fields.MEID, OrderDirection.ASC);

            return ReadEntries(query);
        }

        /// <summary>
        /// Sort order for a new entry: behind the last child of the parent.
        /// </summary>
        public static long GetNextSortOrder(long mepa)
        {
            List<T1MENUE> children = GetChildren(mepa);

            return children.Count == 0 ? 10 : children.Max(c => c.MEOR) + 10;
        }

        /// <summary>
        /// Deletes the menu entry and, for folders, everything below it.
        /// </summary>
        public static void DeleteWithChildren(long meid)
        {
            foreach (T1MENUE child in GetChildren(meid))
                DeleteWithChildren(child.MEID);

            new TXMENUE().Delete(meid);
        }

        /// <summary>
        /// Swaps the sort order of the entry with its previous (direction -1) or next (direction 1) sibling.
        /// Returns false if there is no sibling in that direction.
        /// </summary>
        public static bool Move(T1MENUE t1menue, int direction)
        {
            List<T1MENUE> siblings = GetChildren(t1menue.MEPA);
            int index = siblings.FindIndex(s => s.MEID == t1menue.MEID);
            int otherIndex = index + direction;

            if (index < 0 || otherIndex < 0 || otherIndex >= siblings.Count)
                return false;

            // Renumber all siblings so equal or missing sort orders can't block the swap
            (siblings[index], siblings[otherIndex]) = (siblings[otherIndex], siblings[index]);

            TXMENUE txmenue = new TXMENUE();

            for (int i = 0; i < siblings.Count; i++)
            {
                long sortOrder = (i + 1) * 10;

                if (siblings[i].MEOR == sortOrder)
                    continue;

                siblings[i].MEOR = sortOrder;
                txmenue.Save(siblings[i]);
            }

            return true;
        }

        private static List<T1MENUE> ReadEntries(UIXQuery query)
        {
            List<T1MENUE> t1menues = new List<T1MENUE>();

            TXMENUE txmenue = new TXMENUE();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long meid = UIXQuery.GetInt64(reader, K1MENUE.Name, K1MENUE.Fields.MEID);

                    T1MENUE? t1menue = txmenue.Read(meid);

                    if (t1menue != null)
                        t1menues.Add(t1menue);
                }
            }

            return t1menues;
        }
    }
}
