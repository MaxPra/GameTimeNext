using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Particle effect of the splash wordmark: glowing dust flies in on curved paths and settles on the strokes of
    /// <see cref="Targets"/> just before the solid wordmark is revealed from left to right, then flares up and drifts
    /// off as sparks. The positions are closed-form functions of the elapsed time (no integration), so the effect looks
    /// the same at any frame rate. Drawn by the element itself; the splash runs on its own UI thread, so it keeps
    /// moving while the application starts. Coordinates are those of the wordmark geometry (baseline at y = 0, which is
    /// the vertical middle of the element).
    /// </summary>
    public sealed class SplashWordmarkParticles : FrameworkElement
    {
        private const int ParticleCount = 620;
        private const int ParticleCountRemote = 200;
        private const int AlphaLevels = 24;

        // Reveal mask of the solid wordmark: soft edge of this relative width (gradient stops in SplashScreen.xaml)
        private const double RevealSpan = 1.12;

        public static readonly DependencyProperty TargetsProperty =
            DependencyProperty.Register(nameof(Targets), typeof(Geometry), typeof(SplashWordmarkParticles));

        public static readonly DependencyProperty WriteBeginProperty =
            DependencyProperty.Register(nameof(WriteBegin), typeof(TimeSpan), typeof(SplashWordmarkParticles),
                new PropertyMetadata(TimeSpan.FromSeconds(0.55)));

        public static readonly DependencyProperty WriteDurationProperty =
            DependencyProperty.Register(nameof(WriteDuration), typeof(Duration), typeof(SplashWordmarkParticles),
                new PropertyMetadata(new Duration(TimeSpan.FromSeconds(1.5))));

        /// <summary>Strokes the particles settle on.</summary>
        public Geometry? Targets
        {
            get => (Geometry?)GetValue(TargetsProperty);
            set => SetValue(TargetsProperty, value);
        }

        /// <summary>Start of the reveal of the solid wordmark; must be the one of the reveal animation.</summary>
        public TimeSpan WriteBegin
        {
            get => (TimeSpan)GetValue(WriteBeginProperty);
            set => SetValue(WriteBeginProperty, value);
        }

        /// <summary>Duration of the reveal of the solid wordmark; must be the one of the reveal animation.</summary>
        public Duration WriteDuration
        {
            get => (Duration)GetValue(WriteDurationProperty);
            set => SetValue(WriteDurationProperty, value);
        }

        private struct Particle
        {
            public double TargetX, TargetY, StartX, StartY, ControlX, ControlY;
            public double Arrive, Flight, Life;
            public double DriftX, DriftY;
            public double Radius;
            public int Kind;
        }

        private readonly Brush[][] brushes;
        private readonly Stopwatch clock = new Stopwatch();
        private Particle[] particles = [];
        private double endTime;
        private bool isRunning;
        private int frames;
        private double lastFrame, maxGap, maxGapAt;

        public SplashWordmarkParticles()
        {
            // Two kinds: accent blue dust and white-hot sparkles; every kind in several opacities (frozen, shared)
            brushes =
            [
                CreateBrushes(Color.FromRgb(0x8E, 0xD1, 0xFA), Color.FromRgb(0x34, 0x98, 0xE5)),
                CreateBrushes(Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0xBF, 0xE4, 0xFF))
            ];

            Loaded += (_, _) => Start();
            Unloaded += (_, _) => Stop();
        }

        private static Brush[] CreateBrushes(Color core, Color glow)
        {
            Brush[] result = new Brush[AlphaLevels];
            for (int i = 0; i < AlphaLevels; i++)
            {
                RadialGradientBrush brush = new RadialGradientBrush { Opacity = (i + 1) / (double)AlphaLevels };
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, core.R, core.G, core.B), 0));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xB0, glow.R, glow.G, glow.B), 0.4));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, glow.R, glow.G, glow.B), 1));
                brush.Freeze();
                result[i] = brush;
            }

            return result;
        }

        private void Start()
        {
            if (isRunning || Targets is null || ActualWidth <= 0)
                return;

            BuildParticles();
            clock.Restart();
            isRunning = true;
            CompositionTarget.Rendering += OnRendering;
        }

        private void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;
            CompositionTarget.Rendering -= OnRendering;
            clock.Stop();

            FnLog.AddInfo("MainApp",
                $"Splash animation: {frames} frames in {lastFrame:F2} s, longest gap {maxGap * 1000:F0} ms at {maxGapAt:F2} s" +
                (SystemParameters.IsRemoteSession ? " (remote session)" : ""));
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            // Frame statistics for the log, to see where the animation stutters
            double now = clock.Elapsed.TotalSeconds;
            if (frames > 0 && now - lastFrame > maxGap)
            {
                maxGap = now - lastFrame;
                maxGapAt = now;
            }

            lastFrame = now;
            frames++;

            InvalidateVisual();

            // One last frame after the last particle is gone clears the element
            if (clock.Elapsed.TotalSeconds > endTime)
                Stop();
        }

        private void BuildParticles()
        {
            PathGeometry path = PathGeometry.CreateFromGeometry(Targets!);
            double width = ActualWidth;
            double begin = WriteBegin.TotalSeconds;
            double write = WriteDuration.HasTimeSpan ? WriteDuration.TimeSpan.TotalSeconds : 1.5;
            Random random = new Random(11);

            // Remote desktop renders in software: fewer particles keep the animation smooth there
            int count = SystemParameters.IsRemoteSession ? ParticleCountRemote : ParticleCount;
            particles = new Particle[count];
            endTime = 0;

            for (int i = 0; i < count; i++)
            {
                path.GetPointAtFractionLength(random.NextDouble(), out Point target, out _);

                Particle p = new Particle { TargetX = target.X, TargetY = target.Y };

                // The reveal reaches this point in the middle of its soft edge; the dust lands just before
                p.Arrive = begin + write * (target.X / width + (RevealSpan - 1) / 2) / RevealSpan - 0.04 + random.NextDouble() * 0.12;
                p.Flight = 0.45 + random.NextDouble() * 0.4;

                // Dust comes out of a cloud on the left of its target (like blown in by the writing), spread vertically
                double angle = random.NextDouble() * Math.PI * 2;
                double distance = 90 + Math.Pow(random.NextDouble(), 1.6) * 420;
                p.StartX = target.X - 90 + Math.Cos(angle) * distance;
                p.StartY = target.Y + Math.Sin(angle) * distance * 0.65;

                // Curved flight: control point of a quadratic curve pushed sideways off the straight line
                double bend = (random.NextDouble() - 0.5) * 220;
                double dx = target.X - p.StartX;
                double dy = target.Y - p.StartY;
                double length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                p.ControlX = (p.StartX + target.X) / 2 - dy / length * bend;
                p.ControlY = (p.StartY + target.Y) / 2 + dx / length * bend;

                // After landing: most flare up and fade in place, some fly off as sparks
                bool spark = random.NextDouble() < 0.4;
                p.Life = spark ? 0.55 + random.NextDouble() * 0.6 : 0.3 + random.NextDouble() * 0.3;
                double driftAngle = -Math.PI / 2 + (random.NextDouble() - 0.5) * 2.2;
                double driftLength = spark ? 40 + random.NextDouble() * 110 : 4 + random.NextDouble() * 10;
                p.DriftX = Math.Cos(driftAngle) * driftLength;
                p.DriftY = Math.Sin(driftAngle) * driftLength;

                p.Radius = 6 + random.NextDouble() * 8;
                p.Kind = random.NextDouble() < 0.28 ? 1 : 0;

                particles[i] = p;
                endTime = Math.Max(endTime, p.Arrive + p.Life);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (particles.Length == 0)
                return;

            double t = clock.Elapsed.TotalSeconds;
            if (t > endTime)
                return;

            drawingContext.PushTransform(new TranslateTransform(0, ActualHeight / 2));

            foreach (Particle p in particles)
            {
                double start = p.Arrive - p.Flight;
                if (t < start)
                    continue;

                double x, y, alpha, scale;
                if (t < p.Arrive)
                {
                    // Flight: fast at first, settling softly; dim and small while far away
                    double u = (t - start) / p.Flight;
                    double e = 1 - Math.Pow(1 - u, 3);
                    double w = 1 - e;
                    x = w * w * p.StartX + 2 * w * e * p.ControlX + e * e * p.TargetX;
                    y = w * w * p.StartY + 2 * w * e * p.ControlY + e * e * p.TargetY;
                    alpha = Math.Min(1, u * 3) * (0.3 + 0.7 * u);
                    scale = 0.5 + 0.7 * u;
                }
                else
                {
                    // Landed: flares up, then fades while drifting
                    double u = (t - p.Arrive) / p.Life;
                    if (u >= 1)
                        continue;

                    double drift = 1 - Math.Pow(1 - u, 2);
                    x = p.TargetX + p.DriftX * drift;
                    y = p.TargetY + p.DriftY * drift;
                    alpha = Math.Pow(1 - u, 1.5);
                    scale = 1.2 + 0.5 * Math.Sin(Math.Min(1, u * 4) * Math.PI / 2) - 0.7 * u;
                }

                int level = (int)(alpha * (AlphaLevels - 1) + 0.5);
                if (level <= 0)
                    continue;

                double radius = p.Radius * scale;
                drawingContext.DrawEllipse(brushes[p.Kind][level], null, new Point(x, y), radius, radius);
            }

            drawingContext.Pop();
        }
    }
}
