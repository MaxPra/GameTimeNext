using GameTimeNext.Core.Framework.Utils;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Own mouse cursors of the application (arrow, hand, text, wait, resize, ...): black with a thin white outline, the
    /// busy ring takes the brighter accent segment of the app icon. They are drawn as vectors at startup (sharp at every DPI) and replace the
    /// system cursors wherever a window of this app asks for one (<c>Cursor="Hand"</c> in XAML stays as it is), so outside
    /// of the app the normal cursors remain.
    /// </summary>
    public static class CFCursors
    {
        private const double Grid = 32;
        private const double BaseSize = 24;

        private static readonly Color Body = Color.FromRgb(0x0D, 0x0F, 0x14);
        private static readonly Color Outline = Color.FromRgb(0xFF, 0xFF, 0xFF);
        private static readonly Color Segment = Color.FromRgb(0x5D, 0xB6, 0xF3);

        private static readonly Dictionary<Cursor, Cursor> Replacements = new Dictionary<Cursor, Cursor>();
        private static Cursor? _arrow;
        private static bool _applied;

        /// <summary>
        /// Switches the app cursors on or off (setting "Own mouse cursor"); takes effect with the next mouse move.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Creates the cursors and switches them on for all windows of the app. Called once at startup; if the cursors can't
        /// be created the system cursors stay.
        /// </summary>
        public static void Apply()
        {
            if (_applied)
            {
                return;
            }

            _applied = true;

            try
            {
                int size = Math.Max(16, (int)Math.Round(BaseSize * GetDpiForSystem() / 96.0));

                _arrow = Create(size, 5.6, 3.8, DrawArrow);
                Replacements[Cursors.Arrow] = _arrow;
                Replacements[Cursors.Hand] = Create(size, 12.7, 3.8, DrawHand);
                Replacements[Cursors.IBeam] = Create(size, 16, 16, DrawIBeam);
                Replacements[Cursors.Wait] = Create(size, 16, 16, dc => DrawRing(dc, 16, 16, 9.5, 4));
                Replacements[Cursors.AppStarting] = Create(size, 5.6, 3.8, DrawAppStarting);
                Replacements[Cursors.SizeWE] = Create(size, 16, 16, dc => DrawSolid(dc, DoubleArrow(0)));
                Replacements[Cursors.SizeNS] = Create(size, 16, 16, dc => DrawSolid(dc, DoubleArrow(90)));
                Replacements[Cursors.SizeNWSE] = Create(size, 16, 16, dc => DrawSolid(dc, DoubleArrow(45)));
                Replacements[Cursors.SizeNESW] = Create(size, 16, 16, dc => DrawSolid(dc, DoubleArrow(-45)));
                Replacements[Cursors.SizeAll] = Create(size, 16, 16, dc => DrawSolid(dc, Cross()));
                Replacements[Cursors.No] = Create(size, 16, 16, DrawForbidden);

                // The handlers also run for already handled events (the element's own Cursor handles the event first); the
                // mapping is repeated on every element of the route, so the root of the route sees the final cursor.
                EventManager.RegisterClassHandler(typeof(UIElement), Mouse.QueryCursorEvent,
                    new QueryCursorEventHandler(OnQueryCursor), true);
                EventManager.RegisterClassHandler(typeof(ContentElement), Mouse.QueryCursorEvent,
                    new QueryCursorEventHandler(OnQueryCursor), true);
            }
            catch (Exception ex)
            {
                Replacements.Clear();
                _arrow = null;
                FnLog.AddWarning(nameof(CFCursors), "The app cursors could not be created, the system cursors are used instead.", ex);
            }
        }

        private static void OnQueryCursor(object sender, QueryCursorEventArgs e)
        {
            if (!Enabled)
            {
                return;
            }

            if (e.Cursor != null)
            {
                if (Replacements.TryGetValue(e.Cursor, out Cursor? replacement))
                {
                    e.Cursor = replacement;
                }

                return;
            }

            // Nothing asked for a cursor so far: the top of a visual tree (window, popup) falls back to the app arrow. An
            // element further up the route (e.g. the owner of a popup) can still set its own cursor afterwards.
            if (!e.Handled && _arrow != null && sender is UIElement element && VisualTreeHelper.GetParent(element) == null)
            {
                e.Cursor = _arrow;
            }
        }

        #region Drawing

        private static void DrawArrow(DrawingContext dc)
        {
            DrawAccent(dc, ArrowGeometry());
        }

        private static void DrawAppStarting(DrawingContext dc)
        {
            DrawAccent(dc, ArrowGeometry());
            DrawRing(dc, 24, 24, 5.5, 2.6);
        }

        private static void DrawHand(DrawingContext dc)
        {
            Draw(dc, HandGeometry());

            // Fine lines between the folded fingers
            Pen separator = StrokePen(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF), 0.9, true);
            dc.DrawLine(separator, new Point(18.7, 14.6), new Point(18.7, 19.4));
            dc.DrawLine(separator, new Point(22.5, 15.2), new Point(22.5, 19.4));
        }

        private static void DrawIBeam(DrawingContext dc)
        {
            DrawSolid(dc, new CombinedGeometry(GeometryCombineMode.Union,
                new CombinedGeometry(GeometryCombineMode.Union,
                    new RectangleGeometry(new Rect(14.5, 5, 3, 22), 1.5, 1.5),
                    new RectangleGeometry(new Rect(10.5, 3.5, 11, 3.2), 1.6, 1.6)),
                new RectangleGeometry(new Rect(10.5, 25.3, 11, 3.2), 1.6, 1.6)));
        }

        private static void DrawForbidden(DrawingContext dc)
        {
            Geometry circle = new EllipseGeometry(new Point(16, 16), 9.5, 9.5);
            Geometry slash = new LineGeometry(new Point(9.3, 9.3), new Point(22.7, 22.7));

            foreach (Geometry geometry in new[] { circle, slash })
            {
                dc.DrawGeometry(null, StrokePen(Outline, 6.4, true), geometry);
            }

            foreach (Geometry geometry in new[] { circle, slash })
            {
                dc.DrawGeometry(null, StrokePen(Body, 3.4, true), geometry);
            }
        }

        /// <summary>
        /// The mark of the app (open ring with a brighter segment at the upper left) as busy indicator.
        /// </summary>
        private static void DrawRing(DrawingContext dc, double cx, double cy, double radius, double width)
        {
            Geometry main = Arc(cx, cy, radius, 290, 170, true);
            Geometry segment = Arc(cx, cy, radius, 180, 270, false);

            dc.DrawGeometry(null, StrokePen(Outline, width + 2.8, true), main);
            dc.DrawGeometry(null, StrokePen(Outline, width + 2.8, true), segment);
            dc.DrawGeometry(null, StrokePen(Body, width, true), main);
            dc.DrawGeometry(null, StrokePen(Segment, width, true), segment);
        }

        private static void DrawAccent(DrawingContext dc, Geometry geometry)
        {
            Draw(dc, geometry);
        }

        private static void DrawSolid(DrawingContext dc, Geometry geometry)
        {
            Draw(dc, geometry);
        }

        /// <summary>
        /// Black shape with a thin white outline, readable on dark and light backgrounds.
        /// </summary>
        private static void Draw(DrawingContext dc, Geometry geometry)
        {
            dc.DrawGeometry(Frozen(Outline), StrokePen(Outline, 3.6, true), geometry);
            dc.DrawGeometry(Frozen(Body), StrokePen(Body, 1.8, true), geometry);
        }

        private static Pen StrokePen(Color color, double thickness, bool roundCaps)
        {
            Pen pen = new Pen(Frozen(color), thickness) { LineJoin = PenLineJoin.Round };

            if (roundCaps)
            {
                pen.StartLineCap = PenLineCap.Round;
                pen.EndLineCap = PenLineCap.Round;
            }

            pen.Freeze();
            return pen;
        }

        private static Brush Frozen(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        #endregion

        #region Shapes

        /// <summary>
        /// Slim pointer with a vertical left edge and a short slanted tail. The points lie inside the final shape: the
        /// round pen in <see cref="Draw"/> adds about 0.9 around them and rounds every corner.
        /// </summary>
        private static Geometry ArrowGeometry()
        {
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(6.2, 4.6), true, true);
                ctx.LineTo(new Point(6.2, 22.6), true, true);
                ctx.LineTo(new Point(9.8, 19.6), true, true);
                ctx.LineTo(new Point(12.4, 25.8), true, true);
                ctx.LineTo(new Point(15.4, 24.5), true, true);
                ctx.LineTo(new Point(12.8, 18.4), true, true);
                ctx.LineTo(new Point(18.8, 18.2), true, true);
            }

            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// Pointing hand: round index finger, three folded fingers as soft bumps, slanted thumb at the left.
        /// </summary>
        private static Geometry HandGeometry()
        {
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(10.6, 9), true, true);
                ctx.LineTo(new Point(10.6, 6.4), true, true);
                ctx.BezierTo(new Point(10.6, 3.9), new Point(14.8, 3.9), new Point(14.8, 6.4), true, true);
                ctx.LineTo(new Point(14.8, 12.8), true, true);
                ctx.BezierTo(new Point(14.8, 11.4), new Point(18.7, 11.4), new Point(18.7, 12.8), true, true);
                ctx.BezierTo(new Point(18.7, 11.8), new Point(22.5, 11.8), new Point(22.5, 13.2), true, true);
                ctx.BezierTo(new Point(22.5, 12.4), new Point(26.2, 12.4), new Point(26.2, 14.2), true, true);
                ctx.LineTo(new Point(26.2, 20), true, true);
                ctx.BezierTo(new Point(26.2, 24.6), new Point(23.5, 27.5), new Point(19.5, 27.5), true, true);
                ctx.LineTo(new Point(15.5, 27.5), true, true);
                ctx.BezierTo(new Point(13.5, 27.5), new Point(12, 26.6), new Point(10.8, 25), true, true);
                ctx.LineTo(new Point(5.6, 19.2), true, true);
                ctx.BezierTo(new Point(4.6, 18), new Point(6, 16.4), new Point(7.6, 17.2), true, true);
                ctx.LineTo(new Point(10.6, 19.6), true, true);
            }

            geometry.Freeze();
            return geometry;
        }

        private static Geometry DoubleArrow(double angle)
        {
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(3, 16), true, true);
                ctx.LineTo(new Point(10, 9.5), true, true);
                ctx.LineTo(new Point(10, 13.8), true, true);
                ctx.LineTo(new Point(22, 13.8), true, true);
                ctx.LineTo(new Point(22, 9.5), true, true);
                ctx.LineTo(new Point(29, 16), true, true);
                ctx.LineTo(new Point(22, 22.5), true, true);
                ctx.LineTo(new Point(22, 18.2), true, true);
                ctx.LineTo(new Point(10, 18.2), true, true);
                ctx.LineTo(new Point(10, 22.5), true, true);
            }

            geometry.Transform = new RotateTransform(angle, 16, 16);
            geometry.Freeze();
            return geometry;
        }

        private static Geometry Cross()
        {
            Geometry cross = new CombinedGeometry(GeometryCombineMode.Union, DoubleArrow(0), DoubleArrow(90));
            cross.Freeze();
            return cross;
        }

        private static Geometry Arc(double cx, double cy, double radius, double fromDegrees, double toDegrees, bool large)
        {
            PathFigure figure = new PathFigure { StartPoint = PointOnCircle(cx, cy, radius, fromDegrees), IsClosed = false };
            figure.Segments.Add(new ArcSegment(PointOnCircle(cx, cy, radius, toDegrees), new Size(radius, radius), 0, large,
                SweepDirection.Clockwise, true));

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();
            return geometry;
        }

        private static Point PointOnCircle(double cx, double cy, double radius, double degrees)
        {
            double radians = degrees * Math.PI / 180;
            return new Point(cx + radius * Math.Cos(radians), cy + radius * Math.Sin(radians));
        }

        #endregion

        #region Native cursor

        /// <summary>
        /// Renders the drawing (on a 32 x 32 grid) into a bitmap of the given size and makes a cursor of it.
        /// </summary>
        private static Cursor Create(int size, double hotspotX, double hotspotY, Action<DrawingContext> draw)
        {
            double scale = size / Grid;
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(scale, scale));
                draw(dc);
                dc.Pop();
            }

            RenderTargetBitmap target = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);

            // A cursor bitmap has straight (not premultiplied) alpha
            FormatConvertedBitmap straight = new FormatConvertedBitmap(target, PixelFormats.Bgra32, null, 0);
            byte[] pixels = new byte[size * size * 4];
            straight.CopyPixels(pixels, size * 4, 0);

            IntPtr color = CreateBitmap(size, size, 1, 32, pixels);
            IntPtr mask = CreateBitmap(size, size, 1, 1, new byte[((size + 15) / 16) * 2 * size]);

            try
            {
                ICONINFO info = new ICONINFO
                {
                    fIcon = false,
                    xHotspot = (int)Math.Round(hotspotX * scale),
                    yHotspot = (int)Math.Round(hotspotY * scale),
                    hbmMask = mask,
                    hbmColor = color
                };

                IntPtr handle = CreateIconIndirect(ref info);

                if (handle == IntPtr.Zero)
                {
                    throw new InvalidOperationException("CreateIconIndirect failed.");
                }

                return CursorInteropHelper.Create(new CursorHandle(handle));
            }
            finally
            {
                DeleteObject(color);
                DeleteObject(mask);
            }
        }

        private sealed class CursorHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public CursorHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return DestroyIcon(handle);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, byte[] lpvBits);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion
    }
}
