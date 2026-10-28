using GameTimeNext.Core.Framework.Config;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UIX.ViewController.Engine.Utils;
using DataWriter = Windows.Storage.Streams.DataWriter;
using OcrEngine = Windows.Media.Ocr.OcrEngine;
using OcrLine = Windows.Media.Ocr.OcrLine;
using OcrResult = Windows.Media.Ocr.OcrResult;
using OcrWord = Windows.Media.Ocr.OcrWord;
using SoftwareBitmap = Windows.Graphics.Imaging.SoftwareBitmap;
using WinAlphaMode = Windows.Graphics.Imaging.BitmapAlphaMode;
using WinLanguage = Windows.Globalization.Language;
using WinPixelFormat = Windows.Graphics.Imaging.BitmapPixelFormat;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Prints a cover on a disc (styles GameCase, GameCase.Showcase and GameCase.Preview in TileViewStyle.xaml): set Cover on the disc's
    /// Ellipse and it gets the cover as fill, with a section that shows the game title completely.
    ///
    /// The title is found with the Windows OCR (largest text line plus the lines right next to it, padded). The section
    /// is then chosen so that every title line lies completely
    /// - within the printed ring (outside the hub, inside the rim),
    /// - in the upper part of the print that stays visible in the selected case: turned by PrintAngle, the print ends a
    ///   quarter turn clockwise after the spin of the selected disc (SbSelect), so its upper part is then the right side,
    ///   the only part not covered by the opened front cover. The title runs down that side like on a spine,
    /// - and the section lies within the cover (the print always covers the whole disc).
    /// These are hard conditions; among the sections that meet them, the one closest to the cover's width and centered
    /// wins. When there is none (or no text is found), the disc shows the centered square, not turned (like Stretch
    /// UniformToFill), which is also shown until the analysis is done.
    /// Results are cached per image content, also on disk (AppConfig.Temp.DiscPrintCacheFilePath), and the analyses run
    /// one after the other on a background thread, so a library with many covers stays responsive.
    /// </summary>
    public static class DiscPrint
    {
        /// <summary>Changes with the analysis; older cache entries are ignored then.</summary>
        private const string AnalysisVersion = "2";

        /// <summary>Hub (44 of 112) and rim (3 of 112) radius relative to the disc radius, with a margin.</summary>
        private const double HubRadius = 0.43;
        private const double PrintRadius = 0.92;

        /// <summary>
        /// Visible part of the selected disc: everything right of the opened front cover's free edge. With the cover at
        /// -46 degrees around its hinge (x = -0.7) and the camera at z = 14, the edge is projected to x = 0.2937; the disc
        /// (center 0.15 after sliding out, radius 0.6374) is visible from 0.225 radii right of its center. With a margin
        /// for the cover's glass lip: 0.30. Measured in the print as distance above the center (quarter turn).
        /// </summary>
        private const double VisibleFromCenter = 0.30;

        /// <summary>The cover is scaled up to this width for the OCR (small text is not recognized otherwise).</summary>
        private const int OcrWidth = 600;

        /// <summary>Lines smaller than this (relative to the cover width) are fine print, not the title.</summary>
        private const double MinTitleLineHeight = 0.02;

        /// <summary>OCR boxes are tight around the letters; stylized logos reach a little beyond (relative to the line height).</summary>
        private const double TitlePadding = 0.2;

        public static readonly DependencyProperty CoverProperty =
            DependencyProperty.RegisterAttached(
                "Cover",
                typeof(ImageSource),
                typeof(DiscPrint),
                new PropertyMetadata(null, OnCoverChanged));

        public static ImageSource? GetCover(DependencyObject obj)
            => (ImageSource?)obj.GetValue(CoverProperty);

        public static void SetCover(DependencyObject obj, ImageSource? value)
            => obj.SetValue(CoverProperty, value);

        /// <summary>
        /// Angle (degrees, positive = clockwise on screen) the print is turned by when it shows the title section, so it
        /// ends a quarter turn clockwise after the disc's own spin. GameCase spins 120 degrees clockwise: -30.
        /// 0 for a disc without spin (the title stays upright at the top).
        /// </summary>
        public static readonly DependencyProperty PrintAngleProperty =
            DependencyProperty.RegisterAttached(
                "PrintAngle",
                typeof(double),
                typeof(DiscPrint),
                new PropertyMetadata(0.0));

        public static double GetPrintAngle(DependencyObject obj)
            => (double)obj.GetValue(PrintAngleProperty);

        public static void SetPrintAngle(DependencyObject obj, double value)
            => obj.SetValue(PrintAngleProperty, value);

        /// <summary>Lower limit of the disc radius (units of the cover width): the cover is enlarged at most 1.67 times.</summary>
        private const double MinRadius = 0.3;

        /// <summary>Section in units of the cover width (the cover is 1 wide and its aspect ratio high); Size 0: centered square.</summary>
        private sealed record DiscSection(double X, double Y, double Size);

        private static readonly ConcurrentDictionary<string, Task<DiscSection>> Analyses = new();

        // One analysis at a time: a library loads all covers at once
        private static readonly SemaphoreSlim AnalysisGate = new(1, 1);

        private static readonly Lazy<OcrEngine?> Engine = new(() =>
            OcrEngine.TryCreateFromUserProfileLanguages() ?? OcrEngine.TryCreateFromLanguage(new WinLanguage("en-US")));

        private static readonly Lazy<ConcurrentDictionary<string, DiscSection>> Cache = new(LoadCache);
        private static readonly object CacheFileLock = new();

        private static async void OnCoverChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Shape shape)
                return;

            shape.Fill = e.NewValue is ImageSource image ? new ImageBrush(image) { Stretch = Stretch.UniformToFill } : null;

            // Pixels are read on a background thread
            if (e.NewValue is not BitmapSource cover || !cover.IsFrozen || cover.PixelWidth <= 0 || cover.PixelHeight <= 0)
                return;

            try
            {
                DiscSection section = await Task.Run(() => GetSectionAsync(cover));

                if (section.Size > 0 && ReferenceEquals(GetCover(shape), cover))
                    shape.Fill = CreateBrush(cover, section, GetPrintAngle(shape));
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", "Disc print section of a cover could not be determined", ex);
            }
        }

        private static Task<DiscSection> GetSectionAsync(BitmapSource cover)
        {
            string key = GetKey(cover);

            if (Cache.Value.TryGetValue(key, out DiscSection? cached))
                return Task.FromResult(cached);

            return Analyses.GetOrAdd(key, _ => AnalyzeAsync(key, cover));
        }

        private static string GetKey(BitmapSource cover)
        {
            return AnalysisVersion + "_" + Convert.ToHexString(SHA256.HashData(ReadPixels(cover, 1).Data));
        }

        /// <summary>
        /// Gives a cover file the section of its preview (e.g. the SteamGridDB thumbnail shown before applying it), so its
        /// disc looks exactly like the preview wherever the file is shown, instead of being analyzed again on its own pixels
        /// (the OCR can find a different title box there). The file is read like the profile covers are loaded everywhere
        /// (FnImage.LoadImageWithoutLock with 300 x 450): the same pixels give the same cache key. Waits for the analysis
        /// of the preview if it is still running. Errors are logged; the cover is then analyzed on its own as before.
        /// </summary>
        public static async Task AdoptSectionAsync(BitmapSource preview, string coverFilePath)
        {
            try
            {
                if (!preview.IsFrozen || !File.Exists(coverFilePath))
                    return;

                DiscSection section = await Task.Run(() => GetSectionAsync(preview));

                await Task.Run(() =>
                {
                    BitmapImage? cover = FnImage.LoadImageWithoutLock(coverFilePath, 300, 450);

                    if (cover == null)
                        return;

                    Cache.Value[GetKey(cover)] = section;
                    SaveCache();
                });
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", "Disc print section could not be taken over from the cover preview", ex);
            }
        }

        private static Brush CreateBrush(BitmapSource cover, DiscSection section, double angle)
        {
            double aspect = (double)cover.PixelHeight / cover.PixelWidth;

            // Turned around the disc center; the disc stays covered (it is the circle inscribed in the section)
            var brush = new ImageBrush(cover)
            {
                Viewbox = new Rect(section.X, section.Y / aspect, section.Size, section.Size / aspect),
                ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                Stretch = Stretch.Fill,
                RelativeTransform = new RotateTransform(angle, 0.5, 0.5)
            };

            RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.HighQuality);
            brush.Freeze();

            return brush;
        }

        private static CoverPixels ReadPixels(BitmapSource cover, double scale)
        {
            BitmapSource source = scale > 1 ? new TransformedBitmap(cover, new ScaleTransform(scale, scale)) : cover;
            source = new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            byte[] data = new byte[width * height * 4];
            source.CopyPixels(data, width * 4, 0);

            return new CoverPixels(data, width, height);
        }

        private sealed record CoverPixels(byte[] Data, int Width, int Height);

        private static async Task<DiscSection> AnalyzeAsync(string key, BitmapSource cover)
        {
            DiscSection section = new DiscSection(0, 0, 0);

            await AnalysisGate.WaitAsync();

            try
            {
                List<Rect> title = await FindTitleAsync(cover);
                double aspect = (double)cover.PixelHeight / cover.PixelWidth;
                Rect? circle = title.Count > 0 ? FindSection(title, aspect) : null;

                if (circle is Rect c)
                    section = new DiscSection(c.X, c.Y, c.Width);
            }
            catch (Exception ex)
            {
                // Not cached: tried again with the next start
                FnLog.AddError("MainApp", "Text recognition on a cover failed", ex);
                return section;
            }
            finally
            {
                AnalysisGate.Release();
            }

            Cache.Value[key] = section;
            SaveCache();

            return section;
        }

        /// <summary>Pixel variants the OCR runs on; stylized logos are often only recognized in one of them.</summary>
        private enum OcrVariant
        {
            Color,
            Gray,
            GrayInverted
        }

        /// <summary>Padded boxes of the title lines, in units of the cover width; empty when no text was found.</summary>
        private static async Task<List<Rect>> FindTitleAsync(BitmapSource cover)
        {
            OcrEngine? engine = Engine.Value;

            if (engine == null)
                return [];

            var lines = new List<Rect>();

            foreach ((OcrVariant variant, int width) in new[] { (OcrVariant.Color, OcrWidth), (OcrVariant.Gray, 1000), (OcrVariant.GrayInverted, 1000) })
            {
                CoverPixels pixels = ReadPixels(cover, Math.Max(1, (double)width / cover.PixelWidth));

                if (variant != OcrVariant.Color)
                    ToGray(pixels, variant == OcrVariant.GrayInverted);

                foreach (Rect line in await RecognizeLinesAsync(engine, pixels))
                    AddMerged(lines, line);
            }

            if (lines.Count == 0)
                return [];

            // The title is the largest text, together with the lines right next to it (e.g. "The Legend of" above
            // "Zelda"); taglines, ratings and publisher names further away are left out
            Rect main = lines.MaxBy(l => l.Height);
            var title = new List<Rect> { main };
            Rect block = main;
            bool added = true;

            while (added)
            {
                added = false;

                foreach (Rect line in lines.Where(l => !title.Contains(l)).ToList())
                {
                    double gap = Math.Max(0, Math.Max(line.Top - block.Bottom, block.Top - line.Bottom));

                    if (line.Height >= main.Height * 0.3 && gap <= main.Height)
                    {
                        title.Add(line);
                        block.Union(line);
                        added = true;
                    }
                }
            }

            return title.Select(l =>
            {
                Rect padded = l;
                padded.Inflate(l.Height * TitlePadding, l.Height * TitlePadding);
                return padded;
            }).ToList();
        }

        /// <summary>Boxes of the recognized text lines, in units of the cover width.</summary>
        private static async Task<List<Rect>> RecognizeLinesAsync(OcrEngine engine, CoverPixels pixels)
        {
            OcrResult result;

            using (var writer = new DataWriter())
            {
                writer.WriteBytes(pixels.Data);

                using SoftwareBitmap bitmap = SoftwareBitmap.CreateCopyFromBuffer(
                    writer.DetachBuffer(), WinPixelFormat.Bgra8, pixels.Width, pixels.Height, WinAlphaMode.Premultiplied);

                result = await engine.RecognizeAsync(bitmap);
            }

            var lines = new List<Rect>();

            foreach (OcrLine line in result.Lines)
            {
                // Single characters are mostly misread artwork
                if (line.Text.Count(char.IsLetterOrDigit) < 2)
                    continue;

                Rect box = Rect.Empty;

                foreach (OcrWord word in line.Words)
                {
                    box.Union(new Rect(
                        word.BoundingRect.X / pixels.Width,
                        word.BoundingRect.Y / pixels.Width,
                        word.BoundingRect.Width / pixels.Width,
                        word.BoundingRect.Height / pixels.Width));
                }

                if (!box.IsEmpty && box.Height >= MinTitleLineHeight)
                    lines.Add(box);
            }

            return lines;
        }

        /// <summary>Adds a line; lines of other variants overlapping it are the same text and are joined to one box.</summary>
        private static void AddMerged(List<Rect> lines, Rect line)
        {
            Rect merged = line;
            bool joined = true;

            while (joined)
            {
                joined = false;

                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (lines[i].IntersectsWith(merged))
                    {
                        merged.Union(lines[i]);
                        lines.RemoveAt(i);
                        joined = true;
                    }
                }
            }

            lines.Add(merged);
        }

        /// <summary>Gray with the contrast stretched to the full range (optionally inverted), in place.</summary>
        private static void ToGray(CoverPixels pixels, bool invert)
        {
            byte[] data = pixels.Data;
            byte[] gray = new byte[data.Length / 4];
            byte min = 255, max = 0;

            for (int i = 0; i < gray.Length; i++)
            {
                gray[i] = (byte)((data[i * 4] * 29 + data[i * 4 + 1] * 150 + data[i * 4 + 2] * 77) >> 8);
                min = Math.Min(min, gray[i]);
                max = Math.Max(max, gray[i]);
            }

            double range = Math.Max(1, max - min);

            for (int i = 0; i < gray.Length; i++)
            {
                byte value = (byte)((gray[i] - min) * 255 / range);

                if (invert)
                    value = (byte)(255 - value);

                data[i * 4] = value;
                data[i * 4 + 1] = value;
                data[i * 4 + 2] = value;
                data[i * 4 + 3] = 255;
            }
        }

        /// <summary>
        /// The disc section (square within the cover, in units of the cover width) whose printed ring holds every title box
        /// completely in its visible upper part (see the class description); null when there is none.
        /// For a center, the conditions give an exact range of radii; the largest one (closest to the cover's width) is taken.
        /// </summary>
        private static Rect? FindSection(List<Rect> title, double aspect)
        {
            double titleBottom = title.Max(b => b.Bottom);
            Rect? best = null;
            double bestCost = double.MaxValue;

            for (double cx = MinRadius; cx <= 1 - MinRadius + 1e-9; cx += 0.01)
            {
                for (double cy = titleBottom + 0.01; cy <= aspect - MinRadius + 1e-9; cy += 0.01)
                {
                    double farthest = 0;
                    double nearest = double.MaxValue;

                    foreach (Rect box in title)
                    {
                        farthest = Math.Max(farthest, FarthestDistance(box, cx, cy));
                        nearest = Math.Min(nearest, NearestDistance(box, cx, cy));
                    }

                    double withinCover = Math.Min(Math.Min(cx, 1 - cx), Math.Min(cy, aspect - cy));
                    double minRadius = Math.Max(MinRadius, farthest / PrintRadius);
                    double maxRadius = Math.Min(withinCover, Math.Min(nearest / HubRadius, (cy - titleBottom) / VisibleFromCenter));

                    if (minRadius > maxRadius)
                        continue;

                    double radius = Math.Min(0.5, maxRadius);
                    double cost = Math.Abs(Math.Log(radius * 2)) + 0.5 * Math.Abs(cx - 0.5);

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = new Rect(cx - radius, cy - radius, radius * 2, radius * 2);
                    }
                }
            }

            return best;
        }


        private static double FarthestDistance(Rect box, double x, double y)
        {
            double dx = Math.Max(Math.Abs(box.Left - x), Math.Abs(box.Right - x));
            double dy = Math.Max(Math.Abs(box.Top - y), Math.Abs(box.Bottom - y));

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double NearestDistance(Rect box, double x, double y)
        {
            double dx = Math.Max(0, Math.Max(box.Left - x, x - box.Right));
            double dy = Math.Max(0, Math.Max(box.Top - y, y - box.Bottom));

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static ConcurrentDictionary<string, DiscSection> LoadCache()
        {
            try
            {
                string path = AppConfig.Temp.DiscPrintCacheFilePath;

                if (File.Exists(path))
                {
                    var entries = JsonSerializer.Deserialize<Dictionary<string, DiscSection>>(File.ReadAllText(path));

                    if (entries != null)
                        return new ConcurrentDictionary<string, DiscSection>(entries.Where(e => e.Key.StartsWith(AnalysisVersion + "_", StringComparison.Ordinal)));
                }
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", "Disc print cache could not be read, covers are analyzed again", ex);
            }

            return new ConcurrentDictionary<string, DiscSection>();
        }

        private static void SaveCache()
        {
            try
            {
                lock (CacheFileLock)
                {
                    File.WriteAllText(AppConfig.Temp.DiscPrintCacheFilePath, JsonSerializer.Serialize(Cache.Value));
                }
            }
            catch (Exception ex)
            {
                FnLog.AddError("MainApp", "Disc print cache could not be written", ex);
            }
        }
    }
}
