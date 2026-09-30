using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.Config;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static partial class Metadata
        {
            public static class CodeGenerator
            {
                // OFDO: Add ability to overwrite existing files in solution
                // OFDO: Make a base class for TXBasic with generic methods

                public static void GenerateFor(T1METAH t1metah)
                {
                    if (t1metah is null) throw new ArgumentNullException(nameof(t1metah));

                    string menam = t1metah.MENAM;
                    if (FnString.IsNullEmptyOrWhitespace(menam)) throw new ArgumentNullException($"{nameof(t1metah)}.{nameof(t1metah.MENAM)}");

                    string suffix = menam.Substring(2);
                    List<T1METAP> t1metaps = new TXMETAP().ReadAll(menam).OrderBy(p => p.PORDE).ThenBy(p => p.PONAM).ToList();

                    if (t1metaps.Count == 0) throw new InvalidOperationException($"No metadata positions found for '{menam}'.");

                    Dictionary<T1METAP, SqliteDataType> fields = GenerateFields(t1metaps);
                    TableSchema tSchema = SchemaGenerator.GenerateSingleFromMetadata(t1metah.MENAM);
                    GenerateT1Class(suffix, tSchema, t1metah.DSYNC, fields);
                    GenerateK1Class(suffix, fields);
                    GenerateTXBasicClass(suffix, tSchema, fields);
                    GenerateTXClass(suffix);
                }

                private static Dictionary<T1METAP, SqliteDataType> GenerateFields(List<T1METAP> t1metaps)
                {
                    Dictionary<T1METAP, SqliteDataType> temp = new Dictionary<T1METAP, SqliteDataType>();

                    t1metaps.ForEach(t1metap =>
                    {
                        SqliteDataType dataType = SqliteDataType.GetByKey(t1metap.DATYP);
                        temp.Add(t1metap, dataType);
                    });

                    return temp;
                }

                private static void GenerateT1Class(string suffix, TableSchema tSchema, bool devSync, Dictionary<T1METAP, SqliteDataType> fields)
                {
                    string className = $"T1{suffix}";
                    CodeBuilder code = new CodeBuilder();

                    // Imports
                    code.AddLine($"using {AppConfig.Root.ApplicationName}.Core.Application.DataManagers;");
                    code.AddLine("using UIX.ViewController.Engine.DataBaseObjects;");
                    code.AddLine();

                    // Namespace & Class
                    code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.TableObjects");
                    code.BeginBlock($"public class {className} : UIXTableObjectBase");

                    // Properties
                    code.AddLine($"public override bool IsDevSynced => {(devSync ? "true" : "false")};");
                    code.AddLine();

                    foreach ((int i, KeyValuePair<T1METAP, SqliteDataType> field) in fields.Index())
                    {
                        ColumnSchema cSchema = tSchema.Columns.Where(c => c.PONAM.Equals(field.Key.PONAM)).Single();

                        code.AddLine($"[UIXSignatureField({i})]");
                        code.AddLine($"public {field.Value.CsType} {field.Key.PONAM} {{ get; set; }} = {cSchema.GetDefaultValue(forCsharp: true)};");
                        code.AddLine();
                    }

                    // Methods
                    code.BeginBlock("public override void Save()");
                    code.AddLine($"new TX{suffix}().Save(this);");
                    code.EndBlock();

                    // Namespace & Class
                    code.EndBlock();
                    code.EndBlock();

                    string targetFilePath = Path.Combine(AppConfig.Dev.GenClassTableObjectsDirectoryPath, $"{className}.cs");
                    code.SaveToFile(targetFilePath);
                }

                private static void GenerateK1Class(string suffix, Dictionary<T1METAP, SqliteDataType> fields)
                {
                    string className = $"K1{suffix}";
                    string classNameT1 = $"T1{suffix}";
                    CodeBuilder code = new CodeBuilder();

                    // Namespace & Class
                    code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.TableObjects");
                    code.BeginBlock($"public static class {className}");

                    // Properties
                    code.AddLine($"public const string Name = \"{classNameT1}\";");
                    code.AddLine();

                    // Sub-Classes
                    code.BeginBlock("public static class Fields");

                    foreach (KeyValuePair<T1METAP, SqliteDataType> field in fields)
                    {
                        code.AddLine($"public const string {field.Key.PONAM} = \"{field.Key.PONAM}\";");
                    }

                    code.EndBlock();

                    // Namespace & Class
                    code.EndBlock();
                    code.EndBlock();


                    string targetFilePath = Path.Combine(AppConfig.Dev.GenClassTableObjectsDirectoryPath, $"{className}.cs");
                    code.SaveToFile(targetFilePath);
                }

                private static void GenerateTXBasicClass(string suffix, TableSchema tSchema, Dictionary<T1METAP, SqliteDataType> fields)
                {
                    string className = $"TX{suffix}Basic";
                    string classNameT1 = $"T1{suffix}";
                    string classNameK1 = $"K1{suffix}";
                    CodeBuilder code = new CodeBuilder();

                    Dictionary<T1METAP, SqliteDataType> primkFields = fields.Where(p => p.Key.PRIMK).ToDictionary();

                    // Imports
                    code.AddLine($"using {AppConfig.Root.ApplicationName}.Core.Application.TableObjects;");
                    code.AddLine($"using {AppConfig.Root.ApplicationName}.Core.Framework;");
                    code.AddLine($"using {AppConfig.Root.ApplicationName}.Core.Framework.DataBase.Migration;");
                    code.AddLine($"using System.Data.SQLite;");
                    code.AddLine($"using UIX.ViewController.Engine.DataBaseObjects;");
                    code.AddLine($"using UIX.ViewController.Engine.Querying;");
                    code.AddLine();

                    // Namespace & Class
                    code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.DataManagers");
                    code.BeginBlock($"public class {className}");

                    // Properties
                    code.BeginBlock($"protected SQLiteConnection _connection");
                    code.AddLine($"get => AppEnvironment.GetDataBaseManager().GetConnection();");
                    code.EndBlock();
                    code.AddLine();

                    // Methods PUBLIC
                    {
                        code.AddLine("#region Methods PUBLIC");

                        // CreateNew
                        code.BeginBlock($"public virtual {classNameT1} CreateNew()");
                        code.AddLine($"{classNameT1} obj = new {classNameT1}();");
                        code.AddLine("obj.State = UIXTableObjectState.New;");
                        code.AddLine("return obj;");
                        code.EndBlock();

                        // Save
                        code.AddLine();
                        code.BeginBlock($"public virtual void Save({classNameT1} obj)");
                        code.BeginBlock("if (obj is null)");
                        code.AddLine("throw new ArgumentNullException(nameof(obj));");
                        code.EndBlock();
                        code.AddLine();
                        code.BeginBlock("if (Exists(obj))");
                        code.AddLine("Update(obj);");
                        code.EndBlock();
                        code.BeginBlock("else");
                        code.AddLine("Insert(obj);");
                        code.EndBlock();
                        code.AddLine("obj.State = UIXTableObjectState.Available;");
                        code.AddLine("obj.AcceptChanges();");
                        code.AddLine("MigrationFactory.ToCsv.ExportCsvFileFor(_connection, obj, MigrationFactory.ImportType.DevSync);");
                        code.EndBlock();

                        // Delete
                        code.AddLine();
                        code.BeginBlock($"public virtual void Delete({Helpers.GetMethodParams(primkFields)})");
                        code.AddLine($"string sql = $\"DELETE FROM {classNameT1} WHERE {Helpers.GetSqlWheres(primkFields)};\";");
                        code.AddLine("UIXQuery.ExecuteCustom(sql, _connection);");
                        code.AddLine($"MigrationFactory.ToCsv.ExportCsvFileFor(_connection, \"{classNameT1}\", MigrationFactory.ImportType.DevSync);");
                        code.EndBlock();

                        // Read
                        code.AddLine();
                        code.BeginBlock($"public virtual {classNameT1}? Read({Helpers.GetMethodParams(primkFields)})");
                        code.AddLine($"UIXQuery query = new UIXQuery({classNameK1}.Name, _connection);");
                        foreach (KeyValuePair<T1METAP, SqliteDataType> field in fields)
                        {
                            code.AddLine($"query.AddField({classNameK1}.Name, {classNameK1}.Fields.{field.Key.PONAM});");
                        }
                        foreach (KeyValuePair<T1METAP, SqliteDataType> field in primkFields)
                        {
                            code.AddLine($"query.AddWhere({classNameK1}.Name, {classNameK1}.Fields.{field.Key.PONAM}, QueryCompareType.EQUALS, {field.Key.PONAM.ToLowerInvariant()});");
                        }
                        code.AddLine();
                        code.AddLine("using var reader = query.Execute();");
                        code.BeginBlock("if (!reader.Read())");
                        code.AddLine("return null;");
                        code.EndBlock();
                        code.AddLine();
                        code.AddLine($"{classNameT1} obj = Map(reader);");
                        code.AddLine("obj.AcceptChanges();");
                        code.AddLine("return obj;");
                        code.EndBlock();

                        // ReadAll
                        code.AddLine();
                        code.BeginBlock($"public virtual List<{classNameT1}> ReadAll()");
                        code.AddLine($"UIXQuery query = new UIXQuery({classNameK1}.Name, _connection);");
                        foreach (KeyValuePair<T1METAP, SqliteDataType> field in fields)
                        {
                            code.AddLine($"query.AddField({classNameK1}.Name, {classNameK1}.Fields.{field.Key.PONAM});");
                        }
                        foreach (KeyValuePair<T1METAP, SqliteDataType> field in primkFields)
                        {
                            code.AddLine($"query.AddOrderBy({classNameK1}.Name, {classNameK1}.Fields.{field.Key.PONAM}, OrderDirection.ASC);");
                        }
                        code.AddLine();
                        code.AddLine($"List<{classNameT1}> list = new List<{classNameT1}>();");
                        code.AddLine("using var reader = query.Execute();");
                        code.BeginBlock("while (reader.Read())");
                        code.AddLine($"{classNameT1} obj = Map(reader);");
                        code.AddLine("obj.AcceptChanges();");
                        code.AddLine("list.Add(obj);");
                        code.EndBlock();
                        code.AddLine("return list;");
                        code.EndBlock();

                        code.AddLine("#endregion");
                    }

                    // Methods PROTECTED
                    {
                        code.AddLine();
                        code.AddLine("#region Methods PRIVATE");

                        // Map
                        code.AddLine();
                        code.BeginBlock($"protected static {classNameT1} Map(SQLiteDataReader reader)");
                        code.AddLine($"{classNameT1} obj = new {classNameT1}();");
                        foreach (KeyValuePair<T1METAP, SqliteDataType> field in fields)
                        {
                            code.AddLine($"obj.{field.Key.PONAM} = {Helpers.GetUixQueryMethodCall(classNameK1, field)};");
                        }
                        code.AddLine("obj.State = UIXTableObjectState.Available;");
                        code.AddLine("return obj;");
                        code.EndBlock();

                        code.AddLine("#endregion");
                    }

                    // Methods PRIVATE
                    {
                        code.AddLine();
                        code.AddLine("#region Methods PRIVATE");

                        // Insert
                        code.BeginBlock($"private void Insert({classNameT1} obj)");                        
                        code.AddLine($"string sql = $\"INSERT INTO {classNameT1} ({Helpers.GetSqlInsertFields(fields)}) VALUES ({Helpers.GetSqlInsertValues(fields)});\";");
                        code.AddLine("UIXQuery.ExecuteCustom(sql, _connection);");
                        KeyValuePair<T1METAP, SqliteDataType> autoiField = fields.Where(f => f.Key.AUTOI).SingleOrDefault();
                        if (autoiField.Key is not null)
                        {
                            code.AddLine();
                            code.AddLine("using SQLiteCommand idCmd = _connection.CreateCommand();");
                            code.AddLine("idCmd.CommandText = \"SELECT last_insert_rowid();\";");
                            code.AddLine($"obj.{autoiField.Key.PONAM} = Convert.ToInt64(idCmd.ExecuteScalar());");
                        }
                        code.EndBlock();

                        // Update
                        code.AddLine();
                        code.BeginBlock($"private void Update({classNameT1} obj)");
                        code.AddLine($"string sql = $\"UPDATE {classNameT1} SET {Helpers.GetSqlUpdateFields(fields)} WHERE {Helpers.GetSqlWheres(primkFields, withObjPrefix: true)};\";");
                        code.AddLine("UIXQuery.ExecuteCustom(sql, _connection);");
                        code.EndBlock();

                        // Exists
                        code.AddLine();
                        code.BeginBlock($"private bool Exists({classNameT1} obj)");
                        // OFDO: UIXQuery?
                        code.AddLine("using SQLiteCommand cmd = _connection.CreateCommand();");
                        code.AddLine($"cmd.CommandText = $\"SELECT COUNT(*) FROM {classNameT1} WHERE {Helpers.GetSqlWheres(primkFields, withObjPrefix: true)};\";");
                        code.AddLine("return Convert.ToInt64(cmd.ExecuteScalar()) > 0;");
                        code.EndBlock();

                        code.AddLine("#endregion");
                    }

                    // Namespace & Class
                    code.EndBlock();
                    code.EndBlock();


                    string targetFilePath = Path.Combine(AppConfig.Dev.GenClassDataManagersDirectoryPath, $"{className}.cs");
                    code.SaveToFile(targetFilePath);
                }

                private static void GenerateTXClass(string suffix)
                {
                    string className = $"TX{suffix}";
                    string classNameBasic = $"{className}Basic";
                    CodeBuilder code = new CodeBuilder();

                    // Namespace & Class
                    code.BeginBlock($"namespace {AppConfig.Root.ApplicationName}.Core.Application.DataManagers");
                    code.BeginBlock($"public class {className} : {classNameBasic}");
                    code.EndBlock();
                    code.EndBlock();


                    string targetFilePath = Path.Combine(AppConfig.Dev.GenClassDataManagersDirectoryPath, $"{className}.cs");
                    code.SaveToFile(targetFilePath);
                }

                private static class Helpers
                {
                    public static string GetMethodParams(Dictionary<T1METAP, SqliteDataType> parameters)
                    {
                        List<string> paramStrings = new List<string>();

                        foreach (KeyValuePair<T1METAP, SqliteDataType> parameter in parameters)
                        {
                            paramStrings.Add($"{parameter.Value.CsType} {parameter.Key.PONAM.ToLowerInvariant()}");
                        }

                        return String.Join(", ", paramStrings);
                    }

                    public static string GetSqlWheres(Dictionary<T1METAP, SqliteDataType> fields, bool withObjPrefix = false)
                    {
                        string objPrefix = withObjPrefix ? "obj." : string.Empty;
                        List<string> fieldStrings = new List<string>();

                        foreach (KeyValuePair<T1METAP, SqliteDataType> field in fields)
                        {
                            string valPonam = withObjPrefix ? field.Key.PONAM : field.Key.PONAM.ToLowerInvariant();

                            fieldStrings.Add($"{field.Key.PONAM} = '{{{objPrefix}{valPonam}}}'");
                        }

                        return String.Join(" AND ", fieldStrings);
                    }
                    
                    public static string GetSqlInsertFields(Dictionary<T1METAP, SqliteDataType> fields)
                    {
                        Dictionary<T1METAP, SqliteDataType> filteredFields = fields.Where(f => !f.Key.AUTOI).ToDictionary();

                        return String.Join(", ", filteredFields.Select(f => f.Key.PONAM));
                    }

                    public static string GetSqlInsertValues(Dictionary<T1METAP, SqliteDataType> fields)
                    {
                        Dictionary<T1METAP, SqliteDataType> filteredFields = fields.Where(f => !f.Key.AUTOI).ToDictionary();

                        // OFDO: wird ToDbValue benötigt? return String.Join(", ", filteredFields.Select(f => $"{{ToDbValue(obj.{f.Key.PONAM})}}"));
                        return String.Join(", ", filteredFields.Select(f => $"{{obj.{f.Key.PONAM}}}"));
                    }

                    public static string GetSqlUpdateFields(Dictionary<T1METAP, SqliteDataType> fields)
                    {
                        Dictionary<T1METAP, SqliteDataType> filteredFields = fields.Where(f => !f.Key.AUTOI).ToDictionary();
                        List<string> fieldStrings = new List<string>();

                        foreach (KeyValuePair<T1METAP, SqliteDataType> field in filteredFields)
                        {
                            // fieldStrings.Add($"{field.Key.PONAM} = '{{ToDbValue(obj.{field.Key.PONAM})}}'");
                            fieldStrings.Add($"{field.Key.PONAM} = '{{obj.{field.Key.PONAM}}}'");
                        }

                        return String.Join(", ", fieldStrings);
                    }

                    public static string GetUixQueryMethodCall(string classNameK1, KeyValuePair<T1METAP, SqliteDataType> field)
                    {
                        string methodName = field.Value.GetUixQueryMethod();
                        string ponam = field.Key.PONAM;

                        if (methodName.Equals("GetString"))
                            return $"UIXQuery.{methodName}(reader, {classNameK1}.Name, {classNameK1}.Fields.{ponam}, def: \"\")";

                        return $"UIXQuery.{methodName}(reader, {classNameK1}.Name, {classNameK1}.Fields.{ponam})";
                    }
                }

                private sealed class CodeBuilder
                {
                    private readonly StringBuilder _sb = new StringBuilder();
                    private int _indentLevel = 0;

                    public CodeBuilder AddLine(string line = "")
                    {
                        if (line is null) line = string.Empty;

                        if (line.Length.Equals(0))
                        {
                            _sb.AppendLine();
                            return this;
                        }

                        _sb.Append(new string(' ', _indentLevel * 4));
                        _sb.AppendLine(line);

                        return this;
                    }

                    public CodeBuilder BeginBlock(string header)
                    {
                        AddLine(header);
                        AddLine("{");

                        _indentLevel++;

                        return this;
                    }

                    public CodeBuilder EndBlock(string suffix = "")
                    {
                        if (_indentLevel > 0) _indentLevel--;

                        AddLine($"}}{suffix}");

                        return this;
                    }

                    public void SaveToFile(string targetFilePath)
                    {
                        File.WriteAllText(targetFilePath, ToString(), Encoding.UTF8);

                        // OFDOI: Try to export to solution
                    }

                    public override string ToString()
                    {
                        return _sb.ToString();
                    }
                }
            }
        }
    }
}
