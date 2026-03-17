using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.Profiles.Components;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Data.SQLite;
using System.IO;
using System.Text;
using UIX.ViewController.Engine.FrameworkElements.Loader;
using UIX.ViewController.Engine.Querying;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.GTXMigration
{
    /// <summary>
    /// Für die Migration der alten GameTimeX Daten zu den neuen GameTimeNXT Daten verantwortlich
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

            _databaseFilePath = rootFolder + System.IO.Path.DirectorySeparatorChar + "GameTimeXDB.db";
            _startUpParmsFilePath = rootFolder + System.IO.Path.DirectorySeparatorChar + "startUpParms.json";
            _imagesFolderPath = rootFolder + System.IO.Path.DirectorySeparatorChar + "images";

            _loader = loader;
        }

        public void MigrateToGTNXT()
        {
            _loader!.SetTextStep("connecting to old database");

            // Verbindung zu alter Datenbank aufbauen
            bool connectSuccess = ConnectToSQLite();

            if (!connectSuccess)
                return;

            _loader.SetTextStep("migrating profile data");
            // Profile migrieren
            MigrateProfiles();

            _loader.SetTextStep("migrating session data");
            // Sessions
            MigrateSessions();

            // Playthroughs
            _loader.SetTextStep("migrating playthroughs [NEW]");
            MigratePlaythroughs();

            _loader.SetTextStep("migrating profile covers");
            // Profil Covers
            MigrateProfileImagesToCovers();

            // Alte Verbindung schließen
            _connectionOld.Close();
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

                            string ppfn = AppEnvironment.GetAppConfig().CoverFolderPath + System.IO.Path.DirectorySeparatorChar + reader.GetString(4);
                            string exgf = reader.GetString(5);

                            DateTime crat = ParseOldDateTime(reader.GetString(6));
                            DateTime chat = ParseOldDateTime(reader.GetString(7));

                            int said = reader.GetInt32(8);
                            string prse = reader.GetString(9);
                            string exec = reader.GetString(10);

                            long cupt = 0;

                            List<System.Windows.Media.Color> accentColorsCalc = FnImage.GetTopAccentColors(_imagesFolderPath + System.IO.Path.DirectorySeparatorChar + reader.GetString(4), 3);
                            System.Windows.Media.Color accentColor = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[0].R, accentColorsCalc[0].G, accentColorsCalc[0].B);
                            string[] accentColors = FnTheme.CalculateAccentStateColors(accentColor.ToString());

                            Dictionary<string, string> dicAccentColors = new Dictionary<string, string>();
                            dicAccentColors.Add("accent", accentColors[0]);
                            dicAccentColors.Add("hover", accentColors[1]);
                            dicAccentColors.Add("pressed", accentColors[2]);

                            CAccentColors cAccentColors = new CAccentColors();
                            cAccentColors.AccentColors = dicAccentColors;

                            string acco = cAccentColors.Serialize();

                            string[] accentColorsInitArray = new string[3];
                            accentColorsInitArray[0] = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[0].R, accentColorsCalc[0].G, accentColorsCalc[0].B).ToString();
                            accentColorsInitArray[1] = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[1].R, accentColorsCalc[1].G, accentColorsCalc[1].B).ToString();
                            accentColorsInitArray[2] = System.Windows.Media.Color.FromArgb(255, accentColorsCalc[2].R, accentColorsCalc[2].G, accentColorsCalc[2].B).ToString();

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
                                    "(PFID, GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, CRAT, CHAT, ACCO, ACIN, ACAC, CUPT) " +
                                    "VALUES " +
                                    "(@PFID, @GANA, @FIPL, @LAPL, @PPFN, @EXGF, @SAID, @PRSE, @EXEC, @CRAT, @CHAT, @ACCO, @ACIN, @ACAC, @CUPT);";

                                insertCmd.Parameters.AddWithValue("@PFID", pfid);
                                insertCmd.Parameters.AddWithValue("@GANA", gana);

                                insertCmd.Parameters.AddWithValue("@FIPL", ToDbDateTime(fipl));
                                insertCmd.Parameters.AddWithValue("@LAPL", ToDbDateTime(lapl));

                                insertCmd.Parameters.AddWithValue("@PPFN", ppfn);
                                insertCmd.Parameters.AddWithValue("@EXGF", exgf);

                                insertCmd.Parameters.AddWithValue("@SAID", said);
                                insertCmd.Parameters.AddWithValue("@PRSE", prse);
                                insertCmd.Parameters.AddWithValue("@EXEC", exec);

                                insertCmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(crat));
                                insertCmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(chat));

                                insertCmd.Parameters.AddWithValue("@ACCO", acco);
                                insertCmd.Parameters.AddWithValue("@ACIN", acin);
                                insertCmd.Parameters.AddWithValue("@ACAC", acac ? 1 : 0);

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
                // Gesamtanzahl ermitteln
                using (var countCmd = _connectionOld.CreateCommand())
                {
                    countCmd.CommandText = "SELECT COUNT(*) FROM tblGameSessions";
                    totalCount = Convert.ToInt32(countCmd.ExecuteScalar());
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

                            double plti = Convert.ToDouble(reader.GetValue(4));

                            using (SQLiteCommand insertCmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand())
                            {
                                insertCmd.CommandText =
                                    "INSERT INTO T1SESSI " +
                                    "(SEID, PFID, PLFR, PLTO, PLTI, CRAT, CHAT, PTID) " +
                                    "VALUES " +
                                    "(@SEID, @PFID, @PLFR, @PLTO, @PLTI, @CRAT, @CHAT, @PTID);";

                                insertCmd.Parameters.AddWithValue("@SEID", seid);
                                insertCmd.Parameters.AddWithValue("@PFID", pfid);

                                insertCmd.Parameters.AddWithValue("@PLFR", ToDbDateTime(plfr));
                                insertCmd.Parameters.AddWithValue("@PLTO", ToDbDateTime(plto));

                                insertCmd.Parameters.AddWithValue("@PLTI", plti);

                                DateTime now = DateTime.Now;
                                insertCmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(now));
                                insertCmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(now));

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
            // Alle Profile lasen
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

        private List<T1SESSI> GetAllT1SESSIsOrderedByPFID()
        {
            List<T1SESSI> t1sessis = new List<T1SESSI>();

            UIXQuery query = new UIXQuery(K1SESSI.Name, AppEnvironment.GetDataBaseManager().GetConnection());

            query.AddField(K1SESSI.Name, K1SESSI.Fields.SEID);
            query.AddField(K1SESSI.Name, K1SESSI.Fields.PFID);

            query.AddOrderBy(K1SESSI.Name, K1SESSI.Fields.PFID, OrderDirection.ASC);

            using (var reader = query.Execute())
            {
                while (reader.Read())
                {
                    long seid = UIXQuery.GetInt64(reader, K1SESSI.Name, K1SESSI.Fields.SEID);

                    t1sessis.Add(new TXSESSI().Read(seid));
                }
            }

            return t1sessis;
        }

        private DateTime ParseOldDateTime(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return DateTime.MinValue;
            }

            string str = value.ToString();

            if (string.IsNullOrWhiteSpace(str))
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

        private string ToDbDateTime(DateTime value)
        {
            if (value == DateTime.MinValue)
            {
                return "0001-01-01 00:00:00";
            }

            return value.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        private bool MigrateProfileImagesToCovers()
        {
            bool success = true;

            try
            {
                ConvertFolderToNewFormat(_imagesFolderPath, AppEnvironment.GetAppConfig().CoverFolderPath);
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

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

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

            using (Image<Rgba32> src = Image.Load<Rgba32>(inputPath))
            {
                using (Image<Rgba32> img = src.Clone())
                {
                    img.Mutate(ctx =>
                    {
                        ResizeOptions opt = new ResizeOptions();
                        opt.Size = new Size(maxImageSize, maxImageSize);
                        opt.Mode = ResizeMode.Max;
                        opt.Position = AnchorPositionMode.Center;

                        ctx.Resize(opt);
                    });

                    Color background = Color.FromRgba(18, 18, 18, 255);

                    using (Image<Rgba32> canvas = new Image<Rgba32>(targetW, targetH, background))
                    {
                        int x = (targetW - img.Width) / 2;
                        int y = (targetH - img.Height) / 2;

                        canvas.Mutate(ctx =>
                        {
                            ctx.DrawImage(img, new Point(x, y), 1f);

                            DrawSlantedOverlays(
                                ctx,
                                targetW,
                                targetH,
                                topThicknessLeft,
                                topThicknessRight,
                                bottomThicknessLeft,
                                bottomThicknessRight
                            );
                        });

                        canvas.Save(outputPath);
                    }
                }
            }
        }

        private static void ResizeTo600x900(IImageProcessingContext ctx)
        {
            ResizeOptions opt = new ResizeOptions();
            opt.Size = new Size(600, 900);
            opt.Mode = ResizeMode.Crop;
            opt.Position = AnchorPositionMode.Center;

            ctx.Resize(opt);
        }

        private static void DrawSlantedOverlays(
            IImageProcessingContext ctx,
            int targetW,
            int targetH,
            int topThicknessLeft,
            int topThicknessRight,
            int bottomThicknessLeft,
            int bottomThicknessRight)
        {
            Color overlayBlack = Color.FromRgba(0, 0, 0, 220);
            Color edgeBlack = Color.FromRgba(0, 0, 0, 255);

            IPath topShape = new Polygon(new LinearLineSegment(
                new PointF(0, 0),
                new PointF(targetW, 0),
                new PointF(targetW, topThicknessRight),
                new PointF(0, topThicknessLeft)
            ));
            ctx.Fill(overlayBlack, topShape);

            ctx.DrawLine(edgeBlack, 3f, new PointF(0, topThicknessLeft), new PointF(targetW, topThicknessRight));

            IPath bottomShape = new Polygon(new LinearLineSegment(
                new PointF(0, targetH),
                new PointF(targetW, targetH),
                new PointF(targetW, targetH - bottomThicknessRight),
                new PointF(0, targetH - bottomThicknessLeft)
            ));
            ctx.Fill(overlayBlack, bottomShape);

            ctx.DrawLine(edgeBlack, 3f, new PointF(0, targetH - bottomThicknessLeft), new PointF(targetW, targetH - bottomThicknessRight));
        }

        private static bool IsImageFile(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp";
        }
    }
}