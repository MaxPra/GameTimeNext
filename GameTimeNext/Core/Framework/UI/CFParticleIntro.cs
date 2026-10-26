using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Opening animation in the spirit of the Xbox 360 start: streaks of light swirl in on tilted orbits around the
    /// center and are drawn into it, a flash and a shock wave let the logo appear, the title fades in below, then the
    /// whole animation fades out and reveals what is behind it. Covers its area with a dark background while it runs.
    /// Play starts it, Skip (also a click) jumps to the fade out, Completed is raised at the end.
    /// </summary>
    public sealed class CFParticleIntro : FrameworkElement
    {
        // -- Timeline in seconds
        private const double ConvergeEnd = 1.35;
        private const double MaxParticleDelay = 0.45;
        private const double FlashDuration = 0.6;
        private const double ShockwaveDuration = 0.85;
        private const double LogoInDuration = 0.5;
        private const double TextStart = 1.8;
        private const double TextDuration = 0.45;
        private const double FadeStart = 2.75;
        private const double FadeDuration = 0.45;

        private const int ParticleCount = 260;
        private const int BrushLevels = 24;
        private const int ThicknessClasses = 3;

        // Sizes are given for 1080 px height and scaled with the element
        private const double DesignHeight = 1080;
        private const double LogoSize = 190;
        private const double TitleSize = 54;
        private const double SubtitleSize = 19;

        // Trail of a streak, in progress of its flight
        private const double TrailLength = 0.07;

        private static readonly Color fallbackAccent = Color.FromRgb(0x34, 0x98, 0xE5);

        private struct Particle
        {
            public double StartAngle, StartRadius, AngularTravel, Delay, Tilt, PlaneRotation, Brightness;
            public int ThicknessClass;
        }

        private readonly Random random = new();
        private readonly Particle[] particles = new Particle[ParticleCount];

        // Pens per thickness class and brightness level; rebuilt when the accent or the scale changes
        private readonly Pen[,] headPens = new Pen[ThicknessClasses, BrushLevels];
        private readonly Pen[,] trailPens = new Pen[ThicknessClasses, BrushLevels];
        private Color? brushAccent;
        private double brushScale;

        private Brush backgroundBrush = Brushes.Black;
        private Brush coreBrush = Brushes.Transparent;
        private Brush flashBrush = Brushes.Transparent;
        private Brush logoGlowBrush = Brushes.Transparent;
        private Brush subtitleBrush = Brushes.White;
        private Color accentBright;

        private bool isRunning;
        private double time;
        private TimeSpan lastRenderingTime;

        /// <summary>Raised when the animation has faded out (also after Skip).</summary>
        public event EventHandler? Completed;

        public bool IsPlaying => isRunning;

        /// <summary>Logo in the middle; of a multi-size icon the largest frame is used.</summary>
        public static readonly DependencyProperty LogoProperty =
            DependencyProperty.Register(
                nameof(Logo),
                typeof(ImageSource),
                typeof(CFParticleIntro),
                new PropertyMetadata(null));

        public ImageSource? Logo
        {
            get => (ImageSource?)GetValue(LogoProperty);
            set => SetValue(LogoProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(CFParticleIntro),
                new PropertyMetadata(string.Empty));

        /// <summary>Large text below the logo.</summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(CFParticleIntro),
                new PropertyMetadata(string.Empty));

        /// <summary>Small text in the accent color below the title.</summary>
        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public CFParticleIntro()
        {
            Focusable = false;
            Visibility = Visibility.Collapsed;
        }

        public void Play()
        {
            ResetParticles();

            time = 0;
            lastRenderingTime = TimeSpan.Zero;
            Opacity = 1;
            Visibility = Visibility.Visible;

            if (isRunning)
                return;

            isRunning = true;
            CompositionTarget.Rendering += OnRendering;
        }

        /// <summary>Jumps to the fade out.</summary>
        public void Skip()
        {
            if (isRunning && time < FadeStart)
                time = FadeStart;
        }

        private void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;
            CompositionTarget.Rendering -= OnRendering;

            Visibility = Visibility.Collapsed;
            Completed?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Skip();
            e.Handled = true;
        }

        /// <summary>
        /// Every streak starts somewhere on a wide orbit, on one of many tilted planes, and spirals into the center;
        /// the delays make them arrive one after another.
        /// </summary>
        private void ResetParticles()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = new Particle
                {
                    StartAngle = random.NextDouble() * Math.PI * 2,
                    StartRadius = 0.9 + random.NextDouble() * 0.9,
                    AngularTravel = Math.PI * (1.6 + random.NextDouble() * 1.4),
                    Delay = random.NextDouble() * MaxParticleDelay,
                    Tilt = 0.25 + random.NextDouble() * 0.5,
                    PlaneRotation = (random.NextDouble() - 0.5) * Math.PI * 0.6,
                    Brightness = 0.35 + random.NextDouble() * 0.65,
                    ThicknessClass = random.Next(ThicknessClasses)
                };
            }
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (e is not RenderingEventArgs args || args.RenderingTime == lastRenderingTime)
                return;

            if (lastRenderingTime == TimeSpan.Zero)
            {
                lastRenderingTime = args.RenderingTime;
                return;
            }

            time += Math.Min(0.05, (args.RenderingTime - lastRenderingTime).TotalSeconds);
            lastRenderingTime = args.RenderingTime;

            double fade = Clamp01((time - FadeStart) / FadeDuration);
            Opacity = 1 - EaseInQuad(fade);

            if (fade >= 1)
            {
                Stop();
                return;
            }

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (!isRunning || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            double scale = ActualHeight / DesignHeight;
            EnsureBrushes(scale);

            double cx = ActualWidth / 2;
            double cy = ActualHeight / 2;
            double unit = Math.Min(ActualWidth, ActualHeight) / 2;

            dc.DrawRectangle(backgroundBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));

            DrawCore(dc, cx, cy, unit);
            DrawStreaks(dc, cx, cy, unit);
            DrawFlash(dc, cx, cy, unit, scale);
            DrawLogo(dc, cx, cy, scale);
            DrawTitle(dc, cx, cy, scale);
        }

        /// <summary>Energy gathering in the middle while the streaks fly in.</summary>
        private void DrawCore(DrawingContext dc, double cx, double cy, double unit)
        {
            double gather = Clamp01(time / ConvergeEnd);
            double radius = unit * (0.06 + 0.16 * EaseInQuad(gather));

            dc.PushOpacity(0.25 + 0.75 * gather);
            dc.DrawEllipse(coreBrush, null, new Point(cx, cy), radius, radius);
            dc.Pop();
        }

        private void DrawStreaks(DrawingContext dc, double cx, double cy, double unit)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                ref Particle p = ref particles[i];

                double progress = (time - p.Delay) / (ConvergeEnd - p.Delay);

                if (progress <= 0 || progress >= 1)
                    continue;

                Point head = GetPosition(p, progress, cx, cy, unit);
                Point trail = GetPosition(p, Math.Max(0, progress - TrailLength), cx, cy, unit);
                Point tail = GetPosition(p, Math.Max(0, progress - 2 * TrailLength), cx, cy, unit);

                // Fades in at the start and gets brighter towards the center
                double alpha = Clamp01(progress / 0.15) * (0.3 + 0.7 * progress) * p.Brightness;
                int level = (int)Math.Round(alpha * (BrushLevels - 1));

                if (level <= 0)
                    continue;

                dc.DrawLine(trailPens[p.ThicknessClass, level], tail, trail);
                dc.DrawLine(headPens[p.ThicknessClass, level], trail, head);
            }
        }

        /// <summary>
        /// Position on the spiral: the radius shrinks faster and faster, the angle turns on; the orbit lies on a tilted
        /// plane (flattened circle, rotated), which gives the rings around a sphere.
        /// </summary>
        private static Point GetPosition(in Particle p, double progress, double cx, double cy, double unit)
        {
            double radius = p.StartRadius * (1 - EaseInCubic(progress));
            double angle = p.StartAngle + p.AngularTravel * progress;

            double x = Math.Cos(angle) * radius;
            double y = Math.Sin(angle) * radius * p.Tilt;

            double cos = Math.Cos(p.PlaneRotation);
            double sin = Math.Sin(p.PlaneRotation);

            return new Point(cx + (x * cos - y * sin) * unit, cy + (x * sin + y * cos) * unit);
        }

        /// <summary>Flash and shock wave when the streaks have reached the middle.</summary>
        private void DrawFlash(DrawingContext dc, double cx, double cy, double unit, double scale)
        {
            double flash = (time - ConvergeEnd) / FlashDuration;

            if (flash >= 0 && flash < 1)
            {
                double radius = unit * (0.15 + 1.1 * EaseOutCubic(flash));

                dc.PushOpacity(Math.Pow(1 - flash, 2) * 0.9);
                dc.DrawEllipse(flashBrush, null, new Point(cx, cy), radius, radius);
                dc.Pop();
            }

            double wave = (time - ConvergeEnd) / ShockwaveDuration;

            if (wave >= 0 && wave < 1)
            {
                double radius = unit * (0.2 + 1.3 * EaseOutCubic(wave));
                Pen pen = new Pen(new SolidColorBrush(accentBright), Math.Max(0.5, 4 * scale * (1 - wave)));
                pen.Freeze();

                dc.PushOpacity((1 - wave) * 0.7);
                dc.DrawEllipse(null, pen, new Point(cx, cy), radius, radius * 0.92);
                dc.Pop();
            }
        }

        /// <summary>The logo grows out of the flash with a slight overshoot, on a soft accent glow.</summary>
        private void DrawLogo(DrawingContext dc, double cx, double cy, double scale)
        {
            double progress = Clamp01((time - ConvergeEnd + 0.05) / LogoInDuration);

            if (progress <= 0)
                return;

            double size = LogoSize * scale;
            double grow = EaseOutBack(progress);
            double opacity = Clamp01(progress * 2);

            // Glow breathes gently while the logo is shown
            double glowRadius = size * (0.85 + 0.06 * Math.Sin((time - ConvergeEnd) * 4));

            dc.PushOpacity(opacity);
            dc.DrawEllipse(logoGlowBrush, null, new Point(cx, cy), glowRadius, glowRadius);

            ImageSource? logo = GetLargestFrame(Logo);

            if (logo != null)
            {
                dc.PushTransform(new ScaleTransform(grow, grow, cx, cy));
                dc.DrawImage(logo, new Rect(cx - size / 2, cy - size / 2, size, size));
                dc.Pop();
            }

            dc.Pop();
        }

        private void DrawTitle(DrawingContext dc, double cx, double cy, double scale)
        {
            double progress = Clamp01((time - TextStart) / TextDuration);

            if (progress <= 0)
                return;

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double offset = (1 - EaseOutCubic(progress)) * 18 * scale;
            double top = cy + LogoSize * scale * 0.62 + 18 * scale + offset;

            dc.PushOpacity(progress);

            FormattedText title = new FormattedText(
                Title,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                TitleSize * scale,
                Brushes.White,
                pixelsPerDip);

            dc.DrawText(title, new Point(cx - title.Width / 2, top));

            // Letters spread apart like a logo type
            string spaced = string.Join(" ", Subtitle.ToCharArray());

            FormattedText subtitle = new FormattedText(
                spaced,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                SubtitleSize * scale,
                subtitleBrush,
                pixelsPerDip);

            dc.DrawText(subtitle, new Point(cx - subtitle.Width / 2, top + title.Height + 4 * scale));

            dc.Pop();
        }

        /// <summary>
        /// Of a multi-size icon (BitmapFrame of an .ico) the largest frame, so the logo is sharp at its size.
        /// </summary>
        private static ImageSource? GetLargestFrame(ImageSource? source)
        {
            if (source is BitmapFrame frame && frame.Decoder != null && frame.Decoder.Frames.Count > 1)
                return frame.Decoder.Frames.OrderByDescending(f => f.PixelWidth).First();

            return source;
        }

        /// <summary>
        /// Brushes and pens in the current accent color: dim streaks in the accent, bright ones lightened towards white.
        /// </summary>
        private void EnsureBrushes(double scale)
        {
            var accent = System.Windows.Application.Current?.TryFindResource("Color.Accent") is Color c ? c : fallbackAccent;
            accent.A = 255;

            if (brushAccent == accent && Math.Abs(brushScale - scale) < 0.001)
                return;

            brushAccent = accent;
            brushScale = scale;
            accentBright = Lerp(accent, Colors.White, 0.45);

            for (int thickness = 0; thickness < ThicknessClasses; thickness++)
            {
                double width = (1.4 + thickness * 0.9) * scale;

                for (int level = 0; level < BrushLevels; level++)
                {
                    double t = level / (double)(BrushLevels - 1);
                    Color color = Lerp(accent, Colors.White, 0.2 + 0.6 * t);

                    color.A = (byte)(255 * t);
                    headPens[thickness, level] = CreatePen(color, width);

                    color.A = (byte)(110 * t);
                    trailPens[thickness, level] = CreatePen(color, width * 0.7);
                }
            }

            backgroundBrush = Freeze(new RadialGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Lerp(Color.FromRgb(0x06, 0x08, 0x0C), accent, 0.12), 0),
                    new GradientStop(Color.FromRgb(0x06, 0x08, 0x0C), 0.6),
                    new GradientStop(Color.FromRgb(0x02, 0x03, 0x05), 1)
                }));

            coreBrush = Freeze(new RadialGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF), 0),
                    new GradientStop(WithAlpha(accentBright, 0x80), 0.35),
                    new GradientStop(WithAlpha(accent, 0x00), 1)
                }));

            flashBrush = Freeze(new RadialGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), 0),
                    new GradientStop(WithAlpha(accentBright, 0xA0), 0.3),
                    new GradientStop(WithAlpha(accent, 0x00), 1)
                }));

            logoGlowBrush = Freeze(new RadialGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(WithAlpha(accent, 0x66), 0),
                    new GradientStop(WithAlpha(accent, 0x00), 1)
                }));

            subtitleBrush = Freeze(new SolidColorBrush(accentBright));
        }

        private static Pen CreatePen(Color color, double width)
        {
            Pen pen = new Pen(new SolidColorBrush(color), width)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            pen.Freeze();
            return pen;
        }

        private static Brush Freeze(Brush brush)
        {
            brush.Freeze();
            return brush;
        }

        private static Color WithAlpha(Color color, byte alpha)
        {
            color.A = alpha;
            return color;
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        private static double Clamp01(double value) => Math.Clamp(value, 0, 1);

        private static double EaseInQuad(double t) => t * t;

        private static double EaseInCubic(double t) => t * t * t;

        private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

        private static double EaseOutBack(double t)
        {
            const double c1 = 1.70158;
            const double c3 = c1 + 1;

            return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
        }
    }
}
