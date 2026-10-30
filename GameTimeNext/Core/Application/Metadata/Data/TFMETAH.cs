using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase.Migration;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public static class TFMETAH
    {
        /// <summary>
        /// Metadata headers matching the filters (empty = no filter), ordered by name, at most 1000.
        /// </summary>
        public static List<T1METAH> GetMetadata(string menam, string descr, string mtype)
        {
            UIXQuery query = new UIXQuery(K1METAH.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1METAH.Name, K1METAH.Fields.MENAM);

            if (!FnString.IsNullEmptyOrWhitespace(menam))
                query.AddWhere(K1METAH.Name, K1METAH.Fields.MENAM, QueryCompareType.LIKE, menam);

            if (!FnString.IsNullEmptyOrWhitespace(descr))
                query.AddWhere(K1METAH.Name, K1METAH.Fields.DESCR, QueryCompareType.LIKE, descr);

            if (!FnString.IsNullEmptyOrWhitespace(mtype))
                query.AddWhere(K1METAH.Name, K1METAH.Fields.MTYPE, QueryCompareType.EQUALS, mtype);

            query.SetTopX(1000);
            query.AddOrderBy(K1METAH.Name, K1METAH.Fields.MENAM, OrderDirection.ASC);

            List<T1METAH> t1metahs = new List<T1METAH>();
            TXMETAH txmetah = new TXMETAH();

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    T1METAH? t1metah = txmetah.Read(UIXQuery.GetString(reader, K1METAH.Name, K1METAH.Fields.MENAM, string.Empty));

                    if (t1metah is not null)
                        t1metahs.Add(t1metah);
                }
            }

            return t1metahs;
        }

        public static void DeleteMetadataAndLinkedData(T1METAH t1metah, bool deleteTable)
        {
            if (t1metah == null)
                throw new ArgumentNullException(nameof(t1metah));

            TXMETAP txmetap = new TXMETAP();
            List<T1METAP> positions = txmetap
                .ReadAll()
                .Where(x => string.Equals(x.MENAM, t1metah.MENAM, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (T1METAP pos in positions)
            {
                txmetap.Delete(pos.MENAM, pos.PONAM);
            }

            new TXMETAH().Delete(t1metah.MENAM);

            MigrationFactory.FromCsv.MigrateTables(MigrationFactory.ImportType.MetadataGenerator);
        }
    }
}
