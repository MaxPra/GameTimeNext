using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Particle clock for the blackout mode.
    /// The digits are made of slightly "dancing" particles. When the minute changes,
    /// the particles of the changed digits scatter and reassemble into the new digit.
    /// The clock slowly wanders across the screen to prevent burn-in.
    /// </summary>
    internal sealed class CFBlackoutParticleClock : FrameworkElement
    {
        private const string TimeFormat = "HH:mm";
        private const double GlyphEmSize = 200d;
        private const int DigitParticleCount = 320;
        private const int ColonParticleCount = 60;
        private const int DustParticleCount = 70;
        private const int BrushLevels = 32;
        private const double WanderSpeed = 25d;
        private const double ClockOpacity = 0.75;
        private const double SpringStrength = 26d;
        private const double Damping = 7.5d;

        private struct Particle
        {
            public double X, Y, VX, VY, TX, TY, Phase, Speed;
            public int Slot;
        }

        private struct Dust
        {
            public double X, Y, VX, VY, Phase;
        }

        private static readonly Typeface clockTypeface = new(
            new FontFamily("Bahnschrift, Segoe UI"), FontStyles.Normal, FontWeights.Light, FontStretches.Normal);

        private static readonly Color fallbackAccent = Color.FromRgb(0x34, 0x98, 0xE5);

        private readonly SolidColorBrush[] dotBrushes = new SolidColorBrush[BrushLevels];
        private readonly RadialGradientBrush[] glowBrushes = new RadialGradientBrush[BrushLevels];
        private SolidColorBrush dateBrush = Brushes.Transparent;
        private Color? brushAccent;

        private readonly Random random = new();
        private readonly Particle[] particles;
        private readonly int[] slotStart = new int[TimeFormat.Length];
        private readonly int[] slotCount = new int[TimeFormat.Length];
        private readonly Dust[] dust = new Dust[DustParticleCount];

        private bool isRunning;
        private TimeSpan lastRenderingTime = TimeSpan.Zero;
        private double time;
        private string currentText = string.Empty;
        private double clockHalfWidth = 1;
        private double clockHalfHeight = 1;
        private double centerX = double.NaN;
        private double centerY = double.NaN;
        private double velocityX;
        private double velocityY;

        private FormattedText? dateText;
        private string dateTextKey = string.Empty;

        /// <summary>
        /// Rebuilds the brushes when the accent color has changed
        /// (e.g. a profile switch with its own accent color while the blackout is active).
        /// </summary>
        private void EnsureBrushes()
        {
            var accent = System.Windows.Application.Current?.TryFindResource("Color.Accent") is Color c ? c : fallbackAccent;
            accent.A = 255;

            if (brushAccent == accent)
            {
                return;
            }

            brushAccent = accent;

            // Dim particles use a darkened accent color, bright ones are lightened towards white
            var dim = Lerp(Colors.Black, accent, 0.65);
            var bright = Lerp(accent, Colors.White, 0.6);

            for (int i = 0; i < BrushLevels; i++)
            {
                double t = i / (double)(BrushLevels - 1);
                var color = Lerp(dim, bright, t);

                color.A = (byte)(30 + t * 225);
                var dot = new SolidColorBrush(color);
                dot.Freeze();
                dotBrushes[i] = dot;

                var glowColor = color;
                glowColor.A = (byte)(t * 60);
                var transparent = glowColor;
                transparent.A = 0;
                var glow = new RadialGradientBrush(glowColor, transparent);
                glow.Freeze();
                glowBrushes[i] = glow;
            }

            var dateColor = Lerp(accent, Colors.White, 0.25);
            dateColor.A = 150;
            dateBrush = new SolidColorBrush(dateColor);
            dateBrush.Freeze();
            dateText = null;
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        public CFBlackoutParticleClock()
        {
            IsHitTestVisible = false;
            Focusable = false;
            Opacity = ClockOpacity;

            // Assign particles to fixed character slots (H H : m m)
            int total = 0;
            for (int slot = 0; slot < TimeFormat.Length; slot++)
            {
                slotStart[slot] = total;
                slotCount[slot] = TimeFormat[slot] == ':' ? ColonParticleCount : DigitParticleCount;
                total += slotCount[slot];
            }

            particles = new Particle[total];
            for (int slot = 0; slot < TimeFormat.Length; slot++)
            {
                for (int i = slotStart[slot]; i < slotStart[slot] + slotCount[slot]; i++)
                {
                    // Start scattered so the digits fly together when the clock appears
                    double angle = random.NextDouble() * Math.PI * 2;
                    double distance = 250 + random.NextDouble() * 600;

                    particles[i] = new Particle
                    {
                        Slot = slot,
                        X = Math.Cos(angle) * distance,
                        Y = Math.Sin(angle) * distance * 0.6,
                        Phase = random.NextDouble() * Math.PI * 2,
                        Speed = 0.8 + random.NextDouble() * 2.2
                    };
                }
            }

            for (int i = 0; i < dust.Length; i++)
            {
                dust[i] = new Dust
                {
                    X = (random.NextDouble() * 2 - 1) * 400,
                    Y = (random.NextDouble() * 2 - 1) * 200,
                    VX = (random.NextDouble() - 0.5) * 12,
                    VY = (random.NextDouble() - 0.5) * 8,
                    Phase = random.NextDouble() * Math.PI * 2
                };
            }

            velocityX = random.NextDouble() > 0.5 ? WanderSpeed : -WanderSpeed;
            velocityY = random.NextDouble() > 0.5 ? WanderSpeed * 0.75 : -WanderSpeed * 0.75;
        }

        public void Start()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            lastRenderingTime = TimeSpan.Zero;
            CompositionTarget.Rendering += OnRendering;
        }

        public void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (e is not RenderingEventArgs args || args.RenderingTime == lastRenderingTime)
            {
                return;
            }

            if (lastRenderingTime == TimeSpan.Zero)
            {
                lastRenderingTime = args.RenderingTime;
                return;
            }

            double dt = Math.Min(0.1, (args.RenderingTime - lastRenderingTime).TotalSeconds);
            lastRenderingTime = args.RenderingTime;

            Update(dt);
            InvalidateVisual();
        }

        private double ClockScale => Math.Min(
            ActualWidth * 0.40 / (clockHalfWidth * 2),
            ActualHeight * 0.28 / (clockHalfHeight * 2));

        private void Update(double dt)
        {
            time += dt;

            string text = DateTime.Now.ToString(TimeFormat, CultureInfo.InvariantCulture);
            if (text != currentText)
            {
                ApplyText(text);
            }

            // Spring physics in sub-steps to stay stable on frame drops
            int steps = (int)Math.Ceiling(dt / 0.016);
            double step = dt / steps;
            for (int s = 0; s < steps; s++)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    ref var p = ref particles[i];
                    p.VX += ((p.TX - p.X) * SpringStrength - p.VX * Damping) * step;
                    p.VY += ((p.TY - p.Y) * SpringStrength - p.VY * Damping) * step;
                    p.X += p.VX * step;
                    p.Y += p.VY * step;
                }
            }

            double dustRangeX = clockHalfWidth * 1.5;
            double dustRangeY = clockHalfHeight * 2.6;
            for (int i = 0; i < dust.Length; i++)
            {
                ref var d = ref dust[i];
                d.X += d.VX * dt;
                d.Y += d.VY * dt;

                if (d.X < -dustRangeX) d.X += dustRangeX * 2;
                if (d.X > dustRangeX) d.X -= dustRangeX * 2;
                if (d.Y < -dustRangeY) d.Y += dustRangeY * 2;
                if (d.Y > dustRangeY) d.Y -= dustRangeY * 2;
            }

            // Wander across the screen (burn-in protection)
            double scale = ClockScale;
            double extentX = clockHalfWidth * scale * 1.15;
            double extentY = clockHalfHeight * scale * 2.2;
            double minX = extentX, maxX = Math.Max(extentX, ActualWidth - extentX);
            double minY = extentY, maxY = Math.Max(extentY, ActualHeight - extentY);

            if (double.IsNaN(centerX))
            {
                centerX = ActualWidth * 0.5;
                centerY = ActualHeight * 0.5;
            }

            centerX += velocityX * dt;
            centerY += velocityY * dt;

            if (centerX <= minX || centerX >= maxX)
            {
                velocityX = -velocityX;
                centerX = Math.Clamp(centerX, minX, maxX);
            }

            if (centerY <= minY || centerY >= maxY)
            {
                velocityY = -velocityY;
                centerY = Math.Clamp(centerY, minY, maxY);
            }
        }

        private void ApplyText(string text)
        {
            var pools = SampleGlyphs(text);
            bool firstText = currentText.Length == 0;

            for (int slot = 0; slot < text.Length; slot++)
            {
                if (!firstText && currentText[slot] == text[slot])
                {
                    continue;
                }

                var pool = pools[slot];
                if (pool.Count == 0)
                {
                    continue;
                }

                for (int i = slotStart[slot]; i < slotStart[slot] + slotCount[slot]; i++)
                {
                    ref var p = ref particles[i];
                    var target = pool[random.Next(pool.Count)];
                    p.TX = target.X;
                    p.TY = target.Y;

                    if (!firstText)
                    {
                        // Scatter before the new digit forms
                        double angle = random.NextDouble() * Math.PI * 2;
                        double speed = 150 + random.NextDouble() * 350;
                        p.VX += Math.Cos(angle) * speed;
                        p.VY += Math.Sin(angle) * speed;
                    }
                }
            }

            currentText = text;
        }

        /// <summary>
        /// Renders the text into a bitmap and returns, per character, the pixel positions
        /// the particles fly to (relative to the center of the clock).
        /// </summary>
        private List<Point>[] SampleGlyphs(string text)
        {
            const int pad = 4;
            const int sampleStep = 2;

            var formatted = CreateText(text, GlyphEmSize, Brushes.White, 1.0);
            int width = (int)Math.Ceiling(formatted.WidthIncludingTrailingWhitespace) + pad * 2;
            int height = (int)Math.Ceiling(formatted.Height) + pad * 2;

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawText(formatted, new Point(pad, pad));
            }

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);

            var bounds = new double[text.Length + 1];
            for (int i = 1; i < text.Length; i++)
            {
                bounds[i] = CreateText(text[..i], GlyphEmSize, Brushes.White, 1.0).WidthIncludingTrailingWhitespace;
            }
            bounds[text.Length] = double.MaxValue;

            double capsHalfHeight = clockTypeface.CapsHeight * GlyphEmSize * 0.5;
            double originX = pad + formatted.WidthIncludingTrailingWhitespace * 0.5;
            double originY = pad + formatted.Baseline - capsHalfHeight;

            clockHalfWidth = formatted.WidthIncludingTrailingWhitespace * 0.5;
            clockHalfHeight = capsHalfHeight;

            var pools = new List<Point>[text.Length];
            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = new List<Point>();
            }

            for (int y = 0; y < height; y += sampleStep)
            {
                for (int x = 0; x < width; x += sampleStep)
                {
                    if (pixels[(y * width + x) * 4 + 3] < 140)
                    {
                        continue;
                    }

                    double localX = x - pad;
                    int slot = 0;
                    while (slot < text.Length - 1 && localX >= bounds[slot + 1])
                    {
                        slot++;
                    }

                    pools[slot].Add(new Point(
                        x - originX + (random.NextDouble() - 0.5) * sampleStep,
                        y - originY + (random.NextDouble() - 0.5) * sampleStep));
                }
            }

            return pools;
        }

        private static FormattedText CreateText(string text, double emSize, Brush brush, double pixelsPerDip)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                clockTypeface, emSize, brush, pixelsPerDip);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (ActualWidth <= 0 || ActualHeight <= 0 || double.IsNaN(centerX))
            {
                return;
            }

            EnsureBrushes();

            var now = DateTime.Now;
            double scale = ClockScale;
            double cx = centerX;
            double cy = centerY;
            double secondPulse = Math.Exp(-now.Millisecond / 350d);

            // Time
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];

                double ox = Math.Sin(time * p.Speed + p.Phase) * 1.1 + Math.Sin(time * 1.3 + p.Y * 0.05) * 0.6;
                double oy = Math.Cos(time * p.Speed * 0.8 + p.Phase * 1.7) * 1.1 + Math.Sin(time * 1.7 + p.X * 0.03) * 0.8;

                double motion = Math.Min(1, Math.Sqrt(p.VX * p.VX + p.VY * p.VY) / 250);
                double shimmer = 0.5 + 0.5 * Math.Sin(p.X * 0.025 - time * 2.2);
                double brightness = 0.45 + 0.2 * shimmer + 0.15 * Math.Sin(time * p.Speed * 1.5 + p.Phase) + motion * 0.6;
                brightness = Math.Clamp(brightness, 0, 1);
                int level = (int)(brightness * (BrushLevels - 1));
                double radius = (0.6 + brightness * 0.5) * scale * 0.85;
                var point = new Point(cx + (p.X + ox) * scale, cy + (p.Y + oy) * scale);

                if (brightness > 0.85)
                {
                    dc.DrawEllipse(glowBrushes[level], null, point, radius * 3.5, radius * 3.5);
                }

                dc.DrawEllipse(dotBrushes[level], null, point, radius, radius);
            }

            // Seconds bar
            double rowY = cy + (clockHalfHeight + 30) * scale;
            double rowLeft = cx - clockHalfWidth * 0.92 * scale;
            double rowStep = clockHalfWidth * 1.84 * scale / 59;
            double secondFraction = now.Millisecond / 1000d;

            for (int i = 0; i < 60; i++)
            {
                double brightness;
                double radius = scale * 0.9;

                if (i < now.Second)
                {
                    brightness = 0.55;
                }
                else if (i == now.Second)
                {
                    brightness = 0.6 + 0.4 * secondPulse;
                    radius *= 1.3 + 0.5 * secondPulse;
                }
                else
                {
                    brightness = 0.08 + 0.04 * Math.Sin(time * 2 + i * 0.4) + (i == now.Second + 1 ? secondFraction * 0.15 : 0);
                }

                int level = (int)(Math.Clamp(brightness, 0, 1) * (BrushLevels - 1));
                dc.DrawEllipse(dotBrushes[level], null, new Point(rowLeft + i * rowStep, rowY), radius, radius);
            }

            // Date
            double dateEmSize = Math.Round(26 * scale);
            string dateString = now.ToString("dddd, " + CultureInfo.CurrentCulture.DateTimeFormat.MonthDayPattern, CultureInfo.CurrentCulture);
            string key = dateString + "|" + dateEmSize;
            if (dateText == null || key != dateTextKey)
            {
                dateText = CreateText(dateString, dateEmSize, dateBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dateTextKey = key;
            }

            dc.DrawText(dateText, new Point(cx - dateText.Width * 0.5, rowY + 18 * scale));

            // Floating dust around the clock
            for (int i = 0; i < dust.Length; i++)
            {
                ref var d = ref dust[i];
                double brightness = 0.08 + 0.12 * (0.5 + 0.5 * Math.Sin(time * 0.9 + d.Phase));
                int level = (int)(brightness * (BrushLevels - 1));
                dc.DrawEllipse(dotBrushes[level], null, new Point(cx + d.X * scale, cy + d.Y * scale), scale * 0.6, scale * 0.6);
            }
        }
    }
}
