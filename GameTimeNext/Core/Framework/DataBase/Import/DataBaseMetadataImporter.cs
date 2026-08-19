using GameTimeNext.Core.Application.Metadata;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.DataBase.Import.Base;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;
using static GameTimeNext.Core.Framework.DataBase.Import.DataBaseImporter;

namespace GameTimeNext.Core.Framework.DataBase.Import
{
    public sealed class DataBaseMetadataImporter : DataBaseImporterBase
    {
        public override List<string> GetValidTables()
        {
            return new List<string>
            {
                "T1METAH",
                "T1METAP"
            };
        }

        public override void Import(ImportFile importFile)
        {
            if (importFile.TableName == "T1METAH")
                ImportT1METAH(importFile);

            if (importFile.TableName == "T1METAP")
                ImportT1METAP(importFile);

            SyncMetadataTables();
        }

        private void ImportT1METAH(ImportFile importFile)
        {
            HashSet<string> existingMenams = new TXMETAH()
                .ReadAll()
                .Select(x => x.MENAM)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            DeleteAll("T1METAH");

            HashSet<string> importedMenams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (List<string> row in importFile.Rows)
            {
                UIXStatement statement = new UIXStatement("T1METAH", AppEnvironment.GetDataBaseManager().GetConnection());
                statement.SetStatementType(UIXStatement.StatementType.INSERT);

                for (int i = 0; i < importFile.Header.Count && i < row.Count; i++)
                {
                    string columnName = importFile.Header[i].Trim();
                    string value = row[i];
                    statement.AddValue(columnName, value);

                    if (string.Equals(columnName, "MENAM", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
                        importedMenams.Add(value.Trim());
                }

                statement.ExecuteNonQuery();
            }

            CFMetadataTableGenerator generator = new CFMetadataTableGenerator();
            IEnumerable<string> removedMenams = existingMenams.Where(x => !importedMenams.Contains(x));
            foreach (string removed in removedMenams)
            {
                generator.DeleteTable(removed);
            }
        }

        private void ImportT1METAP(ImportFile importFile)
        {
            DeleteAll("T1METAP");

            foreach (List<string> row in importFile.Rows)
            {
                UIXStatement statement = new UIXStatement("T1METAP", AppEnvironment.GetDataBaseManager().GetConnection());
                statement.SetStatementType(UIXStatement.StatementType.INSERT);

                for (int i = 0; i < importFile.Header.Count && i < row.Count; i++)
                {
                    string columnName = importFile.Header[i].Trim();
                    string value = NormalizeMetadataValue(columnName, row[i]);
                    statement.AddValue(columnName, value);
                }

                statement.ExecuteNonQuery();
            }
        }

        private static void SyncMetadataTables()
        {
            TXMETAH txmetah = new TXMETAH();
            TXMETAP txmetap = new TXMETAP();
            CFMetadataTableGenerator generator = new CFMetadataTableGenerator();

            HashSet<string> menamsWithPositions = txmetap
                .ReadAll()
                .Select(x => x.MENAM)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (T1METAH header in txmetah.ReadAll())
            {
                if (!menamsWithPositions.Contains(header.MENAM))
                    continue;

                generator.EnsureTableFor(header);
            }
        }

        private static void DeleteAll(string tableName)
        {
            UIXStatement statement = new UIXStatement(tableName, AppEnvironment.GetDataBaseManager().GetConnection());
            statement.SetStatementType(UIXStatement.StatementType.DELETE);
            statement.ExecuteNonQuery();
        }

        private static string NormalizeMetadataValue(string columnName, string rawValue)
        {
            if (!string.Equals(columnName, K1METAP.Fields.DATYP, StringComparison.OrdinalIgnoreCase))
                return rawValue;

            if (string.IsNullOrWhiteSpace(rawValue))
                return rawValue;

            string normalized = rawValue.Trim();

            UIXSQLiteDataTypes.DataTypeDefinition? byKey = UIXSQLiteDataTypes.GetDefinitionByKey(normalized);
            if (byKey != null)
                return byKey.Key;

            UIXSQLiteDataTypes.DataTypeDefinition? byTextOrType = UIXSQLiteDataTypes
                .GetDefinitions()
                .FirstOrDefault(x =>
                    string.Equals(x.Text, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.CSharpType, normalized, StringComparison.OrdinalIgnoreCase));

            return byTextOrType?.Key ?? normalized;
        }
    }
}
