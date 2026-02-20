using GameTimeNext.Core.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace GameTimeNext.Core.Application.GTXMigration
{
    /// <summary>
    /// Für die Migration der alten GameTimeX Daten zu den neuen GameTimeNXT Daten verantwortlich
    /// </summary>
    public class GTXMigrationHelper
    {
        private string _rootFolder = string.Empty;
        private string _databaseFilePath = string.Empty;
        private string _startUpParmsFilePath = string.Empty;
        private string _imagesFolderPath = string.Empty;

        private SQLiteConnection _connectionOld = new SQLiteConnection();

        public GTXMigrationHelper(string rootFolder)
        {
            _rootFolder = rootFolder;

            _databaseFilePath = rootFolder + System.IO.Path.DirectorySeparatorChar + "GameTimeXDB.db";
            _startUpParmsFilePath = rootFolder + System.IO.Path.DirectorySeparatorChar + "startUpParms.json";
            _imagesFolderPath = rootFolder + System.IO.Path.DirectorySeparatorChar + "images";
        }

        public void MigrateToGTNXT()
        {
            // Verbindung zu alter Datenbank aufbauen
            bool connectSuccess = ConnectToSQLite();

            if (!connectSuccess)
                // Tu was
                return;

            // Profile migrieren
            MigrateProfiles();

            // Sessions
            MigrateSessions();

            // Profil Covers
            MigrateProfileImagesToCovers();

            // Alte Verbindung schließen
            _connectionOld.Close();

        }

        private bool ConnectToSQLite()
        {
            string connectionString;
            bool newDB;

            if (!File.Exists(_databaseFilePath))
            {
                return false;
            }
            else
            {
                connectionString = $"Data Source={_databaseFilePath};Version=3;Compress=True;BusyTimeout=15000;Pooling=False;";
            }

            _connectionOld = new SQLiteConnection(connectionString);
            try { _connectionOld.Open(); } catch { }

            try
            {
                using var fkOn = new SQLiteCommand("PRAGMA foreign_keys = ON;", _connectionOld);
                fkOn.ExecuteNonQuery();
            }
            catch { }

            return true;
        }

        private bool MigrateProfiles()
        {
            bool success = true;

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ProfileID, GameName, FirstPlay, LastPlay, ProfilePicFileName, ExtGameFolder, CreatedAt, ChangedAt, SteamAppID, ProfileSettings, Executables, PlaythroughStartPointDate ");
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
                            // Werte aus alter DB
                            int pfid = reader.GetInt32(0);
                            string gana = reader.GetString(1);

                            DateTime fipl = ParseOldDateTime(reader.GetValue(2));
                            DateTime lapl = ParseOldDateTime(reader.GetValue(3));

                            string ppfn = AppEnvironment.GetAppConfig().CoverFolderPath + System.IO.Path.DirectorySeparatorChar + reader.GetString(4);
                            string exgf = reader.GetString(5);

                            DateTime crat = ParseOldDateTime(reader.GetString(6));
                            DateTime chat = ParseOldDateTime(reader.GetString(7));

                            int said = reader.GetInt32(8);
                            string prse = reader.GetString(9);
                            string exec = reader.GetString(10);

                            DateTime plsp = ParseOldDateTime(reader.GetString(11));

                            // INSERT in neue DB
                            using (SQLiteCommand insertCmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand())
                            {
                                insertCmd.CommandText =
                                    "INSERT INTO TBL_PROFI " +
                                    "(PFID, GANA, FIPL, LAPL, PPFN, EXGF, SAID, PRSE, EXEC, PLSP, CRAT, CHAT) " +
                                    "VALUES " +
                                    "(@PFID, @GANA, @FIPL, @LAPL, @PPFN, @EXGF, @SAID, @PRSE, @EXEC, @PLSP, @CRAT, @CHAT);";

                                insertCmd.Parameters.AddWithValue("@PFID", pfid);
                                insertCmd.Parameters.AddWithValue("@GANA", gana);

                                insertCmd.Parameters.AddWithValue("@FIPL", ToDbDateTime(fipl));
                                insertCmd.Parameters.AddWithValue("@LAPL", ToDbDateTime(lapl));

                                insertCmd.Parameters.AddWithValue("@PPFN", ppfn);
                                insertCmd.Parameters.AddWithValue("@EXGF", exgf);

                                insertCmd.Parameters.AddWithValue("@SAID", said);
                                insertCmd.Parameters.AddWithValue("@PRSE", prse);
                                insertCmd.Parameters.AddWithValue("@EXEC", exec);

                                insertCmd.Parameters.AddWithValue("@PLSP", ToDbDateTime(plsp));

                                insertCmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(crat));
                                insertCmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(chat));

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

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT SID, FK_PID, Played_From, Played_To, Playtime ");
            sb.Append("from tblGameSessions");

            try
            {
                using (var cmd = _connectionOld.CreateCommand())
                {
                    cmd.CommandText = sb.ToString();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int seid = reader.GetInt32(0);
                            int pfid = reader.GetInt32(1);

                            DateTime plfr = ParseOldDateTime(reader.GetValue(2));
                            DateTime plto = ParseOldDateTime(reader.GetValue(3));

                            double plti = Convert.ToDouble(reader.GetValue(4));

                            using (SQLiteCommand insertCmd = AppEnvironment.GetDataBaseManager().GetConnection().CreateCommand())
                            {
                                insertCmd.CommandText =
                                    "INSERT INTO TBL_SESSI " +
                                    "(SEID, PFID, PLFR, PLTO, PLTI, CRAT, CHAT) " +
                                    "VALUES " +
                                    "(@SEID, @PFID, @PLFR, @PLTO, @PLTI, @CRAT, @CHAT);";

                                insertCmd.Parameters.AddWithValue("@SEID", seid);
                                insertCmd.Parameters.AddWithValue("@PFID", pfid);

                                insertCmd.Parameters.AddWithValue("@PLFR", ToDbDateTime(plfr));
                                insertCmd.Parameters.AddWithValue("@PLTO", ToDbDateTime(plto));

                                insertCmd.Parameters.AddWithValue("@PLTI", plti);

                                // In der alten Tabelle sind CRAT/CHAT nicht im SELECT, daher wie "CreateNew": jetzt
                                DateTime now = DateTime.Now;
                                insertCmd.Parameters.AddWithValue("@CRAT", ToDbDateTime(now));
                                insertCmd.Parameters.AddWithValue("@CHAT", ToDbDateTime(now));

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

        // ISO/Fallbacks
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
                //FileHandler.CopyDirectory(_imagesFolderPath, AppEnvironment.GetAppConfig().CoverFolderTempPath, false);
                ConvertFolderToNewFormat(_imagesFolderPath, AppEnvironment.GetAppConfig().CoverFolderPath);
            }
            catch (Exception ex) { success = false; return success; }

            try
            {
                //ConvertFolderToNewFormat(AppEnvironment.GetAppConfig().CoverFolderTempPath, AppEnvironment.GetAppConfig().CoverFolderPath);
            }
            catch (Exception ex) { success = false; }

            return success;
        }

        public static void ConvertFolderToNewFormat(string sourceFolder, string targetFolder)
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

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];

                if (!IsImageFile(path))
                {
                    continue;
                }

                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string targetPath = System.IO.Path.Combine(targetFolder, fileName + ".jpg");

                ConvertSingleImage(path, targetPath);
            }
        }

        private static void ConvertSingleImage(string inputPath, string outputPath)
        {
            const int targetW = 600;
            const int targetH = 900;

            int maxImageSize = 600; // Bild bleibt quadratisch, maximal 600x600

            int topThicknessLeft = 140;
            int topThicknessRight = 80;

            int bottomThicknessLeft = 80;
            int bottomThicknessRight = 140;

            using (Image<Rgba32> src = Image.Load<Rgba32>(inputPath))
            {
                // Bild nicht strecken: nur so skalieren, dass es maximal 600x600 ist
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

                    // Hintergrund: dunkelgrau statt weiß
                    Color background = Color.FromRgba(18, 18, 18, 255);

                    using (Image<Rgba32> canvas = new Image<Rgba32>(targetW, targetH, background))
                    {
                        int x = (targetW - img.Width) / 2;
                        int y = (targetH - img.Height) / 2;

                        canvas.Mutate(ctx =>
                        {
                            // Bild mittig platzieren
                            ctx.DrawImage(img, new Point(x, y), 1f);

                            // Balken darüber (leicht transparent)
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
            // Etwas transparent wirkt meistens „wertiger“ als komplett deckend
            Color overlayBlack = Color.FromRgba(0, 0, 0, 220);
            Color edgeBlack = Color.FromRgba(0, 0, 0, 255);

            // Oben: Polygon
            IPath topShape = new Polygon(new LinearLineSegment(
                new PointF(0, 0),
                new PointF(targetW, 0),
                new PointF(targetW, topThicknessRight),
                new PointF(0, topThicknessLeft)
            ));
            ctx.Fill(overlayBlack, topShape);

            // Unterkante der oberen Schräge als „saubere Linie“
            ctx.DrawLine(edgeBlack, 3f, new PointF(0, topThicknessLeft), new PointF(targetW, topThicknessRight));

            // Unten: Polygon
            IPath bottomShape = new Polygon(new LinearLineSegment(
                new PointF(0, targetH),
                new PointF(targetW, targetH),
                new PointF(targetW, targetH - bottomThicknessRight),
                new PointF(0, targetH - bottomThicknessLeft)
            ));
            ctx.Fill(overlayBlack, bottomShape);

            // Oberkante der unteren Schräge als „saubere Linie“
            ctx.DrawLine(edgeBlack, 3f, new PointF(0, targetH - bottomThicknessLeft), new PointF(targetW, targetH - bottomThicknessRight));
        }

        private static bool IsImageFile(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".webp";
        }




    }
}
