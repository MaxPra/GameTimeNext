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
        private readonly HashSet<string> _resetNumberRangeTxtyps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                string txtyp = row[txtypIndex].Trim();
                string txnum = row[txnumIndex].Trim();
                string permission = (GetPermission(txtyp) ?? string.Empty).Trim();
                bool isNumberRangeActive = IsNumberRangeActive(txtyp);
                bool isDeveloperNumberRangeEntry = txnum.StartsWith("D_", StringComparison.OrdinalIgnoreCase);
                bool isUpdateOnlyPermission = string.Equals(permission, "U", StringComparison.OrdinalIgnoreCase);
                bool isDeveloperPermission = string.Equals(permission, "D", StringComparison.OrdinalIgnoreCase);

                if (isNumberRangeActive && !_resetNumberRangeTxtyps.Contains(txtyp))
                {
                    DeleteAllNumberRangeEntrysT1CTABD(txtyp);
                    _resetNumberRangeTxtyps.Add(txtyp);
                }

                if (isNumberRangeActive && !isDeveloperNumberRangeEntry)
                    continue;

                if (isUpdateOnlyPermission && !isNumberRangeActive && ExistsEntryByTxtypT1CTABD(txtyp))
                    continue;

                if (isDeveloperPermission && !_resetDeveloperTxtyps.Contains(txtyp))
                {
                    DeleteAllDeveloperEntrysT1CTABD(txtyp);

                    _resetDeveloperTxtyps.Add(txtyp);
                }

                if (isDeveloperPermission)
                    DeleteEntryT1CTABD(txtyp, txnum);

                for (int i = 0; i < header.Count; i++)
                {
                    string columnName = header[i];
                    string value = row[i];

                    if (string.Equals(columnName, "TXTYP", StringComparison.OrdinalIgnoreCase))
                        value = txtyp;

                    if (string.Equals(columnName, "TXNUM", StringComparison.OrdinalIgnoreCase))
                        value = txnum;

                    if (IsDateColumn(columnName))
                        value = NormalizeDateToSqlite(value);

                    uixStatement.AddValue(columnName, value);
                }

                if (ExistsEntryT1CTABD(txtyp, txnum))
                    continue;

                string s = uixStatement.PreviewStatement();

                FnLog.AddInfo(null, s);

                uixStatement.ExecuteNonQuery();
            }
        }

        private bool ExistsEntryByTxtypT1CTABD(string txtyp)
        {
            UIXQuery query = new UIXQuery("T1CTABD", AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField("T1CTABD", "TXTYP");
            query.AddWhere("T1CTABD", "TXTYP", QueryCompareType.EQUALS, txtyp);

            using (var reader = query.Execute())
            {
                return reader.Read();
            }
        }

        private bool ExistsEntryT1CTABD(string txtyp, string txnum)
        {
            UIXQuery query = new UIXQuery("T1CTABD", AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField("T1CTABD", "TXTYP");
            query.AddWhere("T1CTABD", "TXTYP", QueryCompareType.EQUALS, txtyp);
            query.AddWhere("T1CTABD", "TXNUM", QueryCompareType.EQUALS, txnum);

            using (var reader = query.Execute())
            {
                return reader.Read();
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

        private void DeleteAllNumberRangeEntrysT1CTABD(string txtyp)
        {
            using var command = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand();
            command.CommandText = "DELETE FROM T1CTABD WHERE TXTYP = @TXTYP AND substr(TXNUM, 1, 2) = 'D_'";
            var txtypParameter = command.CreateParameter();
            txtypParameter.ParameterName = "@TXTYP";
            txtypParameter.Value = txtyp;
            command.Parameters.Add(txtypParameter);

            FnLog.AddInfo(null, $"DELETE FROM T1CTABD WHERE TXTYP = '{txtyp}' AND substr(TXNUM, 1, 2) = 'D_'");

            command.ExecuteNonQuery();
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

        private bool IsNumberRangeActive(string txtyp)
        {
            UIXQuery query = new UIXQuery("T1CTABH", AppEnvironment.GetDataBaseManager().GetConnection());
            query.AddField("T1CTABH", "NRANA");
            query.AddWhere("T1CTABH", "TXTYP", QueryCompareType.EQUALS, txtyp);

            using (var reader = query.Execute())
            {
                if (reader.Read())
                {
                    string nranaRaw = UIXQuery.GetString(reader, "T1CTABH", "NRANA", "0");
                    int.TryParse(nranaRaw, out int nrana);

                    return nrana == 1;
                }
            }

            return false;
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
