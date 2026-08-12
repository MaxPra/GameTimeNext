using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.Config;
using System.IO;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Metadata
{
    public sealed class CFMetadataClassGenerator
    {
        public void GenerateFor(T1METAH t1metah)
        {
            string projectRoot = ResolveProjectRoot();
            string tableObjectPath = Path.Combine(projectRoot, "Core", "Application", "TableObjects");
            string dataManagerPath = Path.Combine(projectRoot, "Core", "Application", "DataManagers");

            GenerateFor(t1metah, tableObjectPath, dataManagerPath);
        }

        public void GenerateFor(T1METAH t1metah, string outputRootPath)
        {
            if (string.IsNullOrWhiteSpace(outputRootPath))
                throw new ArgumentException("Output path is required.", nameof(outputRootPath));

            string tableObjectPath = Path.Combine(outputRootPath, "TableObjects");
            string dataManagerPath = Path.Combine(outputRootPath, "DataManagers");

            GenerateFor(t1metah, tableObjectPath, dataManagerPath);
        }

        public void GenerateFor(T1METAH t1metah, string tableObjectPath, string dataManagerPath)
        {
            if (t1metah == null)
                throw new ArgumentNullException(nameof(t1metah));

            string tableName = NormalizeName(t1metah.MENAM);
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException("MENAM is required for metadata class generation.");

            string suffix = BuildClassSuffix(tableName);

            List<T1METAP> positions = new TXMETAP()
                .ReadAll()
                .Where(x => string.Equals(x.MENAM, t1metah.MENAM, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.PORDE)
                .ThenBy(x => x.PONAM)
                .ToList();

            if (positions.Count == 0)
                throw new InvalidOperationException($"No metadata positions found for '{t1metah.MENAM}'.");

            List<GeneratedField> fields = BuildFields(positions);

            GenerateT1Class(tableObjectPath, suffix, fields, t1metah.DSYNC);
            GenerateK1Class(tableObjectPath, tableName, suffix, fields);
            GenerateTXBasicClass(dataManagerPath, suffix, tableName, fields);
            GenerateTXClass(dataManagerPath, suffix);
        }

        private static List<GeneratedField> BuildFields(List<T1METAP> positions)
        {
            List<GeneratedField> fields = new List<GeneratedField>();
            foreach (T1METAP position in positions)
            {
                string csharpType = UIXSQLiteDataTypes.NormalizeCSharpType(position.DATYP);
                string sqliteType = UIXSQLiteDataTypes.FromCSharp(position.DATYP);

                if (sqliteType == UIXSQLiteDataTypes.Text && position.DALEN > 0)
                    sqliteType = $"VARCHAR({position.DALEN})";

                fields.Add(new GeneratedField
                {
                    Metadata = position,
                    SqliteType = sqliteType,
                    CSharpType = csharpType,
                    IsPrimaryKey = position.PRIMK,
                    IsAutoIncrement = position.AUTOI
                });
            }

            return fields;
        }

        private static string ResolveProjectRoot()
        {
            string baseDirectory = AppContext.BaseDirectory;
            DirectoryInfo? directory = new DirectoryInfo(baseDirectory);

            while (directory != null)
            {
                string csprojPath = Path.Combine(directory.FullName, AppConfig.Root.ApplicationName + ".csproj");
                if (File.Exists(csprojPath))
                    return directory.FullName;

                directory = directory.Parent;
            }

            return Directory.GetCurrentDirectory();
        }

        private static void GenerateT1Class(string tableObjectPath, string suffix, List<GeneratedField> fields, bool devSync)
        {
            string className = $"T1{suffix}";

            UIXCodeGenerator code = new UIXCodeGenerator();
            code.AppendLine($"using {AppConfig.Root.ApplicationName}.Core.Application.DataManagers;");
            code.AppendLine("using UIX.ViewController.Engine.DataBaseObjects;");
            code.AppendEmptyLine();
            code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.TableObjects");
            code.BeginBlock($"public class {className} : UIXTableObjectBase");
            code.AppendLine($"public override bool IsDevSynced => {(devSync ? "true" : "false")};");
            code.AppendEmptyLine();

            for (int i = 0; i < fields.Count; i++)
            {
                GeneratedField field = fields[i];
                code.AppendLine($"[UIXSignatureField({i})]");
                code.AppendLine($"public {field.CSharpType} {NormalizeName(field.Metadata.PONAM)} {{ get; set; }} = {GetDefaultValue(field.CSharpType)};");
                code.AppendEmptyLine();
            }

            code.BeginBlock($"public override void Save()");
            code.AppendLine($"new TX{suffix}().Save(this);");
            code.EndBlock();
            code.EndBlock();
            code.EndBlock();

            string outputPath = Path.Combine(tableObjectPath, className + ".cs");
            code.SaveToFile(outputPath);
        }

        private static void GenerateK1Class(string tableObjectPath, string tableName, string suffix, List<GeneratedField> fields)
        {
            string className = $"K1{suffix}";

            UIXCodeGenerator code = new UIXCodeGenerator();
            code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.TableObjects");
            code.BeginBlock($"public static class {className}");
            code.AppendLine($"public const string Name = \"{tableName}\";");
            code.AppendEmptyLine();
            code.BeginBlock("public static class Fields");

            foreach (GeneratedField field in fields)
            {
                string fieldName = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"public const string {fieldName} = \"{fieldName}\";");
            }

            code.EndBlock();
            code.EndBlock();
            code.EndBlock();

            string outputPath = Path.Combine(tableObjectPath, className + ".cs");
            code.SaveToFile(outputPath);
        }

        private static void GenerateTXBasicClass(string dataManagerPath, string suffix, string tableName, List<GeneratedField> fields)
        {
            string className = $"TX{suffix}Basic";
            string t1ClassName = $"T1{suffix}";
            string k1ClassName = $"K1{suffix}";

            List<GeneratedField> primaryKeys = fields.Where(x => x.IsPrimaryKey).ToList();
            List<GeneratedField> nonPrimary = fields.Where(x => !x.IsPrimaryKey).ToList();
            GeneratedField? autoIncrementPrimaryKey = fields.FirstOrDefault(x => x.IsPrimaryKey && x.IsAutoIncrement);
            GeneratedField? createdAtField = fields.FirstOrDefault(x =>
                string.Equals(NormalizeName(x.Metadata.PONAM), "CRAT", StringComparison.OrdinalIgnoreCase) &&
                x.CSharpType == "DateTime");
            GeneratedField? changedAtField = fields.FirstOrDefault(x =>
                string.Equals(NormalizeName(x.Metadata.PONAM), "CHAT", StringComparison.OrdinalIgnoreCase) &&
                x.CSharpType == "DateTime");
            List<GeneratedField> insertFields = autoIncrementPrimaryKey == null
                ? fields
                : fields.Where(x => !ReferenceEquals(x, autoIncrementPrimaryKey)).ToList();

            UIXCodeGenerator code = new UIXCodeGenerator();
            code.AppendLine($"using {AppConfig.Root.ApplicationName}.Core.Application.TableObjects;");
            code.AppendLine($"using {AppConfig.Root.ApplicationName}.Core.Framework;");
            code.AppendLine($"using {AppConfig.Root.ApplicationName}.Core.Framework.DataBase.DevSync;");
            code.AppendLine("using System.Data.SQLite;");
            code.AppendLine("using System.Globalization;");
            code.AppendLine("using UIX.ViewController.Engine.DataBaseObjects;");
            code.AppendLine("using UIX.ViewController.Engine.Querying;");
            code.AppendEmptyLine();
            code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.DataManagers");
            code.BeginBlock($"public class {className}");

            code.BeginBlock($"public virtual {t1ClassName} CreateNew()");
            code.AppendLine($"{t1ClassName} obj = new {t1ClassName}();");
            code.AppendLine("obj.State = UIXTableObjectState.New;");
            code.AppendLine("return obj;");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"public virtual void Save({t1ClassName} obj)");
            code.AppendLine("if (obj == null)");
            code.AppendLine("    throw new ArgumentNullException(nameof(obj));");
            code.AppendEmptyLine();
            code.AppendLine("SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();");
            code.AppendLine("EnsureOpen(connection);");
            code.AppendEmptyLine();
            code.AppendLine("if (Exists(connection, obj))");
            code.AppendLine("    Update(connection, obj);");
            code.AppendLine("else");
            code.AppendLine("    Insert(connection, obj);");
            code.AppendEmptyLine();
            code.AppendLine("obj.State = UIXTableObjectState.Available;");
            code.AppendLine("obj.AcceptChanges();");
            code.AppendLine("DevSyncCsvSyncService.ExportTableFor(obj);");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"public virtual void Delete({BuildMethodParameters(primaryKeys)})");
            code.AppendLine("SQLiteConnection connection = AppEnvironment.GetDataBaseManager().GetConnection();");
            code.AppendLine("EnsureOpen(connection);");
            code.AppendEmptyLine();
            code.AppendLine("using SQLiteCommand cmd = connection.CreateCommand();");
            code.AppendLine($"cmd.CommandText = \"DELETE FROM {tableName} WHERE {BuildWhereClause(primaryKeys, "@")}\";");
            foreach (GeneratedField field in primaryKeys)
            {
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"cmd.Parameters.AddWithValue(\"@{name}\", {ToCamelCase(name)}); ");
            }
            code.AppendLine("cmd.ExecuteNonQuery();");
            code.AppendLine($"DevSyncCsvSyncService.ExportTable(\"{tableName}\");");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"public virtual {t1ClassName}? Read({BuildMethodParameters(primaryKeys)})");
            code.AppendLine($"UIXQuery query = new UIXQuery({k1ClassName}.Name, AppEnvironment.GetDataBaseManager().GetConnection());");
            foreach (GeneratedField field in fields)
            {
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"query.AddField({k1ClassName}.Name, {k1ClassName}.Fields.{name});");
            }
            foreach (GeneratedField field in primaryKeys)
            {
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"query.AddWhere({k1ClassName}.Name, {k1ClassName}.Fields.{name}, QueryCompareType.EQUALS, {ToCamelCase(name)});");
            }
            code.AppendEmptyLine();
            code.AppendLine("using var reader = query.Execute();");
            code.AppendLine("if (!reader.Read())");
            code.AppendLine("    return null;");
            code.AppendEmptyLine();
            code.AppendLine($"{t1ClassName} obj = Map(reader);");
            code.AppendLine("obj.AcceptChanges();");
            code.AppendLine("return obj;");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"public virtual List<{t1ClassName}> ReadAll()");
            code.AppendLine($"UIXQuery query = new UIXQuery({k1ClassName}.Name, AppEnvironment.GetDataBaseManager().GetConnection());");
            foreach (GeneratedField field in fields)
            {
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"query.AddField({k1ClassName}.Name, {k1ClassName}.Fields.{name});");
            }
            code.AppendLine($"query.AddOrderBy({k1ClassName}.Name, {k1ClassName}.Fields.{NormalizeName(fields[0].Metadata.PONAM)}, OrderDirection.ASC);");
            code.AppendEmptyLine();
            code.AppendLine($"List<{t1ClassName}> list = new List<{t1ClassName}>();");
            code.AppendLine("using var reader = query.Execute();");
            code.AppendLine("while (reader.Read())");
            code.BeginBlock(string.Empty);
            code.AppendLine($"{t1ClassName} obj = Map(reader);");
            code.AppendLine("obj.AcceptChanges();");
            code.AppendLine("list.Add(obj);");
            code.EndBlock();
            code.AppendLine("return list;");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"private void Insert(SQLiteConnection connection, {t1ClassName} obj)");
            code.AppendLine("using SQLiteCommand cmd = connection.CreateCommand();");
            if (createdAtField != null && changedAtField != null)
            {
                string createdAtName = NormalizeName(createdAtField.Metadata.PONAM);
                string changedAtName = NormalizeName(changedAtField.Metadata.PONAM);
                code.AppendLine("DateTime now = DateTime.Now;");
                code.AppendLine($"obj.{createdAtName} = now;");
                code.AppendLine($"obj.{changedAtName} = now;");
            }
            else
            {
                if (createdAtField != null)
                {
                    string createdAtName = NormalizeName(createdAtField.Metadata.PONAM);
                    code.AppendLine($"obj.{createdAtName} = DateTime.Now;");
                }

                if (changedAtField != null)
                {
                    string changedAtName = NormalizeName(changedAtField.Metadata.PONAM);
                    code.AppendLine($"obj.{changedAtName} = DateTime.Now;");
                }
            }
            code.AppendLine($"cmd.CommandText = \"INSERT INTO {tableName} ({string.Join(", ", insertFields.Select(x => NormalizeName(x.Metadata.PONAM)))}) VALUES ({string.Join(", ", insertFields.Select(x => "@" + NormalizeName(x.Metadata.PONAM)))})\";");
            foreach (GeneratedField field in insertFields)
            {
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"cmd.Parameters.AddWithValue(\"@{name}\", ToDbValue(obj.{name}));");
            }
            code.AppendLine("cmd.ExecuteNonQuery();");

            if (autoIncrementPrimaryKey != null)
            {
                string autoIncrementPkName = NormalizeName(autoIncrementPrimaryKey.Metadata.PONAM);
                code.AppendLine("using SQLiteCommand idCmd = connection.CreateCommand();");
                code.AppendLine("idCmd.CommandText = \"SELECT last_insert_rowid();\";");
                code.AppendLine($"obj.{autoIncrementPkName} = {BuildIdentityAssignmentExpression(autoIncrementPrimaryKey.CSharpType)};");
            }

            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"private void Update(SQLiteConnection connection, {t1ClassName} obj)");
            code.AppendLine("using SQLiteCommand cmd = connection.CreateCommand();");
            if (changedAtField != null)
            {
                string changedAtName = NormalizeName(changedAtField.Metadata.PONAM);
                code.AppendLine($"obj.{changedAtName} = DateTime.Now;");
            }
            code.AppendLine($"cmd.CommandText = \"UPDATE {tableName} SET {string.Join(", ", nonPrimary.Select(x => NormalizeName(x.Metadata.PONAM) + " = @" + NormalizeName(x.Metadata.PONAM)))} WHERE {BuildWhereClause(primaryKeys, "@")}\";");
            foreach (GeneratedField field in fields)
            {
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"cmd.Parameters.AddWithValue(\"@{name}\", ToDbValue(obj.{name}));");
            }
            code.AppendLine("cmd.ExecuteNonQuery();");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"private bool Exists(SQLiteConnection connection, {t1ClassName} obj)");
            code.AppendLine("using SQLiteCommand cmd = connection.CreateCommand();");
            code.AppendLine($"cmd.CommandText = \"SELECT COUNT(*) FROM {tableName} WHERE {BuildWhereClause(primaryKeys, "@")}\";");
            foreach (GeneratedField field in primaryKeys)
            {
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"cmd.Parameters.AddWithValue(\"@{name}\", ToDbValue(obj.{name}));");
            }
            code.AppendLine("return Convert.ToInt64(cmd.ExecuteScalar()) > 0;");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock($"protected static {t1ClassName} Map(SQLiteDataReader reader)");
            code.AppendLine($"{t1ClassName} obj = new {t1ClassName}();");
            for (int i = 0; i < fields.Count; i++)
            {
                GeneratedField field = fields[i];
                string name = NormalizeName(field.Metadata.PONAM);
                code.AppendLine($"obj.{name} = {BuildReaderExpression(field.CSharpType, i)};");
            }
            code.AppendLine("obj.State = UIXTableObjectState.Available;");
            code.AppendLine("return obj;");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock("private static object ToDbValue(object? value)");
            code.AppendLine("if (value is bool boolValue)");
            code.AppendLine("    return boolValue ? 1 : 0;");
            code.AppendLine("if (value is DateTime dt)");
            code.AppendLine("    return dt.ToString(\"yyyy-MM-dd HH:mm:ss\", CultureInfo.InvariantCulture);");
            code.AppendLine("return value ?? DBNull.Value;");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock("private static DateTime ParseDbDateTime(object? value)");
            code.AppendLine("if (value == null || value == DBNull.Value)");
            code.AppendLine("    return DateTime.MinValue;");
            code.AppendEmptyLine();
            code.AppendLine("string raw = value.ToString() ?? string.Empty;");
            code.AppendLine("if (string.IsNullOrWhiteSpace(raw))");
            code.AppendLine("    return DateTime.MinValue;");
            code.AppendEmptyLine();
            code.AppendLine("if (DateTime.TryParseExact(raw, \"yyyy-MM-dd HH:mm:ss\", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))");
            code.AppendLine("    return parsed;");
            code.AppendLine("if (DateTime.TryParseExact(raw, \"dd.MM.yyyy HH:mm:ss\", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))");
            code.AppendLine("    return parsed;");
            code.AppendLine("if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))");
            code.AppendLine("    return parsed;");
            code.AppendLine("if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))");
            code.AppendLine("    return parsed;");
            code.AppendEmptyLine();
            code.AppendLine("return DateTime.MinValue;");
            code.EndBlock();
            code.AppendEmptyLine();

            code.BeginBlock("protected static void EnsureOpen(SQLiteConnection connection)");
            code.AppendLine("if (connection.State != System.Data.ConnectionState.Open)");
            code.AppendLine("    connection.Open();");
            code.EndBlock();

            code.EndBlock();
            code.EndBlock();

            string outputPath = Path.Combine(dataManagerPath, className + ".cs");
            code.SaveToFile(outputPath);
        }

        private static void GenerateTXClass(string dataManagerPath, string suffix)
        {
            string className = $"TX{suffix}";
            string baseClassName = className + "Basic";

            string outputPath = Path.Combine(dataManagerPath, className + ".cs");
            if (File.Exists(outputPath))
                return;

            UIXCodeGenerator code = new UIXCodeGenerator();
            code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.DataManagers");
            code.BeginBlock($"public class {className} : {baseClassName}");
            code.EndBlock();
            code.EndBlock();

            code.SaveToFile(outputPath);
        }

        private static string BuildReaderExpression(string csharpType, int index)
        {
            if (csharpType == "string")
                return $"reader.IsDBNull({index}) ? string.Empty : reader.GetString({index})";

            if (csharpType == "bool")
                return $"!reader.IsDBNull({index}) && Convert.ToInt32(reader.GetValue({index})) == 1";

            if (csharpType == "DateTime")
                return $"ParseDbDateTime(reader.GetValue({index}))";

            if (csharpType == "long")
                return $"reader.IsDBNull({index}) ? 0 : Convert.ToInt64(reader.GetValue({index}))";

            if (csharpType == "int")
                return $"reader.IsDBNull({index}) ? 0 : Convert.ToInt32(reader.GetValue({index}))";

            if (csharpType == "double")
                return $"reader.IsDBNull({index}) ? 0d : Convert.ToDouble(reader.GetValue({index}), CultureInfo.InvariantCulture)";

            return $"reader.IsDBNull({index}) ? default : ({csharpType})reader.GetValue({index})";
        }

        private static string BuildMethodParameters(List<GeneratedField> primaryKeys)
        {
            if (primaryKeys.Count == 0)
                return string.Empty;

            return string.Join(", ", primaryKeys.Select(x => $"{x.CSharpType} {ToCamelCase(NormalizeName(x.Metadata.PONAM))}"));
        }

        private static string BuildWhereClause(List<GeneratedField> fields, string prefix)
        {
            if (fields.Count == 0)
                return "1 = 1";

            return string.Join(" AND ", fields.Select(x =>
            {
                string name = NormalizeName(x.Metadata.PONAM);
                return $"{name} = {prefix}{name}";
            }));
        }

        private static string GetDefaultValue(string csharpType)
        {
            if (csharpType == "string")
                return "string.Empty";

            if (csharpType == "DateTime")
                return "DateTime.MinValue";

            if (csharpType == "bool")
                return "false";

            if (csharpType == "double")
                return "0d";

            if (csharpType == "long" || csharpType == "int")
                return "0";

            return "default!";
        }

        private static string BuildIdentityAssignmentExpression(string csharpType)
        {
            if (csharpType == "int")
                return "Convert.ToInt32(idCmd.ExecuteScalar())";

            if (csharpType == "long")
                return "Convert.ToInt64(idCmd.ExecuteScalar())";

            return $"({csharpType})Convert.ChangeType(idCmd.ExecuteScalar(), typeof({csharpType}), CultureInfo.InvariantCulture)";
        }

        private static string NormalizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string cleaned = new string(input.Trim().Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
            if (string.IsNullOrWhiteSpace(cleaned))
                return string.Empty;

            if (char.IsDigit(cleaned[0]))
                cleaned = "_" + cleaned;

            return cleaned.ToUpperInvariant();
        }

        private static string BuildClassSuffix(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || tableName.Length <= 2)
                throw new InvalidOperationException("MENAM must contain at least 3 characters to build class names.");

            return tableName.Substring(2);
        }

        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        private sealed class GeneratedField
        {
            public T1METAP Metadata { get; set; } = new T1METAP();
            public string SqliteType { get; set; } = string.Empty;
            public string CSharpType { get; set; } = "string";
            public bool IsPrimaryKey { get; set; }
            public bool IsAutoIncrement { get; set; }
        }
    }
}
