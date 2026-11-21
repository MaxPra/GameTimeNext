using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Holographic foil effect like on trading cards.
    ///
    /// Usage: HoloShine.IsEnabled="True" on an empty Border placed on top of the surface (same size and corner radius).
    /// The behavior fills the Border with these layers:
    /// - foil: diagonal rainbow bands seen through very fine parallel wave lines (guilloche); the bands drift
    ///   slowly on their own, the waves travel along the lines, the bands slide with the mouse and the foil is
    ///   strongest around the pointer (radial opacity mask),
    /// - glare: soft white light spot and a brighter rim near the pointer, only while hovered.
    /// The Border receives the mouse (transparent background), so it must not cover interactive elements.
    /// </summary>
    public static class HoloShine
    {
        /// <summary>Effect opacity without mouse (barely visible) and while hovered.</summary>
        private const double FoilOpacityIdle = 0.52;
        private const double FoilOpacityHover = 0.8;

        /// <summary>
        /// Length and direction of one rainbow cycle (relative to the bounding box; on a 2:3 surface the bands run
        /// at about 30 degrees, rising to the right) and how far the bands slide over the full mouse range.
        /// </summary>
        private static readonly Vector BandVector = new(0.29, 0.34);
        private const double BandShift = 0.9;

        /// <summary>Time for the bands to drift by one full rainbow cycle on their own.</summary>
        private static readonly Duration BandDriftDuration = new(TimeSpan.FromSeconds(5));

        /// <summary>Without mouse the highlight wanders slowly along these points (relative) and back.</summary>
        private static readonly Point[] IdlePath = { new(0.2, 0.15), new(0.8, 0.4), new(0.3, 0.85) };
        private static readonly TimeSpan IdleSegmentDuration = TimeSpan.FromSeconds(4);

        /// <summary>Wave lines: angle (parallel to the bands), tile in drawing units (one wave period x five lines), travel time per period.</summary>
        private const double WaveAngle = -30;
        private const double WaveTileWidth = 60;
        private const double WaveTileHeight = 30;
        private const double WaveAmplitude = 7;
        private const double WaveLineThickness = 2.6;
        private static readonly Duration WaveTravelDuration = new(TimeSpan.FromSeconds(5));

        /// <summary>Resting position of the foil highlight without mouse (upper left, like light from above).</summary>
        private static readonly Point IdlePosition = new(0.3, 0.2);

        private static readonly Duration FadeInDuration = new(TimeSpan.FromMilliseconds(200));
        private static readonly Duration FadeOutDuration = new(TimeSpan.FromMilliseconds(450));

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(HoloShine),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        /// <summary>Thickness of the glare rim (in the coordinates of the host Border).</summary>
        public static readonly DependencyProperty RimThicknessProperty =
            DependencyProperty.RegisterAttached(
                "RimThickness",
                typeof(double),
                typeof(HoloShine),
                new PropertyMetadata(1.5));

        public static double GetRimThickness(DependencyObject obj)
            => (double)obj.GetValue(RimThicknessProperty);

        public static void SetRimThickness(DependencyObject obj, double value)
            => obj.SetValue(RimThicknessProperty, value);

        /// <summary>Width of one wave period of the line pattern (in the coordinates of the host Border).</summary>
        public static readonly DependencyProperty PatternSizeProperty =
            DependencyProperty.RegisterAttached(
                "PatternSize",
                typeof(double),
                typeof(HoloShine),
                new PropertyMetadata(30d));

        public static double GetPatternSize(DependencyObject obj)
            => (double)obj.GetValue(PatternSizeProperty);

        public static void SetPatternSize(DependencyObject obj, double value)
            => obj.SetValue(PatternSizeProperty, value);

        private static readonly DependencyProperty LayersProperty =
            DependencyProperty.RegisterAttached("Layers", typeof(HoloLayers), typeof(HoloShine), new PropertyMetadata(null));

        private sealed class HoloLayers
        {
            public required Border Foil { get; init; }
            public required LinearGradientBrush Bands { get; init; }
            public required TranslateTransform BandDrift { get; init; }
            public required RadialGradientBrush FoilMask { get; init; }
            public required TranslateTransform WaveTravel { get; init; }
            public required double WavePeriod { get; init; }
            public required Border Glare { get; init; }
            public required RadialGradientBrush GlareSpot { get; init; }
            public required RadialGradientBrush GlareRim { get; init; }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Border host)
                return;

            if ((bool)e.NewValue)
            {
                HoloLayers layers = CreateLayers(host);
                host.SetValue(LayersProperty, layers);

                // Transparent background: the host receives the mouse
                host.Background = Brushes.Transparent;

                var grid = new Grid { IsHitTestVisible = false };
                grid.Children.Add(layers.Foil);
                grid.Children.Add(layers.Glare);
                host.Child = grid;

                UpdatePosition(layers, IdlePosition);

                host.MouseEnter += OnMouseEnter;
                host.MouseMove += OnMouseMove;
                host.MouseLeave += OnMouseLeave;
                host.SizeChanged += OnSizeChanged;
                host.Loaded += OnLoaded;
                host.Unloaded += OnUnloaded;

                if (host.IsLoaded)
                    StartIdleAnimations(layers);
            }
            else
            {
                host.MouseEnter -= OnMouseEnter;
                host.MouseMove -= OnMouseMove;
                host.MouseLeave -= OnMouseLeave;
                host.SizeChanged -= OnSizeChanged;
                host.Loaded -= OnLoaded;
                host.Unloaded -= OnUnloaded;

                if (host.GetValue(LayersProperty) is HoloLayers layers)
                    StopIdleAnimations(layers);

                host.Child = null;
                host.ClearValue(Border.BackgroundProperty);
                host.ClearValue(LayersProperty);
            }
        }

        private static HoloLayers CreateLayers(Border host)
        {
            double patternSize = GetPatternSize(host);

            // Saturated rainbow; first and last stop are equal so the repeated bands join seamlessly
            var bandDrift = new TranslateTransform();
            var bands = new LinearGradientBrush
            {
                SpreadMethod = GradientSpreadMethod.Repeat,
                RelativeTransform = bandDrift,
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(0xFF, 0x5A, 0x8C), 0),
                    new GradientStop(Color.FromRgb(0xFF, 0xB8, 0x4D), 0.17),
                    new GradientStop(Color.FromRgb(0xD8, 0xF0, 0x4A), 0.33),
                    new GradientStop(Color.FromRgb(0x3C, 0xE0, 0x7A), 0.5),
                    new GradientStop(Color.FromRgb(0x3C, 0xC8, 0xFF), 0.67),
                    new GradientStop(Color.FromRgb(0xA0, 0x6C, 0xFF), 0.83),
                    new GradientStop(Color.FromRgb(0xFF, 0x5A, 0x8C), 1)
                }
            };

            // Foil is strongest around the pointer and stays faintly visible everywhere else
            var foilMask = new RadialGradientBrush
            {
                RadiusX = 0.7,
                RadiusY = 0.7,
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0xFF, 0, 0, 0), 0),
                    new GradientStop(Color.FromArgb(0x90, 0, 0, 0), 0.5),
                    new GradientStop(Color.FromArgb(0x50, 0, 0, 0), 1)
                }
            };

            var waveTravel = new TranslateTransform();

            var waves = new Border
            {
                Background = bands,
                OpacityMask = CreateWaveMask(patternSize, waveTravel)
            };

            var foil = new Border
            {
                Child = waves,
                OpacityMask = foilMask,
                Opacity = FoilOpacityIdle
            };

            var glareSpot = new RadialGradientBrush
            {
                RadiusX = 0.45,
                RadiusY = 0.45,
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF), 0),
                    new GradientStop(Color.FromArgb(0x0C, 0xFF, 0xFF, 0xFF), 0.4),
                    new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1)
                }
            };

            var glareRim = new RadialGradientBrush
            {
                RadiusX = 0.55,
                RadiusY = 0.55,
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF), 0),
                    new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 1)
                }
            };

            var glare = new Border
            {
                Background = glareSpot,
                BorderBrush = glareRim,
                BorderThickness = new Thickness(GetRimThickness(host)),
                CornerRadius = host.CornerRadius,
                Opacity = 0
            };

            return new HoloLayers
            {
                Foil = foil,
                Bands = bands,
                BandDrift = bandDrift,
                FoilMask = foilMask,
                WaveTravel = waveTravel,
                WavePeriod = patternSize,
                Glare = glare,
                GlareSpot = glareSpot,
                GlareRim = glareRim
            };
        }

        /// <summary>
        /// Very fine parallel sine lines (guilloche): the lines show the rainbow, the gaps only faintly.
        /// One tile is one wave period wide and five lines high; the lines on the top and bottom edge are both drawn,
        /// so the parts cut off at the tile border are completed by the neighbor tile.
        /// The brush is rotated parallel to the bands; waveTravel moves the waves along the lines.
        /// </summary>
        private static DrawingBrush CreateWaveMask(double patternSize, TranslateTransform waveTravel)
        {
            const double lineSpacing = WaveTileHeight / 5;
            const double control = WaveAmplitude * 4 / 3;
            const double half = WaveTileWidth / 2;

            var drawing = new DrawingGroup();

            // Very faint base in the gaps (keeps the lines distinct); also fixes the tile bounds
            drawing.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromArgb(0x14, 0, 0, 0)),
                null,
                new RectangleGeometry(new Rect(0, 0, WaveTileWidth, WaveTileHeight))));

            var lines = new StreamGeometry();
            using (StreamGeometryContext context = lines.Open())
            {
                for (double y = 0; y <= WaveTileHeight; y += lineSpacing)
                {
                    context.BeginFigure(new Point(0, y), false, false);
                    context.BezierTo(new Point(half / 3, y - control), new Point(half * 2 / 3, y - control), new Point(half, y), true, true);
                    context.BezierTo(new Point(half * 4 / 3, y + control), new Point(half * 5 / 3, y + control), new Point(WaveTileWidth, y), true, true);
                }
            }
            lines.Freeze();

            drawing.Children.Add(new GeometryDrawing(null, new Pen(Brushes.Black, WaveLineThickness), lines));
            drawing.Freeze();

            double scale = patternSize / WaveTileWidth;

            var transform = new TransformGroup();
            transform.Children.Add(waveTravel);
            transform.Children.Add(new RotateTransform(WaveAngle));

            return new DrawingBrush(drawing)
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.Fill,
                Viewbox = new Rect(0, 0, WaveTileWidth, WaveTileHeight),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, WaveTileWidth * scale, WaveTileHeight * scale),
                ViewportUnits = BrushMappingMode.Absolute,
                Transform = transform
            };
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border host && host.GetValue(LayersProperty) is HoloLayers layers)
                StartIdleAnimations(layers);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border host && host.GetValue(LayersProperty) is HoloLayers layers)
                StopIdleAnimations(layers);
        }

        /// <summary>
        /// Constant slight motion: the bands drift by exactly one rainbow cycle and the waves travel by exactly
        /// one wave period per loop, so both loops are seamless.
        /// </summary>
        private static void StartIdleAnimations(HoloLayers layers)
        {
            layers.BandDrift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, BandVector.X, BandDriftDuration) { RepeatBehavior = RepeatBehavior.Forever });
            layers.BandDrift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, BandVector.Y, BandDriftDuration) { RepeatBehavior = RepeatBehavior.Forever });

            layers.WaveTravel.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, layers.WavePeriod, WaveTravelDuration) { RepeatBehavior = RepeatBehavior.Forever });

            StartIdleWander(layers);
        }

        /// <summary>Lets the foil highlight wander slowly over the surface while the mouse is not over it.</summary>
        private static void StartIdleWander(HoloLayers layers)
        {
            var animation = new PointAnimationUsingKeyFrames
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            for (int i = 0; i < IdlePath.Length; i++)
            {
                animation.KeyFrames.Add(new EasingPointKeyFrame(
                    IdlePath[i],
                    KeyTime.FromTimeSpan(IdleSegmentDuration * i),
                    new SineEase { EasingMode = EasingMode.EaseInOut }));
            }

            layers.FoilMask.BeginAnimation(RadialGradientBrush.CenterProperty, animation);
            layers.FoilMask.BeginAnimation(RadialGradientBrush.GradientOriginProperty, animation);
        }

        private static void StopIdleWander(HoloLayers layers)
        {
            layers.FoilMask.BeginAnimation(RadialGradientBrush.CenterProperty, null);
            layers.FoilMask.BeginAnimation(RadialGradientBrush.GradientOriginProperty, null);
        }

        private static void StopIdleAnimations(HoloLayers layers)
        {
            layers.BandDrift.BeginAnimation(TranslateTransform.XProperty, null);
            layers.BandDrift.BeginAnimation(TranslateTransform.YProperty, null);

            layers.WaveTravel.BeginAnimation(TranslateTransform.XProperty, null);

            StopIdleWander(layers);
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not Border host || host.GetValue(LayersProperty) is not HoloLayers layers)
                return;

            if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
                return;

            // Radii are relative to the bounding box; scale Y so the highlights stay circular
            double aspect = e.NewSize.Width / e.NewSize.Height;

            layers.FoilMask.RadiusY = layers.FoilMask.RadiusX * aspect;
            layers.GlareSpot.RadiusY = layers.GlareSpot.RadiusX * aspect;
            layers.GlareRim.RadiusY = layers.GlareRim.RadiusX * aspect;

            // The pattern is drawn on a plain rectangle: clip it to the rounded corners of the host
            double radius = host.CornerRadius.TopLeft;
            layers.Foil.Clip = new RectangleGeometry(new Rect(e.NewSize), radius, radius);
        }

        private static void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Border host || host.GetValue(LayersProperty) is not HoloLayers layers)
                return;

            StopIdleWander(layers);
            UpdatePosition(layers, GetRelativePosition(host, e));
            Fade(layers.Foil, FoilOpacityHover, FadeInDuration);
            Fade(layers.Glare, 1, FadeInDuration);
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Border host && host.GetValue(LayersProperty) is HoloLayers layers)
                UpdatePosition(layers, GetRelativePosition(host, e));
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Border host || host.GetValue(LayersProperty) is not HoloLayers layers)
                return;

            Fade(layers.Foil, FoilOpacityIdle, FadeOutDuration);
            Fade(layers.Glare, 0, FadeOutDuration);

            if (host.IsLoaded)
                StartIdleWander(layers);
        }

        private static Point GetRelativePosition(Border host, MouseEventArgs e)
        {
            if (host.ActualWidth <= 0 || host.ActualHeight <= 0)
                return IdlePosition;

            Point position = e.GetPosition(host);
            return new Point(position.X / host.ActualWidth, position.Y / host.ActualHeight);
        }

        private static void UpdatePosition(HoloLayers layers, Point relative)
        {
            // Slide the bands along their direction: horizontal and vertical movement both shift the colors
            double shift = ((relative.X - 0.5) + (relative.Y - 0.5)) * BandShift;
            Point start = new(shift * BandVector.X, shift * BandVector.Y);

            layers.Bands.StartPoint = start;
            layers.Bands.EndPoint = start + BandVector;

            layers.FoilMask.Center = relative;
            layers.FoilMask.GradientOrigin = relative;

            layers.GlareSpot.Center = relative;
            layers.GlareSpot.GradientOrigin = relative;

            layers.GlareRim.Center = relative;
            layers.GlareRim.GradientOrigin = relative;
        }

        private static void Fade(UIElement element, double to, Duration duration)
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
}
