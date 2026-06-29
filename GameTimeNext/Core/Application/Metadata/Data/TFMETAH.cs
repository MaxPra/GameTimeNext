using GameTimeNext.Core.Application.Metadata;

namespace GameTimeNext.Core.Application.Metadata.Data
{
    public static class TFMETAH
    {
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

            if (deleteTable)
            {
                CFMetadataTableGenerator tableGenerator = new CFMetadataTableGenerator();
                tableGenerator.DeleteTableFor(t1metah);
            }

            new TXMETAH().Delete(t1metah.MENAM);
        }
    }
}
