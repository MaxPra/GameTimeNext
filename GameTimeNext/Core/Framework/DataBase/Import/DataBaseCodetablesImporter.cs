using GameTimeNext.Core.Framework.DataBase.Import.Base;
using GameTimeNext.Core.Framework.Logging;
using UIX.ViewController.Engine.Querying;
using static GameTimeNext.Core.Framework.DataBase.Import.DataBaseImporter;

namespace GameTimeNext.Core.Framework.DataBase.Import
{
    public class DataBaseCodetablesImporter : DataBaseImporterBase
    {
        public override List<string> GetValidTables()
        {
            return new List<string>
            {
                "T1CTABD",
                "T1CTABH"
            };
        }

        public override void Import(ImportFile importFile)
        {

            FnLog.AddInfo(null, "*** Codetable-Import START ***");

            if (importFile.TableName == "T1CTABH")
                ImportT1CTABH(importFile);

            if (importFile.TableName == "T1CTABD")
                ImportT1CTABD(importFile);

            FnLog.AddInfo(null, "*** Codetable-Import END ***");
        }

        private void ImportT1CTABH(ImportFile importFile)
        {
            // Alle T1CTABH Einträge löschen
            DeleteAllEntrysT1CTABH();

            // Neue Einträge importieren
            ImportT1CTABHEntrys(importFile);
        }

        private void ImportT1CTABD(ImportFile importFile)
        {
            List<string> header = importFile.Header;

            bool checkIfExists = false;
            string txtypBefore = string.Empty;

            foreach (List<string> row in importFile.Rows)
            {
                UIXStatement uixStatement = new UIXStatement("T1CTABD", AppEnvironment.GetDataBaseManager().GetConnection());
                uixStatement.SetStatementType(UIXStatement.StatementType.INSERT);

                checkIfExists = false;

                for (int i = 0; i < header.Count; i++)
                {
                    string columnName = header[i];
                    string value = row[i];

                    if (columnName == "TXTYP")
                        checkIfExists = GetPermission(value) == "U";

                    if (columnName == "TXTYP" && GetPermission(value) == "D" && txtypBefore != value)
                    {
                        DeleteAllDeveloperEntrysT1CTABD(value);
                        txtypBefore = value;
                    }

                    uixStatement.AddValue(columnName, value);
                }

                if (checkIfExists)
                {
                    uixStatement.SetInsertOnlyIfNotExists(true);
                    uixStatement.AddExistsWhere("TXNUM", QueryCompareType.EQUALS, row[1]);
                }

                string s = uixStatement.PreviewStatement();
                string pref = string.Empty;

                if (checkIfExists)
                    pref = "|E| ";

                FnLog.AddInfo(null, pref + s);

                uixStatement.ExecuteNonQuery();
            }
        }

        private void ImportT1CTABHEntrys(ImportFile importFile)
        {
            List<string> header = importFile.Header;

            foreach (List<string> row in importFile.Rows)
            {
                UIXStatement uixStatement = new UIXStatement("T1CTABH", AppEnvironment.GetDataBaseManager().GetConnection());
                uixStatement.SetStatementType(UIXStatement.StatementType.INSERT);

                string txtyp = string.Empty;

                for (int i = 0; i < header.Count; i++)
                {
                    string columnName = header[i];
                    string value = row[i];

                    if (columnName == "TXTYP")
                        txtyp = value;

                    uixStatement.AddValue(columnName, value);
                }

                string s = uixStatement.PreviewStatement();
                FnLog.AddInfo(null, s);

                uixStatement.ExecuteNonQuery();
            }
        }

        private void DeleteAllEntrysT1CTABH()
        {
            UIXStatement uixStatement = new UIXStatement("T1CTABH", AppEnvironment.GetDataBaseManager().GetConnection());

            uixStatement.SetStatementType(UIXStatement.StatementType.DELETE);

            string s = uixStatement.PreviewStatement();
            FnLog.AddInfo(null, s);

            uixStatement.ExecuteNonQuery();
        }

        private void DeleteAllDeveloperEntrysT1CTABD(string txtyp)
        {
            UIXStatement uixStatement = new UIXStatement("T1CTABD", AppEnvironment.GetDataBaseManager().GetConnection());
            uixStatement.SetStatementType(UIXStatement.StatementType.DELETE);

            uixStatement.AddWhere("TXTYP", QueryCompareType.EQUALS, txtyp);

            string s = uixStatement.PreviewStatement();
            FnLog.AddInfo(null, s);

            uixStatement.ExecuteNonQuery();
        }

        private string GetPermission(string txtyp)
        {
            UIXQuery query = new UIXQuery("T1CTABH", AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField("T1CTABH", "PERMI");

            query.AddWhere("T1CTABH", "TXTYP", QueryCompareType.EQUALS, txtyp);

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    string permission = UIXQuery.GetString(reader, "T1CTABH", "PERMI", string.Empty);

                    return permission;
                }
            }

            return null!;
        }
    }
}
