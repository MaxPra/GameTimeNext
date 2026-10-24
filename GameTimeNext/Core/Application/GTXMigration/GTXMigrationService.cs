using System.Globalization;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.MigrationTasks;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.Utils;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.FrameworkElements.Loader;
using UIX.ViewController.Engine.Migration.Types;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.GTXMigration
{
    /// <summary>
    /// Responsible for migrating the old GameTimeX data to the new GameTimeNXT data
    /// </summary>
    public class GTXMigrationService
    {
        private string _rootFolder = string.Empty;
        private string _databaseFilePath = string.Empty;
        private string _startUpParmsFilePath = string.Empty;
        private string _imagesFolderPath = string.Empty;
        private UIXLoader? _loader = null;
        private string _loaderStartText = string.Empty;

        private SQLiteConnection _connectionOld = new SQLiteConnection();

        public GTXMigrationService(string rootFolder, UIXLoader loader)
        {
            _rootFolder = rootFolder;

            _databaseFilePath = System.IO.Path.Join(rootFolder, "GameTimeXDB.db");
            _startUpParmsFilePath = System.IO.Path.Join(rootFolder, "startUpParms.json");
            _imagesFolderPath = System.IO.Path.Join(rootFolder, "images");

            _loader = loader;
        }

        public void MigrateToGTNXT()
        {
            _loader!.SetTextStep("connecting to old database");

            // Connect to the old database
            bool connectSuccess = ConnectToSQLite();

            if (!connectSuccess)
                return;

            _loader.SetTextStep("migrating profile data");
            // Migrate profiles
            MigrateProfiles();

            _loader.SetTextStep("migrating session data");
            // Sessions
            MigrateSessions();

            // Playthroughs
            _loader.SetTextStep("migrating playthroughs [NEW]");
            MigratePlaythroughs();

            _loader.SetTextStep("migrating profile covers");
            // Profile covers
            MigrateProfileImagesToCovers();

            // Close old connection
            _connectionOld.Close();

            new MigTask_026b_005().Execute();
        }

        private bool ConnectToSQLite()
        {
            string connectionString;

            if (!File.Exists(_databaseFilePath))
            {
                return false;
            }
            else
            {
                connectionString = $"Data Source={_databaseFilePath};Version=3;Compress=True;BusyTimeout=15000;Pooling=False;";
            }

            _connectionOld = new SQLiteConnection(connectionString);
            try
            {
                _connectionOld.Open();
            }
            catch
            {
            }

            try
            {
                using var fkOn = new SQLiteCommand("PRAGMA foreign_keys = ON;", _connectionOld);
                fkOn.ExecuteNonQuery();
            }
            catch
            {
            }

            return true;
        }

        private bool MigrateProfiles()
        {
            bool success = true;

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ProfileID, GameName, FirstPlay, LastPlay, ProfilePicFileName, ExtGameFolder, CreatedAt, ChangedAt, SteamAppID, ProfileSettings, Executables ");
            sb.Append("from tblGameProfiles");

            try
            {
                using (var cmd = _connectionOld.CreateCommand())
                {
                    cmd.CommandText = sb.ToString();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int pfid = reader.GetInt32(0);
                            string gana = reader.GetString(1);

                            _loader.SetTextStep($"migrating profile data [ {gana} ]");

                            DateTime fipl = ParseOldDateTime(reader.GetValue(2));
                            DateTime lapl = ParseOldDateTime(reader.GetValue(3));

                            string ppfn = System.IO.Path.Join(AppConfig.Storage.ProfileCoversDirectoryPath, reader.GetString(4));
                            string exgf = reader.GetString(5);

                            DateTime crat = ParseOldDateTime(reader.GetString(6));
                            DateTime chat = ParseOldDateTime(reader.GetString(7));

                            int said = reader.GetInt32(8);
                            string prse = reader.GetString(9);
                            string exec = reader.GetString(10);

                            long cupt = 0;

                            List<System.Windows.Media.Color> accentColorsCalc = FnImage.GetTopAccentColors(System.IO.Path.Join(_imagesFolderPath, reader.GetString(4)), 3);
                            System.Windows.Media.Color accentColor = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[0].R, accentColorsCalc[0].G, accentColorsCalc[0].B);
                            string[] accentColors = FnTheme.CalculateAccentStateColors(accentColor.ToString(CultureInfo.InvariantCulture));

                            Dictionary<string, string> dicAccentColors = new Dictionary<string, string>();
                            dicAccentColors.Add("accent", accentColors[0]);
                            dicAccentColors.Add("hover", accentColors[1]);
                            dicAccentColors.Add("pressed", accentColors[2]);

                            CAccentColors cAccentColors = new CAccentColors();
                            cAccentColors.AccentColors = dicAccentColors;

                            string acco = cAccentColors.Serialize();

                            string[] accentColorsInitArray = new string[3];
                            accentColorsInitArray[0] = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[0].R, accentColorsCalc[0].G, accentColorsCalc[0].B).ToString(CultureInfo.InvariantCulture);
                            accentColorsInitArray[1] = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[1].R, accentColorsCalc[1].G, accentColorsCalc[1].B).ToString(CultureInfo.InvariantCulture);
                            accentColorsInitArray[2] = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[2].R, accentColorsCalc[2].G, accentColorsCalc[2].B).ToString(CultureInfo.InvariantCulture);

                            Dictionary<string, bool> dicAccentColorsInit = new Dictionary<string, bool>();
                            dicAccentColorsInit.Add(accentColorsInitArray[0], true);
                            dicAccentColorsInit.Add(accentColorsInitArray[1], false);
                            dicAccentColorsInit.Add(accentColorsInitArray[2], false);

                            CAccentColorsInit cAccentColorsInit = new CAccentColorsInit();
                            cAccentColorsInit.AccentColors = dicAccentColorsInit;

                            string acin = cAccentColorsInit.Serialize();

                            bool acac = true;

                            using (SQLiteCommand insertCmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand())
                            {
                                insertCmd.CommandText =
                                    "INSERT INTO T1PROFI " +
                                    "(PFID, GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT, ETMA, ETME, ETCO, ETML, ARCH) " +
                                    "VALUES " +
                                    "(@PFID, @GANA, @FIPL, @LAPL, @PPFN, @EXGF, @SAID, @PRSE, @EXEC, @CRAT, @CHAT, @ACCO, @ACIN, @ACAC, @CUPT, 0, 0, 0, 0, 0);";

                                insertCmd.Parameters.AddWithValue("@PFID", pfid);
                                insertCmd.Parameters.AddWithValue("@GANA", gana);

                                insertCmd.Parameters.AddWithValue("@FIPL", ToDbValue(fipl));
                                insertCmd.Parameters.AddWithValue("@LAPL", ToDbValue(lapl));

                                insertCmd.Parameters.AddWithValue("@PPFN", ppfn);
                                insertCmd.Parameters.AddWithValue("@EXGF", exgf);

                                insertCmd.Parameters.AddWithValue("@SAID", said);
                                insertCmd.Parameters.AddWithValue("@PRSE", prse);
                                insertCmd.Parameters.AddWithValue("@EXEC", exec);

                                insertCmd.Parameters.AddWithValue("@CRAT", ToDbValue(crat));
                                insertCmd.Parameters.AddWithValue("@CHAT", ToDbValue(chat));

                                insertCmd.Parameters.AddWithValue("@ACCO", acco);
                                insertCmd.Parameters.AddWithValue("@ACIN", acin);
                                insertCmd.Parameters.AddWithValue("@ACAC", ToDbValue(acac));

                                insertCmd.Parameters.AddWithValue("@CUPT", cupt);

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }

        private bool MigrateSessions()
        {
            bool success = true;

            int totalCount = 0;
            int current = 0;

            try
            {
                // Determine total count
                using (var countCmd = _connectionOld.CreateCommand())
                {
                    countCmd.CommandText = "SELECT COUNT(*) FROM tblGameSessions";
                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT SID, FK_PID, Played_From, Played_To, Playtime ");
                sb.Append("from tblGameSessions");

                using (var cmd = _connectionOld.CreateCommand())
                {
                    cmd.CommandText = sb.ToString();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            current++;

                            int seid = reader.GetInt32(0);
                            int pfid = reader.GetInt32(1);

                            DateTime plfr = ParseOldDateTime(reader.GetValue(2));
                            DateTime plto = ParseOldDateTime(reader.GetValue(3));

                            _loader!.SetTextStep($"migrating session data [ {current} / {totalCount} ]");

                            double plti = Convert.ToDouble(reader.GetValue(4), CultureInfo.InvariantCulture);

                            using (SQLiteCommand insertCmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand())
                            {
                                insertCmd.CommandText =
                                    "INSERT INTO T1SESSI " +
                                    "(SEID, PFID, PLFR, PLTO, PLTI, CRAT, CHAT, PTID) " +
                                    "VALUES " +
                                    "(@SEID, @PFID, @PLFR, @PLTO, @PLTI, @CRAT, @CHAT, @PTID);";

                                insertCmd.Parameters.AddWithValue("@SEID", seid);
                                insertCmd.Parameters.AddWithValue("@PFID", pfid);

                                insertCmd.Parameters.AddWithValue("@PLFR", ToDbValue(plfr));
                                insertCmd.Parameters.AddWithValue("@PLTO", ToDbValue(plto));

                                insertCmd.Parameters.AddWithValue("@PLTI", plti);

                                DateTime now = DateTime.Now;
                                insertCmd.Parameters.AddWithValue("@CRAT", ToDbValue(now));
                                insertCmd.Parameters.AddWithValue("@CHAT", ToDbValue(now));

                                insertCmd.Parameters.AddWithValue("@PTID", 0);

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }

        private void MigratePlaythroughs()
        {
            // Read all profiles
            List<T1PROFI> t1profis = new TXPROFI().ReadAll();

            foreach (T1PROFI t1profi in t1profis)
            {
                long ptid = TFPLTHR.CreateNewPlaythrough(t1profi.PFID);

                List<T1SESSI> t1sessis = TFPROFI.GetAllSessions(t1profi);

                foreach (T1SESSI t1sessi in t1sessis)
                {
                    t1sessi.PTID = ptid;
                    new TXSESSI().Save(t1sessi);
                }

            }
        }

        private DateTime ParseOldDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return DateTime.MinValue;
            }

            string str = value.ToString();

            if (FnString.IsNullEmptyOrWhitespace(str))
            {
                return DateTime.MinValue;
            }

            str = str.Trim();
            str = str.Replace('\u00A0', ' ');

            DateTime result;

            string[] formats = new string[]
            {
                "dd.MM.yyyy HH:mm:ss",
                "dd.MM.yyyy H:mm:ss",
                "dd.MM.yyyy HH:mm",
                "dd.MM.yyyy H:mm",
                "dd.MM.yyyy",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-dd"
            };

            if (DateTime.TryParseExact(
                    str,
                    formats,
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
                    System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                    out result))
            {
                return result;
            }

            if (DateTime.TryParse(
                    str,
                    System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
                    System.Globalization.DateTimeStyles.AllowWhiteSpaces,
                    out result))
            {
                return result;
            }

            return DateTime.MinValue;
        }

        private static object? ToDbValue(object? valFrom)
        {
            return UIXSqliteDataType.Convert(UIXSqliteDataType.ConversionType.CSharp, UIXSqliteDataType.ConversionType.Sqlite, valFrom);
        }

        private bool MigrateProfileImagesToCovers()
        {
            bool success = true;

            try
            {
                ConvertFolderToNewFormat(_imagesFolderPath, AppConfig.Storage.ProfileCoversDirectoryPath);
            }
            catch
            {
                success = false;
                return success;
            }

            try
            {
            }
            catch
            {
                success = false;
            }

            return success;
        }

        public void ConvertFolderToNewFormat(string sourceFolder, string targetFolder)
        {
            if (!Directory.Exists(sourceFolder))
            {
                throw new DirectoryNotFoundException("Quellordner nicht gefunden: " + sourceFolder);
            }

            FnDirectory.EnsureDirectoryExists(targetFolder);

            string[] files = Directory.GetFiles(sourceFolder);
            int total = files.Length;

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];

                if (!IsImageFile(path))
                {
                    continue;
                }

                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string targetPath = System.IO.Path.Combine(targetFolder, fileName + ".jpg");

                _loader.SetTextStep($"migrating covers [ {i + 1} / {total} ]");

                ConvertSingleImage(path, targetPath);
            }
        }

        private static void ConvertSingleImage(string inputPath, string outputPath)
        {
            const int targetW = 600;
            const int targetH = 900;

            int maxImageSize = 600;

            int topThicknessLeft = 140;
            int topThicknessRight = 80;

            int bottomThicknessLeft = 80;
            int bottomThicknessRight = 140;

            BitmapSource src = FnImage.Load(inputPath);
            Size imageSize = FnImage.GetFitSize(src, maxImageSize, maxImageSize);

            double x = Math.Floor((targetW - imageSize.Width) / 2);
            double y = Math.Floor((targetH - imageSize.Height) / 2);

            List<Color> particleColors = GetParticleColors(inputPath);
            int seed = GetStableSeed(System.IO.Path.GetFileName(inputPath));

            BitmapSource canvas = FnImage.Render(targetW, targetH, dc =>
            {
                Rect canvasRect = new Rect(0, 0, targetW, targetH);

                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(18, 18, 18)), null, canvasRect);

                // Subtle particles in the empty background around the image
                FnImage.DrawParticles(dc, new RectangleGeometry(canvasRect), particleColors, seed, 120, 10);

                dc.DrawImage(src, new Rect(new Point(x, y), imageSize));

                DrawSlantedOverlays(
                    dc,
                    targetW,
                    targetH,
                    topThicknessLeft,
                    topThicknessRight,
                    bottomThicknessLeft,
                    bottomThicknessRight,
                    particleColors,
                    seed
                );
            });

            CFProfilesEditApp.SaveProfileCover(canvas, outputPath);
        }

        private static void DrawSlantedOverlays(
            DrawingContext dc,
            int targetW,
            int targetH,
            int topThicknessLeft,
            int topThicknessRight,
            int bottomThicknessLeft,
            int bottomThicknessRight,
            IReadOnlyList<Color> particleColors,
            int seed)
        {
            Brush overlayBrush = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0));
            Pen edgePen = new Pen(Brushes.Black, 3);

            Geometry topShape = FnImage.CreatePolygon(
                new Point(0, 0),
                new Point(targetW, 0),
                new Point(targetW, topThicknessRight),
                new Point(0, topThicknessLeft)
            );

            Geometry bottomShape = FnImage.CreatePolygon(
                new Point(0, targetH),
                new Point(targetW, targetH),
                new Point(targetW, targetH - bottomThicknessRight),
                new Point(0, targetH - bottomThicknessLeft)
            );

            dc.DrawGeometry(overlayBrush, null, topShape);
            FnImage.DrawParticles(dc, topShape, particleColors, seed + 1, 140, 16);
            dc.DrawLine(edgePen, new Point(0, topThicknessLeft), new Point(targetW, topThicknessRight));

            dc.DrawGeometry(overlayBrush, null, bottomShape);
            FnImage.DrawParticles(dc, bottomShape, particleColors, seed + 2, 140, 16);
            dc.DrawLine(edgePen, new Point(0, targetH - bottomThicknessLeft), new Point(targetW, targetH - bottomThicknessRight));
        }

        private static List<Color> GetParticleColors(string imagePath)
        {
            try
            {
                return FnImage.GetTopAccentColors(imagePath, 3);
            }
            catch
            {
                return new List<Color> { Colors.SteelBlue, Colors.SlateGray, Colors.LightSteelBlue };
            }
        }

        /// <summary>
        /// FNV-1a hash, stable across runs (unlike string.GetHashCode), so a cover always gets the same particles.
        /// </summary>
        private static int GetStableSeed(string value)
        {
            unchecked
            {
                uint hash = 2166136261;

                foreach (char c in value)
                {
                    hash ^= c;
                    hash *= 16777619;
                }

                return (int)hash;
            }
        }

        private static bool IsImageFile(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp";
        }
    }
}
