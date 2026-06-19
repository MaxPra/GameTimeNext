using GameTimeNext.Core.Framework.DataBase.Import.Base;
using GameTimeNext.Core.Framework.Logging;
using System.Globalization;
using UIX.ViewController.Engine.Querying;
using static GameTimeNext.Core.Framework.DataBase.Import.DataBaseImporter;

namespace GameTimeNext.Core.Framework.DataBase.Import
{
    public class DataBaseCodetablesImporter : DataBaseImporterBase
    {
        private readonly HashSet<string> _resetDeveloperTxtyps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

            int txtypIndex = header.IndexOf("TXTYP");
            int txnumIndex = header.IndexOf("TXNUM");

            if (txtypIndex < 0 || txnumIndex < 0)
                return;

            foreach (List<string> row in importFile.Rows)
            {
                if (row.Count <= Math.Max(txtypIndex, txnumIndex))
                    continue;

                UIXStatement uixStatement = new UIXStatement("T1CTABD", AppEnvironment.GetDataBaseManager().GetConnection());
                uixStatement.SetStatementType(UIXStatement.StatementType.INSERT);

                string txtyp = row[txtypIndex];
                string txnum = row[txnumIndex];
                string permission = GetPermission(txtyp);
                bool checkIfExists = permission == "U";

                if (permission == "D" && !_resetDeveloperTxtyps.Contains(txtyp))
                {
                    DeleteAllDeveloperEntrysT1CTABD(txtyp);
                    _resetDeveloperTxtyps.Add(txtyp);
                }

                if (permission == "D")
                    DeleteEntryT1CTABD(txtyp, txnum);

                for (int i = 0; i < header.Count; i++)
                {
                    string columnName = header[i];
                    string value = row[i];

                    if (IsDateColumn(columnName))
                        value = NormalizeDateToSqlite(value);

                    uixStatement.AddValue(columnName, value);
                }

                if (checkIfExists)
                {
                    uixStatement.SetInsertOnlyIfNotExists(true);
                    uixStatement.AddExistsWhere("TXTYP", QueryCompareType.EQUALS, txtyp);
                    uixStatement.AddExistsWhere("TXNUM", QueryCompareType.EQUALS, txnum);
                }

                string s = uixStatement.PreviewStatement();

                FnLog.AddInfo(null, s);

                uixStatement.ExecuteNonQuery();
            }
        }

        private void DeleteEntryT1CTABD(string txtyp, string txnum)
        {
            UIXStatement uixStatement = new UIXStatement("T1CTABD", AppEnvironment.GetDataBaseManager().GetConnection());
            uixStatement.SetStatementType(UIXStatement.StatementType.DELETE);

            uixStatement.AddWhere("TXTYP", QueryCompareType.EQUALS, txtyp);
            uixStatement.AddWhere("TXNUM", QueryCompareType.EQUALS, txnum);

            string s = uixStatement.PreviewStatement();
            FnLog.AddInfo(null, s);

            uixStatement.ExecuteNonQuery();
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

                    if (IsDateColumn(columnName))
                        value = NormalizeDateToSqlite(value);

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

        private static bool IsDateColumn(string columnName)
        {
            return string.Equals(columnName, "CRAT", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(columnName, "CHAT", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDateToSqlite(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return rawValue;

            DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces;
            string[] formats =
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss.FFFFFFF",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
                "dd.MM.yyyy HH:mm:ss",
                "dd.MM.yyyy HH:mm",
                "o"
            };

            if (DateTime.TryParseExact(rawValue, formats, CultureInfo.InvariantCulture, styles, out DateTime exactResult) ||
                DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, styles, out exactResult))
            {
                return exactResult.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            return rawValue;
        }
    }
}
