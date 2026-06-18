using static GameTimeNext.Core.Framework.DataBase.Import.DataBaseImporter;

namespace GameTimeNext.Core.Framework.DataBase.Import.Base
{
    public abstract class DataBaseImporterBase
    {
        public abstract List<string> GetValidTables();

        public abstract void Import(ImportFile importFile);
    }
}
