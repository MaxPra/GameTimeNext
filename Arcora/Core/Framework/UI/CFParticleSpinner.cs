using System.Windows;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>Outline the particles of a <see cref="CFParticleSpinner"/> form.</summary>
    public enum CFParticleSpinnerShape
    {
        /// <summary>A ring (busy indicator).</summary>
        Ring,

        /// <summary>A large rounded play triangle instead of the ring; room for an icon in its middle (game time tracking).</summary>
        PlayButton,

        /// <summary>
        /// Two counter-rotating rings while warp streaks shoot outwards through them; room for an icon in the middle
        /// (game launch).
        /// </summary>
        Launch
    }

    /// <summary>
    /// Busy indicator made of particles (same look as the particle clock of the blackout mode).
    /// The particles form slightly "dancing" outlines (see Shape); a bright comet head runs along each outline and
    /// pushes the particles it passes outwards, leaving a fading tail. Some dust floats around.
    /// Runs only while visible; the shape is complete from the first frame (no fly-in, short loads would look odd).
    /// </summary>
    public sealed class CFParticleSpinner : FrameworkElement
    {
        private const int RingParticleCount = 110;
        private const int TriangleParticleCount = 130;
        private const int DustParticleCount = 26;
        private const int LaunchOuterParticleCount = 100;
        private const int LaunchInnerParticleCount = 64;
        private const int WarpStreakCount = 64;
        private const double LaunchInnerRingRadius = 0.7;

        /// <summary>Warp streaks are born in this distance around the center and vanish at WarpStreakEnd.</summary>
        private const double WarpStreakStart = 0.25;
        private const double WarpStreakEnd = 1.65;
        private const int BrushLevels = 32;
        private const int TrackSamples = 720;

        /// <summary>
        /// Size of one shape unit (ring radius) relative to half of the element size; the rest is room for the bulge,
        /// glow and dust. The play triangle reaches 1.32 units to the right, so it uses a smaller unit.
        /// </summary>
        private const double RingRadiusFactor = 0.62;
        private const double PlayButtonUnitFactor = 0.56;
        private const double LaunchUnitFactor = 0.52;
        private const double BaseHeadSpeed = 3.4;
        private const double TailLength = 2.2;
        private const double SpringStrength = 30d;
        private const double Damping = 8d;

        /// <summary>
        /// Play triangle (in ring radius units) before rounding the corners: equilateral, pointing right, the incircle
        /// (radius 0.6, plus PlayCornerRadius after rounding) centered on the element, so an icon fits in the middle.
        /// </summary>
        private static readonly Point[] PlayTriangle = { new(-0.6, -1.039), new(1.2, 0), new(-0.6, 1.039) };
        private const double PlayCornerRadius = 0.12;

        /// <summary>Closed outline sampled at equal arc length; normals point outwards.</summary>
        private sealed class Track
        {
            public required Point[] Points { get; init; }
            public required Vector[] Normals { get; init; }

            /// <summary>How far the comet head pushes particles outwards and how much they scatter around the outline.</summary>
            public required double Bulge { get; init; }
            public required double Spread { get; init; }

            /// <summary>Start position of the comet head on this track (0..1).</summary>
            public required double HeadOffset { get; init; }
        }

        private struct Particle
        {
            public double X, Y, VX, VY, U, Offset, Phase, Speed;
            public int Track;
        }

        private struct Dust
        {
            public double X, Y, VX, VY, Phase;
        }

        private static readonly Color fallbackAccent = Color.FromRgb(0x34, 0x98, 0xE5);

        private readonly SolidColorBrush[] dotBrushes = new SolidColorBrush[BrushLevels];
        private readonly RadialGradientBrush[] glowBrushes = new RadialGradientBrush[BrushLevels];
        private readonly Pen[] streakPens = new Pen[BrushLevels];
        private Color? brushAccent;

        private readonly Random random = new();
        private Track[] tracks = Array.Empty<Track>();
        private Particle[] particles = Array.Empty<Particle>();
        private Dust[] dust = new Dust[DustParticleCount];

        /// <summary>The dust flies outwards as warp streaks instead of floating around (Launch).</summary>
        private bool IsWarp => Shape == CFParticleSpinnerShape.Launch;

        /// <summary>Speed factor of the comet head and the breathing (1 = busy indicator; lower values feel calmer).</summary>
        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register(
                nameof(Speed),
                typeof(double),
                typeof(CFParticleSpinner),
                new PropertyMetadata(1d));

        public double Speed
        {
            get => (double)GetValue(SpeedProperty);
            set => SetValue(SpeedProperty, value);
        }

        /// <summary>Outline the particles form.</summary>
        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.Register(
                nameof(Shape),
                typeof(CFParticleSpinnerShape),
                typeof(CFParticleSpinner),
                new PropertyMetadata(CFParticleSpinnerShape.Ring, (d, _) => ((CFParticleSpinner)d).BuildShape()));

        public CFParticleSpinnerShape Shape
        {
            get => (CFParticleSpinnerShape)GetValue(ShapeProperty);
            set => SetValue(ShapeProperty, value);
        }

        /// <summary>The warp streaks (Shape Launch) fly inwards and slow down towards the center (game quits).</summary>
        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(
                nameof(Reverse),
                typeof(bool),
                typeof(CFParticleSpinner),
                new PropertyMetadata(false));

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }

        private bool isRunning;
        private TimeSpan lastRenderingTime = TimeSpan.Zero;
        private double time;

        public CFParticleSpinner()
        {
            IsHitTestVisible = false;
            Focusable = false;

            BuildShape();

            IsVisibleChanged += (_, _) =>
            {
                if (IsVisible)
                    Start();
                else
                    Stop();
            };
        }

        private void Start()
        {
            if (isRunning)
                return;

            ResetParticles();
            isRunning = true;
            lastRenderingTime = TimeSpan.Zero;
            CompositionTarget.Rendering += OnRendering;
        }

        private void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        /// <summary>Builds the tracks of the current shape and distributes the particles along them.</summary>
        private void BuildShape()
        {
            int[] particleCounts;

            switch (Shape)
            {
                case CFParticleSpinnerShape.PlayButton:
                    tracks = new[] { CreateRoundedPolygonTrack(PlayTriangle, PlayCornerRadius, bulge: 0.16, spread: 0.12, headOffset: 0) };
                    particleCounts = new[] { TriangleParticleCount };
                    break;

                case CFParticleSpinnerShape.Launch:
                    // The inner ring runs the other way round, its comet head starts opposite of the outer one
                    tracks = new[]
                    {
                        CreateRingTrack(1, clockwise: true, bulge: 0.2, spread: 0.12, headOffset: 0),
                        CreateRingTrack(LaunchInnerRingRadius, clockwise: false, bulge: 0.14, spread: 0.08, headOffset: 0.5)
                    };
                    particleCounts = new[] { LaunchOuterParticleCount, LaunchInnerParticleCount };
                    break;

                default:
                    tracks = new[] { CreateRingTrack(1, clockwise: true, bulge: 0.22, spread: 0.16, headOffset: 0) };
                    particleCounts = new[] { RingParticleCount };
                    break;
            }

            dust = new Dust[IsWarp ? WarpStreakCount : DustParticleCount];

            var particleList = new List<Particle>();
            for (int t = 0; t < tracks.Length; t++)
            {
                int count = particleCounts[t];

                for (int i = 0; i < count; i++)
                {
                    particleList.Add(new Particle
                    {
                        Track = t,
                        U = (i + random.NextDouble() * 0.6) / count,
                        Offset = (random.NextDouble() - 0.5) * tracks[t].Spread,
                        Phase = random.NextDouble() * Math.PI * 2,
                        Speed = 0.8 + random.NextDouble() * 2.2
                    });
                }
            }

            particles = particleList.ToArray();
            ResetParticles();
        }

        /// <summary>Ring with the given radius; the comet head runs clockwise or counter-clockwise along it.</summary>
        private static Track CreateRingTrack(double radius, bool clockwise, double bulge, double spread, double headOffset)
        {
            var points = new Point[TrackSamples];
            var normals = new Vector[TrackSamples];

            for (int k = 0; k < TrackSamples; k++)
            {
                double angle = k / (double)TrackSamples * Math.PI * 2 * (clockwise ? 1 : -1);
                normals[k] = new Vector(Math.Cos(angle), Math.Sin(angle));
                points[k] = new Point(normals[k].X * radius, normals[k].Y * radius);
            }

            return new Track { Points = points, Normals = normals, Bulge = bulge, Spread = spread, HeadOffset = headOffset };
        }

        /// <summary>
        /// Outline of a polygon with rounded corners (the polygon grown by cornerRadius): straight pieces parallel to
        /// the edges and circular arcs around the corners, sampled at equal arc length.
        /// </summary>
        private static Track CreateRoundedPolygonTrack(Point[] polygon, double cornerRadius, double bulge, double spread, double headOffset)
        {
            int n = polygon.Length;
            Point centroid = new(polygon.Average(p => p.X), polygon.Average(p => p.Y));

            var edgeNormals = new Vector[n];
            for (int i = 0; i < n; i++)
            {
                Point a = polygon[i];
                Point b = polygon[(i + 1) % n];
                Vector normal = new(b.Y - a.Y, a.X - b.X);
                normal.Normalize();

                Point middle = new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                if (Vector.Multiply(normal, middle - centroid) < 0)
                    normal = -normal;

                edgeNormals[i] = normal;
            }

            // Pieces: edge i, then the arc around the end corner of edge i
            var lengths = new double[n * 2];
            var arcStart = new double[n];
            var arcSweep = new double[n];

            for (int i = 0; i < n; i++)
            {
                lengths[i * 2] = (polygon[(i + 1) % n] - polygon[i]).Length;

                Vector from = edgeNormals[i];
                Vector to = edgeNormals[(i + 1) % n];
                double start = Math.Atan2(from.Y, from.X);
                double sweep = Math.Atan2(to.Y, to.X) - start;
                while (sweep > Math.PI) sweep -= Math.PI * 2;
                while (sweep < -Math.PI) sweep += Math.PI * 2;

                arcStart[i] = start;
                arcSweep[i] = sweep;
                lengths[i * 2 + 1] = Math.Abs(sweep) * cornerRadius;
            }

            double total = lengths.Sum();
            var points = new Point[TrackSamples];
            var normals = new Vector[TrackSamples];

            for (int k = 0; k < TrackSamples; k++)
            {
                double s = k / (double)TrackSamples * total;
                int piece = 0;
                while (piece < lengths.Length - 1 && s > lengths[piece])
                {
                    s -= lengths[piece];
                    piece++;
                }

                int i = piece / 2;
                double t = lengths[piece] > 0 ? s / lengths[piece] : 0;

                if (piece % 2 == 0)
                {
                    Point a = polygon[i];
                    Point b = polygon[(i + 1) % n];
                    normals[k] = edgeNormals[i];
                    points[k] = a + (b - a) * t + edgeNormals[i] * cornerRadius;
                }
                else
                {
                    double angle = arcStart[i] + arcSweep[i] * t;
                    Vector normal = new(Math.Cos(angle), Math.Sin(angle));
                    normals[k] = normal;
                    points[k] = polygon[(i + 1) % n] + normal * cornerRadius;
                }
            }

            return new Track { Points = points, Normals = normals, Bulge = bulge, Spread = spread, HeadOffset = headOffset };
        }

        private static int SampleIndex(double u)
        {
            int index = (int)(u * TrackSamples) % TrackSamples;
            return index < 0 ? index + TrackSamples : index;
        }

        /// <summary>Puts the particles onto their outline at once and spreads new dust.</summary>
        private void ResetParticles()
        {
            time = 0;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                Track track = tracks[p.Track];
                int index = SampleIndex(p.U);
                Point point = track.Points[index] + track.Normals[index] * p.Offset;

                p.X = point.X;
                p.Y = point.Y;
                p.VX = 0;
                p.VY = 0;
            }

            for (int i = 0; i < dust.Length; i++)
            {
                if (IsWarp)
                {
                    // Spread over the whole way at once, so the streaks don't start as one wave
                    dust[i] = CreateWarpStreak(WarpStreakStart + random.NextDouble() * (WarpStreakEnd - WarpStreakStart));
                    continue;
                }

                dust[i] = new Dust
                {
                    X = (random.NextDouble() * 2 - 1) * 1.4,
                    Y = (random.NextDouble() * 2 - 1) * 1.4,
                    VX = (random.NextDouble() - 0.5) * 0.12,
                    VY = (random.NextDouble() - 0.5) * 0.12,
                    Phase = random.NextDouble() * Math.PI * 2
                };
            }
        }

        /// <summary>Warp streak at the given distance from the center in a random direction.</summary>
        private Dust CreateWarpStreak(double distance)
        {
            double angle = random.NextDouble() * Math.PI * 2;

            return new Dust
            {
                X = Math.Cos(angle) * distance,
                Y = Math.Sin(angle) * distance,
                Phase = random.NextDouble() * Math.PI * 2
            };
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

            double dt = Math.Min(0.1, (args.RenderingTime - lastRenderingTime).TotalSeconds);
            lastRenderingTime = args.RenderingTime;

            Update(dt);
            InvalidateVisual();
        }

        /// <summary>
        /// Distance behind the comet head of the particle's track, in radians of one lap
        /// (0 = at the head, growing along the tail up to 2 pi).
        /// </summary>
        private double DistanceBehindHead(in Particle p)
        {
            double head = time * BaseHeadSpeed / (Math.PI * 2) + tracks[p.Track].HeadOffset;
            double distance = (head - p.U) % 1;
            if (distance < 0)
                distance += 1;

            return distance * Math.PI * 2;
        }

        private void Update(double dt)
        {
            // Speed scales the animation time (head, breathing, dancing), not the spring physics
            time += dt * Speed;

            // Coordinates are relative to the ring radius (1 = on the ring); the shape breathes slightly
            double breathing = 1 + 0.05 * Math.Sin(time * 1.7);

            int steps = (int)Math.Ceiling(dt / 0.016);
            double step = dt / steps;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                Track track = tracks[p.Track];
                int index = SampleIndex(p.U);

                double bulge = track.Bulge * Math.Exp(-DistanceBehindHead(p) / 0.35);
                Point target = new Point(track.Points[index].X * breathing, track.Points[index].Y * breathing)
                    + track.Normals[index] * (p.Offset + bulge);

                // Spring physics in sub-steps to stay stable on frame drops
                for (int s = 0; s < steps; s++)
                {
                    p.VX += ((target.X - p.X) * SpringStrength - p.VX * Damping) * step;
                    p.VY += ((target.Y - p.Y) * SpringStrength - p.VY * Damping) * step;
                    p.X += p.VX * step;
                    p.Y += p.VY * step;
                }
            }

            for (int i = 0; i < dust.Length; i++)
            {
                ref var d = ref dust[i];

                if (IsWarp)
                {
                    // Accelerates outwards, a new streak is born in the middle when it has left the shape;
                    // reversed it comes from outside, slows down towards the middle and is born outside again
                    bool reverse = Reverse;
                    double distance = Math.Sqrt(d.X * d.X + d.Y * d.Y);
                    double speed = WarpSpeed(distance) * dt * Speed * (reverse ? -1 : 1);
                    d.X += d.X / distance * speed;
                    d.Y += d.Y / distance * speed;

                    if (!reverse && distance > WarpStreakEnd)
                        d = CreateWarpStreak(WarpStreakStart + random.NextDouble() * 0.15);
                    else if (reverse && distance < WarpStreakStart)
                        d = CreateWarpStreak(WarpStreakEnd - random.NextDouble() * 0.15);

                    continue;
                }

                d.X += d.VX * dt;
                d.Y += d.VY * dt;

                if (d.X < -1.5) d.X += 3;
                if (d.X > 1.5) d.X -= 3;
                if (d.Y < -1.5) d.Y += 3;
                if (d.Y > 1.5) d.Y -= 3;
            }
        }

        /// <summary>Speed of a warp streak (units per second) at the given distance from the center.</summary>
        private static double WarpSpeed(double distance)
        {
            return 0.25 + distance * distance * 1.6;
        }

        /// <summary>
        /// Rebuilds the brushes when the accent color has changed (e.g. a profile with its own accent color).
        /// Dim particles use a darkened accent color, bright ones are lightened towards white.
        /// </summary>
        private void EnsureBrushes()
        {
            var accent = System.Windows.Application.Current?.TryFindResource("Color.Accent") is Color c ? c : fallbackAccent;
            accent.A = 255;

            if (brushAccent == accent)
                return;

            brushAccent = accent;

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
                glowColor.A = (byte)(t * 70);
                var transparent = glowColor;
                transparent.A = 0;
                var glow = new RadialGradientBrush(glowColor, transparent);
                glow.Freeze();
                glowBrushes[i] = glow;

                var streak = new Pen(dot, 1.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                streak.Freeze();
                streakPens[i] = streak;
            }
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (!isRunning || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            EnsureBrushes();

            double cx = ActualWidth / 2;
            double cy = ActualHeight / 2;
            double unitFactor = Shape switch
            {
                CFParticleSpinnerShape.PlayButton => PlayButtonUnitFactor,
                CFParticleSpinnerShape.Launch => LaunchUnitFactor,
                _ => RingRadiusFactor
            };
            double scale = Math.Min(ActualWidth, ActualHeight) / 2 * unitFactor;
            double dotSize = scale / 30;

            // Dust behind the shape
            for (int i = 0; i < dust.Length; i++)
            {
                ref var d = ref dust[i];

                if (IsWarp)
                {
                    DrawWarpStreak(dc, d, cx, cy, scale);
                    continue;
                }

                double brightness = 0.1 + 0.14 * (0.5 + 0.5 * Math.Sin(time * 0.9 + d.Phase));
                int level = (int)(brightness * (BrushLevels - 1));
                dc.DrawEllipse(dotBrushes[level], null, new Point(cx + d.X * scale, cy + d.Y * scale), dotSize * 0.7, dotSize * 0.7);
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];

                // Slight dancing around the spring position
                double ox = Math.Sin(time * p.Speed + p.Phase) * 0.025;
                double oy = Math.Cos(time * p.Speed * 0.8 + p.Phase * 1.7) * 0.025;

                double tail = Math.Exp(-DistanceBehindHead(p) / TailLength * 2.2);
                double motion = Math.Min(1, Math.Sqrt(p.VX * p.VX + p.VY * p.VY) / 4);
                double twinkle = 0.08 * Math.Sin(time * p.Speed * 1.5 + p.Phase);

                double brightness = Math.Clamp(0.14 + tail * 0.86 + twinkle + motion * 0.4, 0, 1);
                int level = (int)(brightness * (BrushLevels - 1));
                double radius = (0.7 + brightness * 0.8) * dotSize;
                var point = new Point(cx + (p.X + ox) * scale, cy + (p.Y + oy) * scale);

                if (brightness > 0.7)
                    dc.DrawEllipse(glowBrushes[level], null, point, radius * 4, radius * 4);

                dc.DrawEllipse(dotBrushes[level], null, point, radius, radius);
            }
        }

        /// <summary>
        /// Line from the streak's position back to where it comes from (the center, reversed the outside);
        /// the faster the streak, the longer the line. Fades in after its birth and out before it vanishes.
        /// </summary>
        private void DrawWarpStreak(DrawingContext dc, in Dust d, double cx, double cy, double scale)
        {
            double distance = Math.Sqrt(d.X * d.X + d.Y * d.Y);
            double progress = Math.Clamp((distance - WarpStreakStart) / (WarpStreakEnd - WarpStreakStart), 0, 1);
            double length = (0.03 + WarpSpeed(distance) * 0.09) * (Reverse ? -1 : 1);

            double fade = Math.Sin(progress * Math.PI);
            double twinkle = 0.1 * Math.Sin(time * 3 + d.Phase);
            double brightness = Math.Clamp(0.12 + fade * 0.7 + twinkle, 0, 1);
            int level = (int)(brightness * (BrushLevels - 1));

            var head = new Point(cx + d.X * scale, cy + d.Y * scale);
            var tail = new Point(cx + (d.X - d.X / distance * length) * scale, cy + (d.Y - d.Y / distance * length) * scale);

            dc.DrawLine(streakPens[level], tail, head);
        }
    }
}
