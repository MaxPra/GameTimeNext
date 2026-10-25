using System.Globalization;
using GameTimeNext.Core.Application.Metadata.Data;
using GameTimeNext.Core.Framework.Config;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.DataBase.Migration
{
    internal static partial class MigrationFactory
    {
        public static partial class Metadata
        {
            public static class CodeGenerator
            {
                // OFDO (#OF001): Make a base class for TXBasic with generic methods

                public static void GenerateFor(T1METAH t1metah)
                {
                    if (t1metah is null) throw new ArgumentNullException(nameof(t1metah));

                    string menam = t1metah.MENAM;
                    if (FnString.IsNullEmptyOrWhitespace(menam)) throw new ArgumentNullException($"{nameof(t1metah)}.{nameof(t1metah.MENAM)}");

                    string suffix = menam.Substring(2);
                    List<T1METAP> t1metaps = new TXMETAP().ReadAll(menam).OrderBy(p => p.PORDE).ThenBy(p => p.PONAM).ToList();

                    if (t1metaps.Count == 0) throw new InvalidOperationException($"No metadata positions found for '{menam}'.");

                    Dictionary<T1METAP, UIXSqliteDataType> fields = GenerateFields(t1metaps);
                    TableSchema tSchema = SchemaGenerator.GenerateSingleFromMetadata(t1metah.MENAM);
                    string nsTableObjects = $"{AppConfig.Root.ApplicationName}.Core.Application.TableObjects";
                    string nsDataManagers = $"{AppConfig.Root.ApplicationName}.Core.Application.DataManagers";
                    Generate(suffix, tSchema, t1metah.DSYNC, fields, nsTableObjects, nsDataManagers);
                }

                /// <summary>
                /// Generates the classes of the metadata tables (T1METAH, T1METAP) from the hard-coded METADATA definition.
                /// </summary>
                public static void GenerateForMetadata()
                {
                    string nsMetadata = $"{AppConfig.Root.ApplicationName}.Core.Application.Metadata.Data";

                    foreach (TableSchema tSchema in METADATA)
                    {
                        List<T1METAP> t1metaps = tSchema.Columns.Select(c => new T1METAP()
                        {
                            MENAM = tSchema.MENAM,
                            PONAM = c.PONAM,
                            DATYP = c.DATYP.Key,
                            DALEN = c.DALEN,
                            PORDE = c.PORDE,
                            PRIMK = c.PRIMK,
                            AUTOI = c.AUTOI,
                            DEFAK = c.DEFAK,
                            DEFVL = c.DEFVL,
                        }).OrderBy(p => p.PORDE).ThenBy(p => p.PONAM).ToList();

                        Generate(tSchema.MENAM.Substring(2), tSchema, true, GenerateFields(t1metaps), nsMetadata, nsMetadata);
                    }
                }

                private static void Generate(string suffix, TableSchema tSchema, bool devSync, Dictionary<T1METAP, UIXSqliteDataType> fields, string nsTableObjects, string nsDataManagers)
                {
                    GenerateT1Class(suffix, tSchema, devSync, fields, nsTableObjects, nsDataManagers);
                    GenerateK1Class(suffix, fields, nsTableObjects);
                    GenerateTXBasicClass(suffix, tSchema, fields, nsTableObjects, nsDataManagers);
                    GenerateTXClass(suffix, nsDataManagers);
                }

                private static Dictionary<T1METAP, UIXSqliteDataType> GenerateFields(List<T1METAP> t1metaps)
                {
                    Dictionary<T1METAP, UIXSqliteDataType> temp = new Dictionary<T1METAP, UIXSqliteDataType>();

                    t1metaps.ForEach(t1metap =>
                    {
                        UIXSqliteDataType dataType = UIXSqliteDataType.GetByKey(t1metap.DATYP);
                        temp.Add(t1metap, dataType);
                    });

                    return temp;
                }

                private static void GenerateT1Class(string suffix, TableSchema tSchema, bool devSync, Dictionary<T1METAP, UIXSqliteDataType> fields, string nsTableObjects, string nsDataManagers)
                {
                    string className = $"T1{suffix}";
                    CodeBuilder code = new CodeBuilder();

                    // Imports
                    if (!nsDataManagers.Equals(nsTableObjects))
                        code.AddLine($"using {nsDataManagers};");
                    code.AddLine("using UIX.ViewController.Engine.DataBaseObjects;");
                    code.AddLine();

                    // Namespace & Class
                    code.BeginBlock($"namespace {nsTableObjects}");
                    code.BeginBlock($"public class {className} : UIXTableObjectBase");

                    // Properties
                    code.AddLine($"public override bool IsDevSynced => {(devSync ? "true" : "false")};");
                    code.AddLine();

                    foreach ((int i, KeyValuePair<T1METAP, UIXSqliteDataType> field) in fields.Index())
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


                    code.SaveToFile(AppConfig.Dev.GenClassTableObjectsDirectoryPath, $"{className}.cs");
                }

                private static void GenerateK1Class(string suffix, Dictionary<T1METAP, UIXSqliteDataType> fields, string nsTableObjects)
                {
                    string className = $"K1{suffix}";
                    string classNameT1 = $"T1{suffix}";
                    CodeBuilder code = new CodeBuilder();

                    // Namespace & Class
                    code.BeginBlock($"namespace {nsTableObjects}");
                    code.BeginBlock($"public static class {className}");

                    // Properties
                    code.AddLine($"public const string Name = \"{classNameT1}\";");
                    code.AddLine();

                    // Sub-Classes
                    code.BeginBlock("public static class Fields");

                    foreach (KeyValuePair<T1METAP, UIXSqliteDataType> field in fields)
                    {
                        code.AddLine($"public const string {field.Key.PONAM} = \"{field.Key.PONAM}\";");
                    }

                    code.EndBlock();

                    // Namespace & Class
                    code.EndBlock();
                    code.EndBlock();


                    code.SaveToFile(AppConfig.Dev.GenClassTableObjectsDirectoryPath, $"{className}.cs");
                }

                private static void GenerateTXBasicClass(string suffix, TableSchema tSchema, Dictionary<T1METAP, UIXSqliteDataType> fields, string nsTableObjects, string nsDataManagers)
                {
                    string className = $"TX{suffix}Basic";
                    string classNameT1 = $"T1{suffix}";
                    string classNameK1 = $"K1{suffix}";
                    CodeBuilder code = new CodeBuilder();

                    Dictionary<T1METAP, UIXSqliteDataType> primkFields = fields.Where(p => p.Key.PRIMK).ToDictionary();
                    bool hasCrat = Helpers.HasField(fields, "CRAT");
                    bool hasChat = Helpers.HasField(fields, "CHAT");
                    bool hasCrus = Helpers.HasField(fields, "CRUS");
                    bool hasChus = Helpers.HasField(fields, "CHUS");

                    // Imports
                    if (!nsTableObjects.Equals(nsDataManagers))
                        code.AddLine($"using {nsTableObjects};");
                    code.AddLine($"using {AppConfig.Root.ApplicationName}.Core.Framework;");
                    code.AddLine($"using {AppConfig.Root.ApplicationName}.Core.Framework.DataBase.Migration;");
                    code.AddLine($"using System.Data.SQLite;");
                    code.AddLine($"using System.Globalization;");
                    code.AddLine($"using UIX.ViewController.Engine.DataBaseObjects;");
                    code.AddLine($"using UIX.ViewController.Engine.Migration.Types;");
                    code.AddLine($"using UIX.ViewController.Engine.Querying;");
                    if (hasCrus || hasChus)
                        code.AddLine($"using UIX.ViewController.Engine.Utils;");
                    code.AddLine();

                    // Namespace & Class
                    code.BeginBlock($"namespace {nsDataManagers}");
                    code.BeginBlock($"public class {className}");

                    // Properties
                    code.AddLine("private readonly SQLiteConnection? _connectionOverride;");
                    code.AddLine();
                    code.BeginBlock($"protected SQLiteConnection _connection");
                    code.AddLine($"get => _connectionOverride ?? AppEnvironment.GetDataBaseManager().GetConnection();");
                    code.EndBlock();
                    code.AddLine();

                    // Constructors
                    code.BeginBlock($"public {className}()");
                    code.EndBlock();
                    code.AddLine();
                    code.BeginBlock($"public {className}(SQLiteConnection connection)");
                    code.AddLine("_connectionOverride = connection;");
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
                        code.AddLine("using SQLiteCommand cmd = _connection.CreateCommand();");
                        code.AddLine($"cmd.CommandText = \"DELETE FROM {classNameT1} WHERE {Helpers.GetSqlWheres(primkFields)};\";");
                        Helpers.AddSqlParameters(code, primkFields);
                        code.AddLine("cmd.ExecuteNonQuery();");
                        code.AddLine($"MigrationFactory.ToCsv.ExportCsvFileFor(_connection, \"{classNameT1}\", MigrationFactory.ImportType.DevSync);");
                        code.EndBlock();

                        // Read
                        code.AddLine();
                        code.BeginBlock($"public virtual {classNameT1}? Read({Helpers.GetMethodParams(primkFields)})");
                        code.AddLine($"UIXQuery query = new UIXQuery({classNameK1}.Name, _connection);");
                        foreach (KeyValuePair<T1METAP, UIXSqliteDataType> field in fields)
                        {
                            code.AddLine($"query.AddField({classNameK1}.Name, {classNameK1}.Fields.{field.Key.PONAM});");
                        }
                        foreach (KeyValuePair<T1METAP, UIXSqliteDataType> field in primkFields)
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
                        foreach (KeyValuePair<T1METAP, UIXSqliteDataType> field in fields)
                        {
                            code.AddLine($"query.AddField({classNameK1}.Name, {classNameK1}.Fields.{field.Key.PONAM});");
                        }
                        foreach (KeyValuePair<T1METAP, UIXSqliteDataType> field in primkFields)
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
                        foreach (KeyValuePair<T1METAP, UIXSqliteDataType> field in fields)
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
                        if (hasCrat)
                            code.AddLine("obj.CRAT = DateTime.Now;");
                        if (hasChat)
                            code.AddLine("obj.CHAT = DateTime.Now;");
                        if (hasCrus)
                            code.AddLine("obj.CRUS = FnOpSys.GetCurrentUserName();");
                        if (hasChus)
                            code.AddLine("obj.CHUS = FnOpSys.GetCurrentUserName();");
                        code.AddLine("using SQLiteCommand cmd = _connection.CreateCommand();");
                        code.AddLine($"cmd.CommandText = \"INSERT INTO {classNameT1} ({Helpers.GetSqlInsertFields(fields)}) VALUES ({Helpers.GetSqlInsertValues(fields)});\";");
                        Helpers.AddSqlParameters(code, fields.Where(f => !f.Key.AUTOI), withObjPrefix: true);
                        code.AddLine("cmd.ExecuteNonQuery();");
                        KeyValuePair<T1METAP, UIXSqliteDataType> autoiField = fields.Where(f => f.Key.AUTOI).SingleOrDefault();
                        if (autoiField.Key is not null)
                        {
                            code.AddLine();
                            code.AddLine("using SQLiteCommand idCmd = _connection.CreateCommand();");
                            code.AddLine("idCmd.CommandText = \"SELECT last_insert_rowid();\";");
                            code.AddLine($"obj.{autoiField.Key.PONAM} = Convert.ToInt64(idCmd.ExecuteScalar(), CultureInfo.InvariantCulture);");
                        }
                        code.EndBlock();

                        // Update
                        code.AddLine();
                        code.BeginBlock($"private void Update({classNameT1} obj)");
                        if (hasChat)
                            code.AddLine("obj.CHAT = DateTime.Now;");
                        if (hasChus)
                            code.AddLine("obj.CHUS = FnOpSys.GetCurrentUserName();");
                        code.AddLine("using SQLiteCommand cmd = _connection.CreateCommand();");
                        code.AddLine($"cmd.CommandText = \"UPDATE {classNameT1} SET {Helpers.GetSqlUpdateFields(fields)} WHERE {Helpers.GetSqlWheres(primkFields)};\";");
                        Helpers.AddSqlParameters(code, fields.Where(f => !f.Key.AUTOI || f.Key.PRIMK), withObjPrefix: true);
                        code.AddLine("cmd.ExecuteNonQuery();");
                        code.EndBlock();

                        // Exists
                        code.AddLine();
                        code.BeginBlock($"private bool Exists({classNameT1} obj)");
                        // OFDO (#OF001): UIXQuery?
                        code.AddLine("using SQLiteCommand cmd = _connection.CreateCommand();");
                        code.AddLine($"cmd.CommandText = \"SELECT COUNT(*) FROM {classNameT1} WHERE {Helpers.GetSqlWheres(primkFields)};\";");
                        Helpers.AddSqlParameters(code, primkFields, withObjPrefix: true);
                        code.AddLine("return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;");
                        code.EndBlock();

                        // ToDbValue
                        code.AddLine();
                        code.BeginBlock("private static object? ToDbValue(object? valFrom)");
                        code.AddLine("return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);");
                        code.EndBlock();

                        code.AddLine("#endregion");
                    }

                    // Namespace & Class
                    code.EndBlock();
                    code.EndBlock();


                    code.SaveToFile(AppConfig.Dev.GenClassDataManagersDirectoryPath, $"{className}.cs");
                }

                private static void GenerateTXClass(string suffix, string nsDataManagers)
                {
                    string className = $"TX{suffix}";
                    string classNameBasic = $"{className}Basic";
                    CodeBuilder code = new CodeBuilder();

                    // Imports
                    code.AddLine("using System.Data.SQLite;");
                    code.AddLine();

                    // Namespace & Class
                    code.BeginBlock($"namespace {nsDataManagers}");
                    code.BeginBlock($"public class {className} : {classNameBasic}");
                    code.BeginBlock($"public {className}()");
                    code.EndBlock();
                    code.AddLine();
                    code.BeginBlock($"public {className}(SQLiteConnection connection) : base(connection)");
                    code.EndBlock();
                    code.EndBlock();
                    code.EndBlock();


                    code.SaveToFile(AppConfig.Dev.GenClassDataManagersDirectoryPath, $"{className}.cs", preventSolutionOverride: true);
                }

                private static class Helpers
                {
                    public static bool HasField(Dictionary<T1METAP, UIXSqliteDataType> fields, string ponam)
                    {
                        return fields.Keys.Any(f => f.PONAM.Equals(ponam));
                    }

                    public static string GetMethodParams(Dictionary<T1METAP, UIXSqliteDataType> parameters)
                    {
                        List<string> paramStrings = new List<string>();

                        foreach (KeyValuePair<T1METAP, UIXSqliteDataType> parameter in parameters)
                        {
                            paramStrings.Add($"{parameter.Value.CsType} {parameter.Key.PONAM.ToLowerInvariant()}");
                        }

                        return String.Join(", ", paramStrings);
                    }

                    public static string GetSqlWheres(Dictionary<T1METAP, UIXSqliteDataType> fields)
                    {
                        return String.Join(" AND ", fields.Select(f => $"{f.Key.PONAM} = @{f.Key.PONAM}"));
                    }
                    
                    public static string GetSqlInsertFields(Dictionary<T1METAP, UIXSqliteDataType> fields)
                    {
                        Dictionary<T1METAP, UIXSqliteDataType> filteredFields = fields.Where(f => !f.Key.AUTOI).ToDictionary();

                        return String.Join(", ", filteredFields.Select(f => f.Key.PONAM));
                    }

                    public static string GetSqlInsertValues(Dictionary<T1METAP, UIXSqliteDataType> fields)
                    {
                        Dictionary<T1METAP, UIXSqliteDataType> filteredFields = fields.Where(f => !f.Key.AUTOI).ToDictionary();

                        return String.Join(", ", filteredFields.Select(f => $"@{f.Key.PONAM}"));
                    }

                    public static string GetSqlUpdateFields(Dictionary<T1METAP, UIXSqliteDataType> fields)
                    {
                        Dictionary<T1METAP, UIXSqliteDataType> filteredFields = fields.Where(f => !f.Key.AUTOI).ToDictionary();

                        return String.Join(", ", filteredFields.Select(f => $"{f.Key.PONAM} = @{f.Key.PONAM}"));
                    }

                    /// <summary>
                    /// Adds a "cmd.Parameters.AddWithValue" line per field, so values are passed typed instead of being formatted into the SQL string.
                    /// </summary>
                    public static void AddSqlParameters(CodeBuilder code, IEnumerable<KeyValuePair<T1METAP, UIXSqliteDataType>> fields, bool withObjPrefix = false)
                    {
                        foreach (KeyValuePair<T1METAP, UIXSqliteDataType> field in fields)
                        {
                            string valName = withObjPrefix ? $"obj.{field.Key.PONAM}" : field.Key.PONAM.ToLowerInvariant();

                            code.AddLine($"cmd.Parameters.AddWithValue(\"@{field.Key.PONAM}\", ToDbValue({valName}));");
                        }
                    }

                    public static string GetUixQueryMethodCall(string classNameK1, KeyValuePair<T1METAP, UIXSqliteDataType> field)
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
                    #region Properties
                    private readonly StringBuilder _sb = new StringBuilder();
                    private int _indentLevel = 0;
                    #endregion

                    #region Methods PUBLIC
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

                    public void SaveToFile(string targetDirectoryPath, string fileName, bool preventSolutionOverride = false)
                    {
                        string? solutionFilePath = GetSolutionFilePath(fileName);

                        if (solutionFilePath is not null && !preventSolutionOverride)
                        {
                            File.WriteAllText(solutionFilePath, ToString(), Encoding.UTF8);
                            FnLog.AddInfo(this, $"{fileName} generated to solution.");
                        }
                        else
                        {
                            File.WriteAllText(Path.Combine(targetDirectoryPath, fileName), ToString(), Encoding.UTF8);
                            FnLog.AddInfo(this, $"{fileName} generated to genClass.");
                        }
                    }

                    public override string ToString()
                    {
                        return _sb.ToString();
                    }
                    #endregion

                    #region Methods PRIVATE
                    private string? GetSolutionFilePath(string fileName)
                    {
                        try
                        {
                            DirectoryInfo slnDirInfo = new DirectoryInfo(AppConfig.Dev.SolutionDirectoryPath);
                            return slnDirInfo.GetFiles(fileName, new EnumerationOptions()
                            {
                                MatchCasing = MatchCasing.CaseInsensitive,
                                RecurseSubdirectories = true
                            }).Single().FullName;
                        }
                        catch{
                            return null;
                        }
                    }
                    #endregion
                }
            }
        }
    }
}
