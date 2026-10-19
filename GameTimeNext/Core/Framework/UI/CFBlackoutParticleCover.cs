using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.Profiles;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Profile cover variant for the blackout mode.
    /// The cover of the currently selected profile is made of slightly "dancing" particles,
    /// next to it the profile name and its total playtime are shown.
    /// Every minute a ripple runs through the cover and from time to time a light sweep passes over it.
    /// When the selected profile changes, the particles scatter and reassemble into the new cover.
    /// The whole card slowly wanders across the screen to prevent burn-in.
    /// </summary>
    internal sealed class CFBlackoutParticleCover : CFBlackoutAnimationBase
    {
        private const int TargetParticleCount = 12000;
        private const double ReferenceCoverHeight = 67d;
        private const int AccentLevels = 32;
        private const double MaxCellPixels = 4d;
        private const double BitmapMargin = 1.8d;
        private const int MaxSparks = 450;
        private const double SparksPerSecond = 70d;
        private const double DotRadius = 0.34d;
        private const int DustParticleCount = 60;
        private const double WanderSpeed = 22d;
        private const double CardOpacity = 0.8;
        private const double SpringStrength = 16d;
        private const double Damping = 6.5d;
        private const double RefreshInterval = 30d;
        private const double SweepInterval = 16d;
        private const double SweepDuration = 3.5d;
        private const double RippleDuration = 2.6d;
        private const double ImpactDuration = 0.9d;
        private const double ImpactStrength = 16d;
        private const double MinFactor = 0.5d;
        private const double MaxFactor = 1.25d;

        // OLED protection
        private const double MaxCoverLuminance = 165d;   // bright cover pixels get compressed above this (soft knee)
        private const double DimStartMinutes = 2d;       // after this the whole card slowly dims ...
        private const double DimFullMinutes = 20d;       // ... until it reaches MinBrightness
        private const double MinBrightness = 0.55d;
        private const double RelocateInterval = 480d;    // every 8 minutes the card dissolves and reappears elsewhere
        private const double RelocateOutDuration = 1.6d;
        private const double RelocateInDuration = 1.4d;

        private struct Particle
        {
            public double X, Y, VX, VY, TX, TY, Phase, Speed;
            public double Brightness;
            public byte R, G, B;
        }

        /// <summary>
        /// Particle that detached from the cover and drifts away while fading out.
        /// </summary>
        private struct Spark
        {
            public double X, Y, VX, VY, Age, Life, Size, Phase;
            public byte R, G, B;
        }

        private struct Glow
        {
            public double X, Y, Intensity;
        }

        private struct Dust
        {
            public double X, Y, VX, VY, Phase;
        }

        private readonly record struct CoverPixel(double X, double Y, Color Color);

        private sealed class CoverData
        {
            public List<CoverPixel> Pixels { get; init; } = new();
            public double HalfWidth { get; init; }
            public double HalfHeight { get; init; }
            public double CellSize { get; init; } = 1;
        }

        /// <summary>
        /// Caches a FormattedText and only rebuilds it when text, size or brush change.
        /// </summary>
        private sealed class CachedText
        {
            private FormattedText? text;
            private string key = string.Empty;

            public FormattedText Get(string value, Typeface typeface, double emSize, Brush brush, double pixelsPerDip, double maxWidth, int maxLines)
            {
                string newKey = string.Join("|", value, emSize, brush.GetHashCode(), pixelsPerDip, Math.Round(maxWidth), maxLines);
                if (text == null || newKey != key)
                {
                    text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, emSize, brush, pixelsPerDip)
                    {
                        MaxTextWidth = Math.Max(1, maxWidth),
                        MaxLineCount = maxLines,
                        Trimming = TextTrimming.CharacterEllipsis
                    };
                    key = newKey;
                }

                return text;
            }
        }

        private static readonly Typeface nameTypeface = new(
            new FontFamily("Bahnschrift, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        private static readonly Typeface infoTypeface = new(
            new FontFamily("Bahnschrift, Segoe UI"), FontStyles.Normal, FontWeights.Light, FontStretches.Normal);

        private static readonly Color fallbackAccent = Color.FromRgb(0x34, 0x98, 0xE5);

        private readonly Random random = new();

        // The cover particles are rasterized into a single bitmap instead of thousands of DrawEllipse calls
        private WriteableBitmap? coverBitmap;
        private int[] coverBuffer = Array.Empty<int>();
        private int bitmapWidth;
        private int bitmapHeight;
        private double renderScale = 1;
        private readonly List<Glow> glows = new();
        private Color glowColor = Colors.White;

        private readonly SolidColorBrush[] accentBrushes = new SolidColorBrush[AccentLevels];
        private SolidColorBrush nameBrush = Brushes.Transparent;
        private SolidColorBrush labelBrush = Brushes.Transparent;
        private SolidColorBrush playtimeBrush = Brushes.Transparent;
        private Color? brushAccent;

        private readonly CachedText nameText = new();
        private readonly CachedText labelText = new();
        private readonly CachedText playtimeText = new();

        private Particle[] particles = Array.Empty<Particle>();
        private readonly Dust[] dust = new Dust[DustParticleCount];

        private long currentPfid;
        private string currentCoverFile = string.Empty;
        private string profileName = string.Empty;
        private string playtimeString = string.Empty;
        private double refreshTimer;

        private double coverHalfWidth = 1;
        private double coverHalfHeight = 1;
        private double cellSize = 1;

        private bool isRunning;
        private TimeSpan lastRenderingTime = TimeSpan.Zero;
        private double time;
        private double centerX = double.NaN;
        private double centerY = double.NaN;
        private double velocityX;
        private double velocityY;

        private double sweepAge = SweepDuration;
        private double rippleAge = RippleDuration;
        private double rippleX;
        private double rippleY;
        private int lastMinute = -1;

        private readonly List<Spark> sparks = new();
        private double sparkSpawn;
        private double disturbanceTimer = 3;

        private enum RelocatePhase { None, Dissolving, Assembling }

        // The first appearance uses the same fade-in as a relocation (smooth handover from the transition)
        private RelocatePhase relocatePhase = RelocatePhase.Assembling;
        private double relocateTimer;
        private double relocateAge;
        private double fade = 0;
        private double springScale = 1;
        private double wanderMinX = double.NaN, wanderMaxX, wanderMinY, wanderMaxY;

        private double impactAge = ImpactDuration;
        private int impactDirX;
        private int impactDirY;

        // Layout of the current frame (pixels)
        private double scale = 1;
        private double groupHalfWidth;
        private double textWidth;
        private double textGap;

        private CFBlackoutParticleCover(T1PROFI profile, CoverData cover)
        {
            IsHitTestVisible = false;
            Focusable = false;
            Opacity = 0;

            // Moving text: without this WPF snaps the glyphs to whole pixels and the text stutters
            TextOptions.SetTextHintingMode(this, TextHintingMode.Animated);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);

            ApplyProfile(profile, cover);

            for (int i = 0; i < dust.Length; i++)
            {
                dust[i] = new Dust
                {
                    X = (random.NextDouble() * 2 - 1) * coverHalfWidth * 1.6,
                    Y = (random.NextDouble() * 2 - 1) * coverHalfHeight * 1.3,
                    VX = (random.NextDouble() - 0.5) * 1.2,
                    VY = (random.NextDouble() - 0.5) * 0.9,
                    Phase = random.NextDouble() * Math.PI * 2
                };
            }

            velocityX = random.NextDouble() > 0.5 ? WanderSpeed : -WanderSpeed;
            velocityY = random.NextDouble() > 0.5 ? WanderSpeed * 0.75 : -WanderSpeed * 0.75;
        }

        /// <summary>
        /// Creates the animation for the currently selected profile.
        /// Returns null if there is no selected profile or its cover can't be loaded.
        /// </summary>
        public static CFBlackoutParticleCover? TryCreate()
        {
            var profile = AppEnvironment.GetCurrentProfile();
            if (profile == null || profile.PFID == 0)
            {
                return null;
            }

            var cover = SampleCover(profile.PPFN);
            return cover == null ? null : new CFBlackoutParticleCover(profile, cover);
        }

        public override void Start()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            lastRenderingTime = TimeSpan.Zero;
            CompositionTarget.Rendering += OnRendering;
        }

        public override void Stop()
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

            // Small cap: a dropped frame slows the motion down a bit instead of making it jump
            double dt = Math.Min(0.05, (args.RenderingTime - lastRenderingTime).TotalSeconds);
            lastRenderingTime = args.RenderingTime;

            Update(dt);
            InvalidateVisual();
        }

        // ------------------------------------------------------------------
        // Profile data
        // ------------------------------------------------------------------

        private static CoverData? SampleCover(string coverFile)
        {
            if (string.IsNullOrWhiteSpace(coverFile))
            {
                return null;
            }

            string path = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath ?? string.Empty, coverFile);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                int sourceWidth, sourceHeight;
                using (var stream = File.OpenRead(path))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    sourceWidth = decoder.Frames[0].PixelWidth;
                    sourceHeight = decoder.Frames[0].PixelHeight;
                }

                if (sourceWidth <= 0 || sourceHeight <= 0)
                {
                    return null;
                }

                // One particle per pixel of the downscaled cover
                double aspect = sourceWidth / (double)sourceHeight;
                int cols = Math.Clamp((int)Math.Round(Math.Sqrt(TargetParticleCount * aspect)), 16, 200);
                int rows = Math.Clamp((int)Math.Round(cols / aspect), 16, 300);

                // Keep the model size independent of the resolution so all motion values stay the same
                double cellSize = ReferenceCoverHeight / rows;

                var image = new BitmapImage();
                using (var stream = File.OpenRead(path))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.DecodePixelWidth = cols;
                    image.DecodePixelHeight = rows;
                    image.EndInit();
                    image.Freeze();
                }

                var converted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
                int width = converted.PixelWidth;
                int height = converted.PixelHeight;
                var buffer = new byte[width * height * 4];
                converted.CopyPixels(buffer, width * 4, 0);

                var random = new Random();
                var pixels = new List<CoverPixel>(width * height);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int idx = (y * width + x) * 4;
                        byte b = buffer[idx], g = buffer[idx + 1], r = buffer[idx + 2], a = buffer[idx + 3];

                        double luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;

                        // Transparent and (almost) black pixels are invisible on the blackout anyway
                        if (a < 128 || luminance < 12)
                        {
                            continue;
                        }

                        // OLED: compress highlights instead of clipping them, so the contrast stays
                        // but large white areas don't drive the panel at full brightness
                        double k = 1;
                        if (luminance > MaxCoverLuminance)
                        {
                            k = (MaxCoverLuminance + (luminance - MaxCoverLuminance) * 0.3) / luminance;
                        }

                        pixels.Add(new CoverPixel(
                            (x - width * 0.5 + 0.5 + (random.NextDouble() - 0.5) * 0.15) * cellSize,
                            (y - height * 0.5 + 0.5 + (random.NextDouble() - 0.5) * 0.15) * cellSize,
                            Color.FromRgb((byte)(r * k), (byte)(g * k), (byte)(b * k))));
                    }
                }

                if (pixels.Count == 0)
                {
                    return null;
                }

                // Shuffle so particles get random targets when switching to another cover
                for (int i = pixels.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (pixels[i], pixels[j]) = (pixels[j], pixels[i]);
                }

                return new CoverData { Pixels = pixels, HalfWidth = width * 0.5 * cellSize, HalfHeight = height * 0.5 * cellSize, CellSize = cellSize };
            }
            catch
            {
                return null;
            }
        }

        private void ApplyProfile(T1PROFI profile, CoverData cover)
        {
            bool firstCover = particles.Length == 0;

            currentPfid = profile.PFID;
            currentCoverFile = profile.PPFN;
            profileName = profile.GANA;
            coverHalfWidth = cover.HalfWidth;
            coverHalfHeight = cover.HalfHeight;
            cellSize = cover.CellSize;

            var old = particles;
            particles = new Particle[cover.Pixels.Count];

            for (int i = 0; i < particles.Length; i++)
            {
                var pixel = cover.Pixels[i];
                ref var p = ref particles[i];

                if (firstCover)
                {
                    // Start as a loose cloud (inside the bitmap area) that condenses into the cover
                    p.X = (random.NextDouble() * 2 - 1) * coverHalfWidth * BitmapMargin * 0.95;
                    p.Y = (random.NextDouble() * 2 - 1) * coverHalfHeight * BitmapMargin * 0.95;
                }
                else
                {
                    // Reuse the old particles, additional ones spawn from a random old particle
                    var source = old[i < old.Length ? i : random.Next(old.Length)];
                    double angle = random.NextDouble() * Math.PI * 2;
                    double speed = 15 + random.NextDouble() * 40;
                    p.X = source.X;
                    p.Y = source.Y;
                    p.VX = source.VX + Math.Cos(angle) * speed;
                    p.VY = source.VY + Math.Sin(angle) * speed;
                }

                p.Phase = random.NextDouble() * Math.PI * 2;
                p.Speed = 0.6 + random.NextDouble() * 1.8;
                p.Brightness = 0.82 + random.NextDouble() * 0.28;
                p.TX = pixel.X;
                p.TY = pixel.Y;
                p.R = pixel.Color.R;
                p.G = pixel.Color.G;
                p.B = pixel.Color.B;
            }

            RefreshPlaytime();
        }

        private void RefreshPlaytime()
        {
            try
            {
                playtimeString = CFProfilesApp.FormatGameTimeHours(TFPROFI.GetTotalGameTimeInMinutes(currentPfid));
            }
            catch
            {
                playtimeString = "n.A.";
            }
        }

        /// <summary>
        /// Checks if another profile was selected in the meantime and updates the playtime.
        /// </summary>
        private void RefreshProfile()
        {
            var profile = AppEnvironment.GetCurrentProfile();
            if (profile == null || profile.PFID == 0)
            {
                RefreshPlaytime();
                return;
            }

            if (profile.PFID != currentPfid || profile.PPFN != currentCoverFile)
            {
                var cover = SampleCover(profile.PPFN);
                if (cover != null)
                {
                    ApplyProfile(profile, cover);
                    return;
                }
            }

            profileName = profile.GANA;
            RefreshPlaytime();
        }

        // ------------------------------------------------------------------
        // Brushes
        // ------------------------------------------------------------------

        /// <summary>
        /// Rebuilds the accent brushes when the accent color has changed
        /// (e.g. a profile switch with its own accent color while the blackout is active).
        /// </summary>
        private void EnsureAccentBrushes()
        {
            var accent = System.Windows.Application.Current?.TryFindResource("Color.Accent") is Color c ? c : fallbackAccent;
            accent.A = 255;

            if (brushAccent == accent)
            {
                return;
            }

            brushAccent = accent;

            var dim = Lerp(Colors.Black, accent, 0.65);
            var bright = Lerp(accent, Colors.White, 0.6);

            for (int i = 0; i < AccentLevels; i++)
            {
                double t = i / (double)(AccentLevels - 1);
                var color = Lerp(dim, bright, t);

                color.A = (byte)(30 + t * 225);
                var dot = new SolidColorBrush(color);
                dot.Freeze();
                accentBrushes[i] = dot;
            }

            glowColor = Lerp(bright, Colors.White, 0.5);

            // OLED: no pure white, the text is the most static element of the card
            nameBrush = CreateFrozenBrush(Lerp(accent, Colors.White, 0.7), 205);
            labelBrush = CreateFrozenBrush(Lerp(accent, Colors.White, 0.2), 140);
            playtimeBrush = CreateFrozenBrush(Lerp(accent, Colors.White, 0.4), 200);
        }

        private static SolidColorBrush CreateFrozenBrush(Color color, byte alpha)
        {
            color.A = alpha;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t));
        }

        private int AccentLevel(double brightness)
        {
            return (int)(Math.Clamp(brightness, 0, 1) * (AccentLevels - 1));
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        private void Update(double dt)
        {
            time += dt;

            UpdateRelocation(dt);

            double opacity = CardOpacity * GetLongTermDim() * fade;
            if (Math.Abs(Opacity - opacity) > 0.002)
            {
                Opacity = opacity;
            }

            refreshTimer += dt;
            if (refreshTimer >= RefreshInterval)
            {
                refreshTimer = 0;
                RefreshProfile();
            }

            // A ripple runs through the cover on every new minute
            int minute = DateTime.Now.Minute;
            if (lastMinute != -1 && minute != lastMinute && particles.Length > 0)
            {
                var origin = particles[random.Next(particles.Length)];
                rippleX = origin.TX;
                rippleY = origin.TY;
                rippleAge = 0;
            }
            lastMinute = minute;
            rippleAge = Math.Min(RippleDuration, rippleAge + dt);

            sweepAge += dt;
            if (sweepAge >= SweepInterval)
            {
                sweepAge = 0;
            }

            // From time to time the particles at a random spot get blown apart and spring back
            disturbanceTimer -= dt;
            if (disturbanceTimer <= 0 && particles.Length > 0)
            {
                disturbanceTimer = 3.5 + random.NextDouble() * 4;
                Disturb();
            }

            UpdateSparks(dt);

            // Spring physics in sub-steps to stay stable on frame drops
            int steps = (int)Math.Ceiling(dt / 0.016);
            double step = dt / steps;
            for (int s = 0; s < steps; s++)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    ref var p = ref particles[i];
                    p.VX += ((p.TX - p.X) * SpringStrength * springScale - p.VX * Damping) * step;
                    p.VY += ((p.TY - p.Y) * SpringStrength * springScale - p.VY * Damping) * step;
                    p.X += p.VX * step;
                    p.Y += p.VY * step;
                }
            }

            double dustRangeX = coverHalfWidth * 1.6;
            double dustRangeY = coverHalfHeight * 1.3;
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

            UpdateLayoutMetrics();

            // Wander across the screen (burn-in protection)
            double extentX = groupHalfWidth + coverHalfWidth * scale * 0.15;
            double extentY = coverHalfHeight * scale * 1.15;
            double minX = extentX, maxX = Math.Max(extentX, ActualWidth - extentX);
            double minY = extentY, maxY = Math.Max(extentY, ActualHeight - extentY);
            wanderMinX = minX; wanderMaxX = maxX; wanderMinY = minY; wanderMaxY = maxY;
            bool bounced = false;

            if (double.IsNaN(centerX))
            {
                centerX = ActualWidth * 0.5;
                centerY = ActualHeight * 0.5;
            }

            centerX += velocityX * dt;
            centerY += velocityY * dt;

            if (centerX <= minX || centerX >= maxX)
            {
                double newVelocityX = centerX <= minX ? Math.Abs(velocityX) : -Math.Abs(velocityX);
                if (Math.Sign(newVelocityX) != Math.Sign(velocityX))
                {
                    Impact(Math.Sign(velocityX), 0);
                    bounced = true;
                }

                velocityX = newVelocityX;
                centerX = Math.Clamp(centerX, minX, maxX);
            }

            if (centerY <= minY || centerY >= maxY)
            {
                double newVelocityY = centerY <= minY ? Math.Abs(velocityY) : -Math.Abs(velocityY);
                if (Math.Sign(newVelocityY) != Math.Sign(velocityY))
                {
                    Impact(0, Math.Sign(velocityY));
                    bounced = true;
                }

                velocityY = newVelocityY;
                centerY = Math.Clamp(centerY, minY, maxY);
            }

            if (bounced)
            {
                RandomizeDirection();
            }

            impactAge = Math.Min(ImpactDuration, impactAge + dt);
        }

        /// <summary>
        /// OLED: slowly dims the whole card the longer the blackout is active.
        /// </summary>
        private double GetLongTermDim()
        {
            double t = Math.Clamp((time / 60 - DimStartMinutes) / (DimFullMinutes - DimStartMinutes), 0, 1);
            t = t * t * (3 - 2 * t);
            return 1 - (1 - MinBrightness) * t;
        }

        /// <summary>
        /// OLED: changes the angle slightly on every bounce, so the card does not run
        /// the same path over and over again and every area of the screen gets covered.
        /// The angle stays diagonal enough to avoid long horizontal or vertical runs.
        /// </summary>
        private void RandomizeDirection()
        {
            double speed = Math.Sqrt(velocityX * velocityX + velocityY * velocityY);
            double angle = Math.Atan2(Math.Abs(velocityY), Math.Abs(velocityX)) + (random.NextDouble() - 0.5) * 0.35;
            angle = Math.Clamp(angle, 0.45, 1.1);

            velocityX = Math.Sign(velocityX) * Math.Cos(angle) * speed;
            velocityY = Math.Sign(velocityY) * Math.Sin(angle) * speed;
        }

        /// <summary>
        /// OLED: every few minutes the card dissolves into a cloud, fades out and assembles again
        /// at a random position. Gives the panel a short rest and breaks up long-term patterns.
        /// </summary>
        private void UpdateRelocation(double dt)
        {
            switch (relocatePhase)
            {
                case RelocatePhase.None:
                    relocateTimer += dt;
                    if (relocateTimer >= RelocateInterval && impactAge >= ImpactDuration && particles.Length > 0)
                    {
                        relocateTimer = 0;
                        relocateAge = 0;
                        relocatePhase = RelocatePhase.Dissolving;
                        Dissolve();
                    }
                    break;

                case RelocatePhase.Dissolving:
                    relocateAge += dt;
                    fade = 1 - SmoothStep(relocateAge / RelocateOutDuration);
                    if (relocateAge >= RelocateOutDuration)
                    {
                        relocateAge = 0;
                        relocatePhase = RelocatePhase.Assembling;
                        Reassemble();
                    }
                    break;

                case RelocatePhase.Assembling:
                    relocateAge += dt;
                    fade = SmoothStep(relocateAge / RelocateInDuration);
                    if (relocateAge >= RelocateInDuration)
                    {
                        fade = 1;
                        relocatePhase = RelocatePhase.None;
                    }
                    break;
            }
        }

        private static double SmoothStep(double t)
        {
            t = Math.Clamp(t, 0, 1);
            return t * t * (3 - 2 * t);
        }

        private void Dissolve()
        {
            // Release the springs so the particles drift apart
            springScale = 0;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                double angle = random.NextDouble() * Math.PI * 2;
                double speed = 8 + random.NextDouble() * 30;
                p.VX += p.TX / coverHalfWidth * 12 + Math.Cos(angle) * speed;
                p.VY += p.TY / coverHalfHeight * 12 + Math.Sin(angle) * speed - 6;
            }

            for (int i = 0; i < 120; i++)
            {
                SpawnSpark(particles[random.Next(particles.Length)], 4 + random.NextDouble() * 10);
            }
        }

        private void Reassemble()
        {
            if (!double.IsNaN(wanderMinX))
            {
                centerX = wanderMinX + random.NextDouble() * (wanderMaxX - wanderMinX);
                centerY = wanderMinY + random.NextDouble() * (wanderMaxY - wanderMinY);
            }

            double angle = 0.45 + random.NextDouble() * 0.65;
            velocityX = Math.Cos(angle) * WanderSpeed * 1.25 * (random.NextDouble() > 0.5 ? 1 : -1);
            velocityY = Math.Sin(angle) * WanderSpeed * 1.25 * (random.NextDouble() > 0.5 ? 1 : -1);

            // Start as a loose cloud again, like on the first appearance
            sparks.Clear();
            springScale = 1;
            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                p.X = (random.NextDouble() * 2 - 1) * coverHalfWidth * BitmapMargin * 0.95;
                p.Y = (random.NextDouble() * 2 - 1) * coverHalfHeight * BitmapMargin * 0.95;
                p.VX = 0;
                p.VY = 0;
            }
        }

        /// <summary>
        /// Position of a particle between the impacted edge (0) and the opposite edge (1).
        /// </summary>
        private double ImpactDepth(double tx, double ty)
        {
            double half = Math.Abs(impactDirX) * coverHalfWidth + Math.Abs(impactDirY) * coverHalfHeight;
            return (half - (tx * impactDirX + ty * impactDirY)) / (half * 2);
        }

        /// <summary>
        /// The card hit a screen edge. dirX/dirY point towards the wall.
        /// The particles keep moving towards the wall due to inertia (the cover gets squashed and springs back),
        /// a shockwave runs through the cover and sparks splash off the impacted edge.
        /// </summary>
        private void Impact(int dirX, int dirY)
        {
            if (particles.Length == 0)
            {
                return;
            }

            impactDirX = dirX;
            impactDirY = dirY;
            impactAge = 0;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                double depth = ImpactDepth(p.TX, p.TY);

                // The far side keeps moving the most, the edge at the wall stops -> squash
                double push = ImpactStrength * (0.25 + 0.75 * depth);
                p.VX += dirX * push;
                p.VY += dirY * push;

                // Particles directly at the wall splash sideways
                if (depth < 0.06)
                {
                    double splash = (random.NextDouble() - 0.5) * ImpactStrength * 1.5;
                    p.VX += dirY * splash - dirX * ImpactStrength * 0.6;
                    p.VY += dirX * splash - dirY * ImpactStrength * 0.6;
                }
            }

            // Sparks bounce off the wall, mostly along it
            for (int i = 0, spawned = 0; i < 400 && spawned < 45; i++)
            {
                var source = particles[random.Next(particles.Length)];
                if (ImpactDepth(source.TX, source.TY) > 0.05)
                {
                    continue;
                }

                spawned++;
                double along = (random.NextDouble() - 0.5) * 2;
                double back = 0.2 + random.NextDouble() * 0.6;
                double speed = 6 + random.NextDouble() * 12;
                double vx = (dirY * along - dirX * back) * speed;
                double vy = (dirX * along - dirY * back) * speed;
                if (SpawnSpark(source, 0))
                {
                    var s = sparks[^1];
                    s.VX = vx;
                    s.VY = vy;
                    s.Life = 1 + random.NextDouble() * 1.5;
                    sparks[^1] = s;
                }
            }
        }

        /// <summary>
        /// Slowly changing air current that carries the sparks.
        /// </summary>
        private void GetFlow(double x, double y, out double fx, out double fy)
        {
            double breathe = 0.6 + 0.4 * Math.Sin(time * 0.23);
            fx = (Math.Sin(y * 0.11 + time * 0.8) * 0.55 + Math.Sin((x + y) * 0.045 - time * 0.5) * 0.45) * breathe;
            fy = (Math.Cos(x * 0.09 - time * 0.7) * 0.45 + Math.Sin((x - y) * 0.06 + time * 0.4) * 0.35) * breathe;
        }

        /// <summary>
        /// Blows the particles at a random spot apart, the springs pull them back afterwards.
        /// </summary>
        private void Disturb()
        {
            var origin = particles[random.Next(particles.Length)];
            double radius = coverHalfHeight * (0.12 + random.NextDouble() * 0.1);
            double radiusSq = radius * radius;
            double strength = 12 + random.NextDouble() * 10;
            double swirl = random.NextDouble() > 0.5 ? 0.6 : -0.6;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];
                double dx = p.X - origin.TX;
                double dy = p.Y - origin.TY;
                double distSq = dx * dx + dy * dy;
                if (distSq >= radiusSq || distSq < 0.0001)
                {
                    continue;
                }

                double dist = Math.Sqrt(distSq);
                double force = (1 - dist / radius) * strength / dist;
                p.VX += (dx - dy * swirl) * force;
                p.VY += (dy + dx * swirl) * force;
            }

            for (int i = 0; i < 20; i++)
            {
                SpawnSpark(origin, 4 + random.NextDouble() * 6);
            }
        }

        private void UpdateSparks(double dt)
        {
            if (particles.Length > 0)
            {
                sparkSpawn += dt * SparksPerSecond;
                while (sparkSpawn >= 1)
                {
                    sparkSpawn -= 1;
                    SpawnSpark(PickSparkSource(), 1.5 + random.NextDouble() * 4);
                }
            }

            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i];
                s.Age += dt;

                if (s.Age >= s.Life)
                {
                    sparks[i] = sparks[^1];
                    sparks.RemoveAt(sparks.Count - 1);
                    continue;
                }

                // Carried by the wind and slowly rising like embers
                GetFlow(s.X, s.Y, out double fx, out double fy);
                s.VX += (fx * 4 - s.VX * 0.4) * dt;
                s.VY += (fy * 4 - s.VY * 0.4 - 1.2) * dt;
                s.X += s.VX * dt;
                s.Y += s.VY * dt;
                sparks[i] = s;
            }
        }

        /// <summary>
        /// Mostly picks particles at the border of the cover, so it looks like the edges dissolve.
        /// </summary>
        private Particle PickSparkSource()
        {
            var p = particles[random.Next(particles.Length)];
            if (random.NextDouble() < 0.3)
            {
                return p;
            }

            for (int attempt = 0; attempt < 6; attempt++)
            {
                if (Math.Abs(p.TX) > coverHalfWidth * 0.88 || Math.Abs(p.TY) > coverHalfHeight * 0.9)
                {
                    break;
                }

                p = particles[random.Next(particles.Length)];
            }

            return p;
        }

        private bool SpawnSpark(Particle source, double speed)
        {
            if (sparks.Count >= MaxSparks)
            {
                return false;
            }

            // Outwards from the cover center with some randomness
            double angle = Math.Atan2(source.TY, source.TX) + (random.NextDouble() - 0.5) * 1.6;
            sparks.Add(new Spark
            {
                X = source.X,
                Y = source.Y,
                VX = Math.Cos(angle) * speed,
                VY = Math.Sin(angle) * speed,
                Life = 2 + random.NextDouble() * 3,
                Size = 0.6 + random.NextDouble() * 0.7,
                Phase = random.NextDouble() * Math.PI * 2,
                R = (byte)Math.Min(255, source.R + 15),
                G = (byte)Math.Min(255, source.G + 15),
                B = (byte)Math.Min(255, source.B + 15)
            });

            return true;
        }

        private double PixelsPerDip => VisualTreeHelper.GetDpi(this).PixelsPerDip;

        private double CoverHeightPx => coverHalfHeight * 2 * scale;

        private double MaxTextWidth => Math.Min(ActualWidth * 0.38, coverHalfWidth * 2 * scale * 2.2);

        private void UpdateLayoutMetrics()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            EnsureAccentBrushes();

            scale = Math.Min(
                ActualHeight * 0.5 / (coverHalfHeight * 2),
                ActualWidth * 0.28 / (coverHalfWidth * 2));

            double coverHeight = CoverHeightPx;
            double ppd = PixelsPerDip;
            double maxWidth = MaxTextWidth;

            var name = nameText.Get(profileName, nameTypeface, Math.Round(coverHeight * 0.1), nameBrush, ppd, maxWidth, 2);
            var label = labelText.Get("TOTAL PLAYTIME", infoTypeface, Math.Round(coverHeight * 0.035), labelBrush, ppd, maxWidth, 1);
            var playtime = playtimeText.Get(playtimeString, infoTypeface, Math.Round(coverHeight * 0.09), playtimeBrush, ppd, maxWidth, 1);

            textWidth = Math.Max(name.Width, Math.Max(label.Width, playtime.Width));
            textGap = coverHeight * 0.09;
            groupHalfWidth = (coverHalfWidth * 2 * scale + textGap + textWidth) * 0.5;
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        protected override void OnRender(DrawingContext dc)
        {
            if (ActualWidth <= 0 || ActualHeight <= 0 || double.IsNaN(centerX))
            {
                return;
            }

            double coverWidthPx = coverHalfWidth * 2 * scale;
            double coverHeightPx = CoverHeightPx;
            double cx = centerX - groupHalfWidth + coverWidthPx * 0.5;
            double cy = centerY;

            RenderCover(dc, cx, cy);
            RenderDust(dc, cx, cy);
            RenderText(dc, cx + coverWidthPx * 0.5 + textGap, cy, coverHeightPx);
        }

        private void RenderCover(DrawingContext dc, double cx, double cy)
        {
            EnsureCoverBitmap();
            if (coverBitmap == null)
            {
                return;
            }

            Array.Clear(coverBuffer);
            glows.Clear();

            double coverWidth = coverHalfWidth * 2;
            double coverHeight = coverHalfHeight * 2;
            double originX = bitmapWidth * 0.5;
            double originY = bitmapHeight * 0.5;

            // Diagonal light sweep from the top left to the bottom right
            bool sweepActive = sweepAge < SweepDuration;
            double sweepPos = -0.3 + 1.6 * (sweepAge / SweepDuration);

            // Ripple ring growing from a random point of the cover
            bool rippleActive = rippleAge < RippleDuration;
            double rippleRadius = rippleAge / RippleDuration * coverHeight * 1.3;
            double rippleFade = 1 - rippleAge / RippleDuration;
            double rippleWidth = coverHeight * 0.06;

            // Shockwave running from the impacted edge through the cover
            bool impactActive = impactAge < ImpactDuration;
            double impactFront = impactAge / ImpactDuration * 1.3;
            double impactFade = 1 - impactAge / ImpactDuration;

            for (int i = 0; i < particles.Length; i++)
            {
                ref var p = ref particles[i];

                double ox = Math.Sin(time * p.Speed + p.Phase) * 0.12 * cellSize;
                double oy = Math.Cos(time * p.Speed * 0.8 + p.Phase * 1.7) * 0.12 * cellSize;

                double boost = 0;

                if (sweepActive)
                {
                    double u = p.TX / coverWidth + 0.5;
                    double v = p.TY / coverHeight + 0.5;
                    double d = (u * 0.55 + v * 0.45) - sweepPos;
                    if (d > -0.15 && d < 0.15)
                    {
                        boost += Math.Exp(-d * d / 0.003);
                    }
                }

                if (rippleActive)
                {
                    double dx = p.TX - rippleX;
                    double dy = p.TY - rippleY;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    double dd = (dist - rippleRadius) / rippleWidth;

                    if (dd > -3 && dd < 3 && dist > 0.001)
                    {
                        // Push the particles outwards along the ring
                        double wave = Math.Exp(-dd * dd) * rippleFade;
                        ox += dx / dist * wave * 1.4;
                        oy += dy / dist * wave * 1.4;
                        boost += wave * 0.7;
                    }
                }

                if (impactActive)
                {
                    double depth = ImpactDepth(p.TX, p.TY);
                    double dd = (depth - impactFront) / 0.07;

                    if (dd > -3 && dd < 3)
                    {
                        // Compression front, pushes back away from the wall
                        double wave = Math.Exp(-dd * dd) * impactFade * impactFade;
                        ox -= impactDirX * wave * 1.6;
                        oy -= impactDirY * wave * 1.6;
                        boost += wave * 0.9;
                    }

                    // Short flash at the impacted edge
                    if (depth < 0.12)
                    {
                        boost += (1 - depth / 0.12) * Math.Max(0, 1 - impactAge / 0.35) * 0.9;
                    }
                }

                double speedSq = p.VX * p.VX + p.VY * p.VY;
                double motion = speedSq < 0.01 ? 0 : Math.Min(1, Math.Sqrt(speedSq) / 25);
                double shimmer = 0.07 * Math.Sin(time * p.Speed * 1.3 + p.Phase);
                double factor = Math.Clamp(p.Brightness + shimmer + boost * 0.5 + motion * 0.3, MinFactor, MaxFactor);

                int r, g, b;
                if (factor <= 1)
                {
                    r = (int)(p.R * factor);
                    g = (int)(p.G * factor);
                    b = (int)(p.B * factor);
                }
                else
                {
                    double t = factor - 1;
                    r = (int)(p.R + (255 - p.R) * t);
                    g = (int)(p.G + (255 - p.G) * t);
                    b = (int)(p.B + (255 - p.B) * t);
                }

                // Small gaps between the dots keep the particle look
                double radius = (DotRadius + 0.06 * Math.Sin(time * p.Speed + p.Phase * 2) + boost * 0.12 + motion * 0.1) * cellSize * renderScale;
                double px = originX + (p.X + ox) * renderScale;
                double py = originY + (p.Y + oy) * renderScale;

                DrawDot(px, py, radius, r, g, b);

                // Only every third particle glows, otherwise the dense sweep gets too bright
                if (boost > 0.6 && i % 3 == 0)
                {
                    glows.Add(new Glow { X = px, Y = py, Intensity = Math.Min(1, boost - 0.3) * 0.2 });
                }
            }

            foreach (var s in sparks)
            {
                double fadeIn = Math.Min(1, s.Age / 0.3);
                double fadeOut = 1 - s.Age / s.Life;
                double alpha = fadeIn * fadeOut * fadeOut * (0.7 + 0.3 * Math.Sin(time * 6 + s.Phase));
                double radius = DotRadius * cellSize * renderScale * s.Size;

                DrawDot(originX + s.X * renderScale, originY + s.Y * renderScale, radius,
                    (int)(s.R * alpha), (int)(s.G * alpha), (int)(s.B * alpha));
            }

            double glowRadius = 1.4 * renderScale;
            foreach (var glow in glows)
            {
                DrawGlow(glow.X, glow.Y, glowRadius, glow.Intensity);
            }

            coverBitmap.WritePixels(new Int32Rect(0, 0, bitmapWidth, bitmapHeight), coverBuffer, bitmapWidth * 4, 0);

            double width = bitmapWidth / renderScale * scale;
            double height = bitmapHeight / renderScale * scale;
            dc.DrawImage(coverBitmap, new Rect(cx - width * 0.5, cy - height * 0.5, width, height));
        }

        /// <summary>
        /// (Re)creates the bitmap when the size of the cover on screen has changed.
        /// The resolution is limited to MaxCellPixels per cover pixel, larger screens are upscaled.
        /// </summary>
        private void EnsureCoverBitmap()
        {
            double newRenderScale = Math.Min(scale * PixelsPerDip, MaxCellPixels / cellSize);
            int width = Math.Max(1, (int)Math.Ceiling(coverHalfWidth * 2 * BitmapMargin * newRenderScale));
            int height = Math.Max(1, (int)Math.Ceiling(coverHalfHeight * 2 * BitmapMargin * newRenderScale));

            renderScale = newRenderScale;

            if (coverBitmap != null && width == bitmapWidth && height == bitmapHeight)
            {
                return;
            }

            bitmapWidth = width;
            bitmapHeight = height;
            coverBuffer = new int[width * height];
            coverBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
        }

        /// <summary>
        /// Draws an antialiased dot. Overlapping dots keep the brighter value per channel
        /// so the dense cover does not get overexposed.
        /// </summary>
        private void DrawDot(double px, double py, double radius, int r, int g, int b)
        {
            int x0 = Math.Max(0, (int)(px - radius - 0.5));
            int x1 = Math.Min(bitmapWidth - 1, (int)(px + radius + 0.5));
            int y0 = Math.Max(0, (int)(py - radius - 0.5));
            int y1 = Math.Min(bitmapHeight - 1, (int)(py + radius + 0.5));

            if (x0 > x1 || y0 > y1)
            {
                return;
            }

            double outer = radius + 0.5;
            double outerSq = outer * outer;

            for (int y = y0; y <= y1; y++)
            {
                double dy = y + 0.5 - py;
                int row = y * bitmapWidth;

                for (int x = x0; x <= x1; x++)
                {
                    double dx = x + 0.5 - px;
                    double distSq = dx * dx + dy * dy;
                    if (distSq >= outerSq)
                    {
                        continue;
                    }

                    double coverage = outer - Math.Sqrt(distSq);
                    if (coverage > 1)
                    {
                        coverage = 1;
                    }

                    int existing = coverBuffer[row + x];
                    int nr = Math.Max((existing >> 16) & 0xFF, (int)(r * coverage));
                    int ng = Math.Max((existing >> 8) & 0xFF, (int)(g * coverage));
                    int nb = Math.Max(existing & 0xFF, (int)(b * coverage));
                    coverBuffer[row + x] = nr << 16 | ng << 8 | nb;
                }
            }
        }

        /// <summary>
        /// Adds a soft glow (additive blending).
        /// </summary>
        private void DrawGlow(double px, double py, double radius, double intensity)
        {
            int x0 = Math.Max(0, (int)(px - radius));
            int x1 = Math.Min(bitmapWidth - 1, (int)(px + radius));
            int y0 = Math.Max(0, (int)(py - radius));
            int y1 = Math.Min(bitmapHeight - 1, (int)(py + radius));
            double radiusSq = radius * radius;

            for (int y = y0; y <= y1; y++)
            {
                double dy = y + 0.5 - py;
                int row = y * bitmapWidth;

                for (int x = x0; x <= x1; x++)
                {
                    double dx = x + 0.5 - px;
                    double distSq = dx * dx + dy * dy;
                    if (distSq >= radiusSq)
                    {
                        continue;
                    }

                    double falloff = 1 - distSq / radiusSq;
                    double weight = falloff * falloff * intensity;

                    int existing = coverBuffer[row + x];
                    int nr = Math.Min(255, ((existing >> 16) & 0xFF) + (int)(glowColor.R * weight));
                    int ng = Math.Min(255, ((existing >> 8) & 0xFF) + (int)(glowColor.G * weight));
                    int nb = Math.Min(255, (existing & 0xFF) + (int)(glowColor.B * weight));
                    coverBuffer[row + x] = nr << 16 | ng << 8 | nb;
                }
            }
        }

        private void RenderDust(DrawingContext dc, double cx, double cy)
        {
            for (int i = 0; i < dust.Length; i++)
            {
                ref var d = ref dust[i];
                double brightness = 0.08 + 0.12 * (0.5 + 0.5 * Math.Sin(time * 0.9 + d.Phase));
                dc.DrawEllipse(accentBrushes[AccentLevel(brightness)], null, new Point(cx + d.X * scale, cy + d.Y * scale), scale * 0.25, scale * 0.25);
            }
        }

        private void RenderText(DrawingContext dc, double left, double cy, double coverHeight)
        {
            double ppd = PixelsPerDip;
            double maxWidth = MaxTextWidth;

            var name = nameText.Get(profileName, nameTypeface, Math.Round(coverHeight * 0.1), nameBrush, ppd, maxWidth, 2);
            var label = labelText.Get("TOTAL PLAYTIME", infoTypeface, Math.Round(coverHeight * 0.035), labelBrush, ppd, maxWidth, 1);
            var playtime = playtimeText.Get(playtimeString, infoTypeface, Math.Round(coverHeight * 0.09), playtimeBrush, ppd, maxWidth, 1);

            double dividerGap = coverHeight * 0.05;
            double labelGap = coverHeight * 0.012;
            double blockHeight = name.Height + dividerGap * 2 + label.Height + labelGap + playtime.Height;
            double y = cy - blockHeight * 0.5;

            dc.DrawText(name, new Point(left, y));
            y += name.Height + dividerGap;

            // Divider made of shimmering accent dots
            int dotCount = Math.Max(8, (int)(Math.Max(textWidth, coverHeight * 0.5) / (coverHeight * 0.025)));
            double dotStep = Math.Max(textWidth, coverHeight * 0.5) / dotCount;
            double dotRadius = coverHeight * 0.0045;
            for (int i = 0; i < dotCount; i++)
            {
                double fade = 1 - i / (double)dotCount;
                double brightness = (0.25 + 0.3 * (0.5 + 0.5 * Math.Sin(time * 1.6 - i * 0.35))) * fade;
                dc.DrawEllipse(accentBrushes[AccentLevel(brightness)], null, new Point(left + dotRadius + i * dotStep, y), dotRadius, dotRadius);
            }

            y += dividerGap;
            dc.DrawText(label, new Point(left, y));
            y += label.Height + labelGap;
            dc.DrawText(playtime, new Point(left, y));
        }
    }
}
