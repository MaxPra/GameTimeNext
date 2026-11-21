using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameTimeNext.Core.Framework.UI
{
    internal enum BlackoutTransitionMode
    {
        /// <summary>Black burns in from the center of the screen over the live desktop.</summary>
        In,

        /// <summary>A hole burns open from the center and reveals the desktop again.</summary>
        Out
    }

    /// <summary>
    /// Short-lived, transparent and click-through window that plays the particle transition
    /// when the blackout is turned on or off. The actual blackout window stays opaque
    /// (fast to render), only this window needs per-pixel transparency for about 1.5 seconds.
    /// </summary>
    internal sealed class CFBlackoutTransitionWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private readonly TransitionSurface surface;
        private bool completedRaised;
        private bool surfaceDetached;

        /// <summary>
        /// In mode only: raised once when the screen is completely black.
        /// The surface can now be moved into the blackout window with <see cref="DetachSurface"/>.
        /// </summary>
        public event Action? Covered;

        /// <summary>
        /// Raised once when the transition has finished and the window can be closed.
        /// Not raised anymore once the surface has been detached.
        /// </summary>
        public event Action? Completed;

        /// <summary>
        /// Starts the animation if the window was created with startImmediately = false.
        /// </summary>
        public void Begin()
        {
            surface.Start();
        }

        /// <summary>
        /// In mode only: hands the running surface (black + embers) over to the opaque blackout window,
        /// so the rest of the transition does not need this expensive transparent window anymore.
        /// The window itself stays black until it is closed.
        /// </summary>
        public TransitionSurface DetachSurface()
        {
            surfaceDetached = true;
            Background = Brushes.Black;
            Content = null;
            return surface;
        }

        /// <param name="startImmediately">
        /// false: the window only shows the first frame (Out: fully black) until <see cref="Begin"/> is called.
        /// </param>
        public CFBlackoutTransitionWindow(Window? owner, Rect boundsDip, BlackoutTransitionMode mode, bool startImmediately = true)
        {
            Owner = owner;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Left = boundsDip.Left;
            Top = boundsDip.Top;
            Width = boundsDip.Width;
            Height = boundsDip.Height;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Focusable = false;
            IsHitTestVisible = false;

            surface = new TransitionSurface(mode, boundsDip.Width, boundsDip.Height);
            surface.Finished += RaiseCompleted;
            surface.Covered += () => Covered?.Invoke();
            Content = surface;

            SourceInitialized += (_, _) =>
            {
                // Click-through, never activated and hidden from Alt+Tab
                var hwnd = new WindowInteropHelper(this).Handle;
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            };

            if (startImmediately)
            {
                Loaded += (_, _) => surface.Start();
            }

            Closed += (_, _) =>
            {
                if (!surfaceDetached)
                {
                    surface.Stop();
                }
            };
        }

        private void RaiseCompleted()
        {
            if (completedRaised || surfaceDetached)
            {
                return;
            }

            completedRaised = true;
            Completed?.Invoke();
        }

        /// <summary>
        /// Renders the transition into a low resolution buffer (upscaled with linear filtering),
        /// so the cost stays the same on 1080p and 4K.
        /// </summary>
        internal sealed class TransitionSurface : FrameworkElement
        {
            private const int BufferTargetWidth = 960;
            private const double FrontDuration = 0.95d;   // time until the front has reached the corners
            private const double EmberTail = 0.55d;       // time the last embers need to fade out
            private const double EdgeSoftness = 0.05d;
            private const double EdgeGlowStrength = 0.75d;
            private const double EmberChance = 0.012d;    // chance per buffer pixel to emit an ember when the front passes
            private const int MaxEmbers = 6000;
            private const double ReleaseDuration = 0.9d;  // In: black fades out over the starting blackout content
            private const int GatherEmberCount = 700;
            private const double ReleaseHoldTime = 0.12d;

            private struct Ember
            {
                public double X, Y, VX, VY, Age, Life, Size, Intensity, FadeIn;
            }

            private static readonly Color fallbackAccent = Color.FromRgb(0x34, 0x98, 0xE5);

            private readonly BlackoutTransitionMode mode;
            private readonly int width;
            private readonly int height;
            private readonly float[] thresholds;
            private readonly int[] buffer;
            private readonly WriteableBitmap bitmap;
            private readonly List<Ember> embers = new();
            private readonly Random random = new();

            private readonly int glowR, glowG, glowB;
            private readonly int emberR, emberG, emberB;

            private bool isRunning;
            private bool finished;
            private TimeSpan lastRenderingTime = TimeSpan.Zero;
            private double time;
            private double previousTime = -1;
            private bool coveredRaised;
            private bool releasing;
            private double releaseAge;

            public event Action? Covered;
            public event Action? Finished;

            public TransitionSurface(BlackoutTransitionMode mode, double widthDip, double heightDip)
            {
                this.mode = mode;
                IsHitTestVisible = false;
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Linear);

                width = Math.Max(16, Math.Min(BufferTargetWidth, (int)widthDip));
                height = Math.Max(9, (int)Math.Round(width * heightDip / Math.Max(1, widthDip)));

                buffer = new int[width * height];
                bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                thresholds = BuildThresholds();

                var accent = System.Windows.Application.Current?.TryFindResource("Color.Accent") is Color c ? c : fallbackAccent;
                glowR = accent.R + (255 - accent.R) / 3;
                glowG = accent.G + (255 - accent.G) / 3;
                glowB = accent.B + (255 - accent.B) / 3;
                emberR = accent.R + (255 - accent.R) / 2;
                emberG = accent.G + (255 - accent.G) / 2;
                emberB = accent.B + (255 - accent.B) / 2;

                // First frame: In starts clear, Out starts fully black (no flicker when the blackout window closes)
                RenderFrame();
            }

            /// <summary>
            /// Time at which every buffer pixel is passed by the front: radial from the center,
            /// broken up with two octaves of value noise so the edge looks like burning paper.
            /// </summary>
            private float[] BuildThresholds()
            {
                var coarse = BuildNoiseGrid(12, 7);
                var fine = BuildNoiseGrid(40, 23);
                var result = new float[width * height];

                double cx = width * 0.5;
                double cy = height * 0.5;
                double maxDist = Math.Sqrt(cx * cx + cy * cy);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double dx = x - cx;
                        double dy = y - cy;
                        double r = Math.Sqrt(dx * dx + dy * dy) / maxDist;
                        double u = x / (double)(width - 1);
                        double v = y / (double)(height - 1);
                        double noise = SampleNoise(coarse, 12, 7, u, v) * 0.7 + SampleNoise(fine, 40, 23, u, v) * 0.3;

                        result[y * width + x] = (float)(FrontDuration * (0.78 * r + 0.22 * noise));
                    }
                }

                return result;
            }

            private double[] BuildNoiseGrid(int cols, int rows)
            {
                var grid = new double[(cols + 1) * (rows + 1)];
                for (int i = 0; i < grid.Length; i++)
                {
                    grid[i] = random.NextDouble();
                }

                return grid;
            }

            private static double SampleNoise(double[] grid, int cols, int rows, double u, double v)
            {
                double gx = u * cols;
                double gy = v * rows;
                int x0 = Math.Min(cols - 1, (int)gx);
                int y0 = Math.Min(rows - 1, (int)gy);
                double tx = gx - x0;
                double ty = gy - y0;
                tx = tx * tx * (3 - 2 * tx);
                ty = ty * ty * (3 - 2 * ty);

                int stride = cols + 1;
                double a = grid[y0 * stride + x0];
                double b = grid[y0 * stride + x0 + 1];
                double c = grid[(y0 + 1) * stride + x0];
                double d = grid[(y0 + 1) * stride + x0 + 1];

                return (a + (b - a) * tx) + ((c + (d - c) * tx) - (a + (b - a) * tx)) * ty;
            }

            public void Start()
            {
                if (isRunning || finished)
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

                double dt = Math.Min(0.05, (args.RenderingTime - lastRenderingTime).TotalSeconds);
                lastRenderingTime = args.RenderingTime;

                previousTime = time;
                time += dt;

                if (releasing)
                {
                    releaseAge += dt;
                }

                UpdateEmbers(dt);
                RenderFrame();

                if (mode == BlackoutTransitionMode.Out)
                {
                    if (time >= FrontDuration + EmberTail)
                    {
                        Finish();
                    }

                    return;
                }

                if (!coveredRaised && time >= FrontDuration + EdgeSoftness)
                {
                    coveredRaised = true;
                    Covered?.Invoke();
                }

                // Safety net in case nobody releases the transition
                if ((releasing && releaseAge >= ReleaseDuration) || (!releasing && time >= FrontDuration + 3))
                {
                    Finish();
                }
            }

            private void Finish()
            {
                finished = true;
                Stop();
                Finished?.Invoke();
            }

            public void Release()
            {
                if (releasing || mode != BlackoutTransitionMode.In)
                {
                    return;
                }

                releasing = true;

                // Keep the black for a few more frames: the first frames of the blackout content
                // (bitmap allocation, text layout, JIT) are the most expensive ones and stay hidden
                releaseAge = -ReleaseHoldTime;

                // Existing embers live on until the end and get pulled into the center
                for (int i = 0; i < embers.Count; i++)
                {
                    var e = embers[i];
                    e.Life = e.Age + ReleaseDuration * (0.7 + random.NextDouble() * 0.3);
                    embers[i] = e;
                }

                // Additional embers gather from all over the screen
                for (int i = 0; i < GatherEmberCount && embers.Count < MaxEmbers; i++)
                {
                    double x = random.NextDouble() * width;
                    double y = random.NextDouble() * height;
                    double dx = x - width * 0.5;
                    double dy = y - height * 0.5;
                    double len = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                    double swirl = 40 + random.NextDouble() * 60;

                    embers.Add(new Ember
                    {
                        X = x,
                        Y = y,
                        VX = -dy / len * swirl,
                        VY = dx / len * swirl,
                        Life = ReleaseDuration * (0.6 + random.NextDouble() * 0.4),
                        Size = 0.4 + random.NextDouble() * 0.7,
                        Intensity = 0.3 + random.NextDouble() * 0.4,
                        FadeIn = 0.25
                    });
                }
            }

            private void UpdateEmbers(double dt)
            {
                for (int i = embers.Count - 1; i >= 0; i--)
                {
                    var e = embers[i];
                    e.Age += dt;

                    if (e.Age >= e.Life)
                    {
                        embers[i] = embers[^1];
                        embers.RemoveAt(embers.Count - 1);
                        continue;
                    }

                    if (releasing)
                    {
                        // Pulled into the center (where the blackout content forms) on a slight spiral
                        double dx = width * 0.5 - e.X;
                        double dy = height * 0.5 - e.Y;
                        double dist = Math.Sqrt(dx * dx + dy * dy);

                        if (dist < 10)
                        {
                            embers[i] = embers[^1];
                            embers.RemoveAt(embers.Count - 1);
                            continue;
                        }

                        e.VX += (dx / dist * 1800 - dy / dist * 350) * dt - e.VX * 2.2 * dt;
                        e.VY += (dy / dist * 1800 + dx / dist * 350) * dt - e.VY * 2.2 * dt;
                    }
                    else
                    {
                        // Slight drag and rising like embers from a fire
                        e.VX -= e.VX * 1.2 * dt;
                        e.VY -= e.VY * 1.2 * dt + 60 * dt;
                    }

                    e.X += e.VX * dt;
                    e.Y += e.VY * dt;
                    embers[i] = e;
                }
            }

            private void SpawnEmber(int x, int y)
            {
                if (embers.Count >= MaxEmbers)
                {
                    return;
                }

                // Fly outwards, ahead of the front
                double dx = x - width * 0.5;
                double dy = y - height * 0.5;
                double len = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                double angle = Math.Atan2(dy, dx) + (random.NextDouble() - 0.5) * 1.2;
                double speed = 30 + random.NextDouble() * 110;

                embers.Add(new Ember
                {
                    X = x + 0.5,
                    Y = y + 0.5,
                    VX = Math.Cos(angle) * speed + dx / len * 20,
                    VY = Math.Sin(angle) * speed + dy / len * 20,
                    Life = 0.25 + random.NextDouble() * 0.3,
                    Size = 0.5 + random.NextDouble() * 0.9,
                    Intensity = 0.6 + random.NextDouble() * 0.4
                });
            }

            private void RenderFrame()
            {
                bool spawn = previousTime >= 0 && time <= FrontDuration + 0.05;
                bool modeIn = mode == BlackoutTransitionMode.In;

                // In: after the handover the black fades out and reveals the starting blackout content
                double blackFade = 1;
                if (releasing)
                {
                    double t = Math.Clamp(releaseAge / (ReleaseDuration * 0.75), 0, 1);
                    blackFade = 1 - t * t * (3 - 2 * t);
                }

                for (int i = 0; i < buffer.Length; i++)
                {
                    double d = time - thresholds[i];

                    // Coverage of the black: 0 before the front, 1 behind it
                    double covered;
                    if (d <= 0)
                    {
                        covered = 0;
                    }
                    else if (d >= EdgeSoftness)
                    {
                        covered = 1;
                    }
                    else
                    {
                        double t = d / EdgeSoftness;
                        covered = t * t * (3 - 2 * t);
                    }

                    double alpha = modeIn ? covered * blackFade : 1 - covered;

                    // Glowing seam along the front
                    double glow = 0;
                    if (d > -0.06 && d < 0.14)
                    {
                        double g = (d - 0.02) / 0.045;
                        glow = Math.Exp(-g * g) * EdgeGlowStrength;
                    }

                    if (glow < 0.004)
                    {
                        buffer[i] = alpha >= 1 ? unchecked((int)0xFF000000) : (int)(alpha * 255) << 24;
                    }
                    else
                    {
                        int a = Math.Min(255, (int)((alpha + glow) * 255));
                        int r = Math.Min(a, (int)(glowR * glow));
                        int g = Math.Min(a, (int)(glowG * glow));
                        int b = Math.Min(a, (int)(glowB * glow));
                        buffer[i] = a << 24 | r << 16 | g << 8 | b;
                    }

                    if (spawn && thresholds[i] > previousTime && thresholds[i] <= time && random.NextDouble() < EmberChance)
                    {
                        SpawnEmber(i % width, i / width);
                    }
                }

                double centerX = width * 0.5;
                double centerY = height * 0.5;

                foreach (var e in embers)
                {
                    double fade = 1 - e.Age / e.Life;
                    double intensity = fade * fade * e.Intensity;

                    if (e.FadeIn > 0 && e.Age < e.FadeIn)
                    {
                        intensity *= e.Age / e.FadeIn;
                    }

                    if (releasing)
                    {
                        // Dissolve into the content when getting close to the center
                        double dx = e.X - centerX;
                        double dy = e.Y - centerY;
                        intensity *= Math.Min(1, Math.Sqrt(dx * dx + dy * dy) / 80);
                    }

                    DrawEmber(e.X, e.Y, e.Size, intensity);
                }

                bitmap.WritePixels(new Int32Rect(0, 0, width, height), buffer, width * 4, 0);
            }

            /// <summary>
            /// Additive, premultiplied, antialiased dot.
            /// </summary>
            private void DrawEmber(double px, double py, double radius, double intensity)
            {
                int x0 = Math.Max(0, (int)(px - radius - 1));
                int x1 = Math.Min(width - 1, (int)(px + radius + 1));
                int y0 = Math.Max(0, (int)(py - radius - 1));
                int y1 = Math.Min(height - 1, (int)(py + radius + 1));
                double outer = radius + 0.5;

                for (int y = y0; y <= y1; y++)
                {
                    double dy = y + 0.5 - py;
                    for (int x = x0; x <= x1; x++)
                    {
                        double dx = x + 0.5 - px;
                        double coverage = outer - Math.Sqrt(dx * dx + dy * dy);
                        if (coverage <= 0)
                        {
                            continue;
                        }

                        double w = Math.Min(1, coverage) * intensity;
                        int idx = y * width + x;
                        int existing = buffer[idx];

                        int a = Math.Min(255, ((existing >> 24) & 0xFF) + (int)(255 * w));
                        int r = Math.Min(a, ((existing >> 16) & 0xFF) + (int)(emberR * w));
                        int g = Math.Min(a, ((existing >> 8) & 0xFF) + (int)(emberG * w));
                        int b = Math.Min(a, (existing & 0xFF) + (int)(emberB * w));
                        buffer[idx] = a << 24 | r << 16 | g << 8 | b;
                    }
                }
            }

            protected override void OnRender(DrawingContext dc)
            {
                dc.DrawImage(bitmap, new Rect(RenderSize));
            }
        }
    }
}
