using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Star rating drawn by the control itself (no template, no bindings). Value runs from 0 (not rated) to
    /// Maximum, spread evenly over StarCount stars: with the defaults (10 / 5) one star is two points, so a click
    /// on the left half of a star gives a half star. Fractional values (e.g. an average) are drawn exactly.
    /// Filled stars use Foreground, empty ones EmptyFill. Clicking the current value again clears the rating.
    /// </summary>
    public class StarRating : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(StarRating),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceValue));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum), typeof(int), typeof(StarRating),
            new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StarCountProperty = DependencyProperty.Register(
            nameof(StarCount), typeof(int), typeof(StarRating),
            new FrameworkPropertyMetadata(5, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StarSizeProperty = DependencyProperty.Register(
            nameof(StarSize), typeof(double), typeof(StarRating),
            new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StarSpacingProperty = DependencyProperty.Register(
            nameof(StarSpacing), typeof(double), typeof(StarRating),
            new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EmptyFillProperty = DependencyProperty.Register(
            nameof(EmptyFill), typeof(Brush), typeof(StarRating),
            new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(StarRating),
            new FrameworkPropertyMetadata(false, OnIsReadOnlyChanged));

        // Value under the mouse while hovering (preview), null otherwise
        private double? _hoverValue;

        static StarRating()
        {
            ForegroundProperty.OverrideMetadata(typeof(StarRating),
                new FrameworkPropertyMetadata(Brushes.Gold, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
            FocusableProperty.OverrideMetadata(typeof(StarRating), new FrameworkPropertyMetadata(true));
        }

        public StarRating()
        {
            Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Current rating, 0 (not rated) to Maximum.
        /// </summary>
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Value of a full rating (all stars filled).
        /// </summary>
        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public int StarCount
        {
            get => (int)GetValue(StarCountProperty);
            set => SetValue(StarCountProperty, value);
        }

        public double StarSize
        {
            get => (double)GetValue(StarSizeProperty);
            set => SetValue(StarSizeProperty, value);
        }

        public double StarSpacing
        {
            get => (double)GetValue(StarSpacingProperty);
            set => SetValue(StarSpacingProperty, value);
        }

        /// <summary>
        /// Brush for the empty part of the stars.
        /// </summary>
        public Brush EmptyFill
        {
            get => (Brush)GetValue(EmptyFillProperty);
            set => SetValue(EmptyFillProperty, value);
        }

        /// <summary>
        /// Display only: no hover preview, no clicks, no keyboard input.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        private double PointsPerStar => StarCount > 0 ? (double)Maximum / StarCount : 1;

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            StarRating rating = (StarRating)d;
            return Math.Clamp((double)baseValue, 0, rating.Maximum);
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StarRating rating = (StarRating)d;
            bool isReadOnly = (bool)e.NewValue;

            rating.Cursor = isReadOnly ? null : Cursors.Hand;
            rating.Focusable = !isReadOnly;
            rating._hoverValue = null;
            rating.InvalidateVisual();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            int count = Math.Max(StarCount, 0);
            double width = count * StarSize + Math.Max(count - 1, 0) * StarSpacing;

            return new Size(width + Padding.Left + Padding.Right, StarSize + Padding.Top + Padding.Bottom);
        }

        protected override void OnRender(DrawingContext dc)
        {
            // Transparent background so the gaps between the stars are hit-testable too
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

            double shownValue = _hoverValue ?? Value;
            double filledStars = shownValue / PointsPerStar;

            for (int i = 0; i < StarCount; i++)
            {
                Rect starRect = GetStarRect(i);
                Geometry star = CreateStarGeometry(starRect);

                dc.DrawGeometry(EmptyFill, null, star);

                double fraction = Math.Clamp(filledStars - i, 0, 1);
                if (fraction <= 0)
                    continue;

                dc.PushClip(new RectangleGeometry(new Rect(starRect.X, starRect.Y, starRect.Width * fraction, starRect.Height)));
                dc.DrawGeometry(Foreground, null, star);
                dc.Pop();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (IsReadOnly)
                return;

            _hoverValue = GetValueAt(e.GetPosition(this));
            InvalidateVisual();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            _hoverValue = null;
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (IsReadOnly)
                return;

            Focus();

            double clicked = GetValueAt(e.GetPosition(this));
            Value = clicked == Value ? 0 : clicked;

            _hoverValue = null;
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (IsReadOnly)
                return;

            switch (e.Key)
            {
                case Key.Right:
                case Key.Up:
                    Value = Math.Floor(Value) + 1;
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Down:
                    Value = Math.Ceiling(Value) - 1;
                    e.Handled = true;
                    break;
                case Key.Delete:
                case Key.Back:
                    Value = 0;
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Rating for a mouse position: the star under the mouse, split into PointsPerStar parts.
        /// Left of the first star gives the smallest value, right of the last star the maximum.
        /// </summary>
        private double GetValueAt(Point position)
        {
            double x = position.X - Padding.Left;
            double starPitch = StarSize + StarSpacing;

            int index = (int)Math.Floor(x / starPitch);
            if (index < 0)
                return 1;
            if (index >= StarCount)
                return Maximum;

            double withinStar = Math.Clamp((x - index * starPitch) / StarSize, 0, 1);
            double part = Math.Max(Math.Ceiling(withinStar * PointsPerStar), 1);

            return Math.Min(index * PointsPerStar + part, Maximum);
        }

        private Rect GetStarRect(int index)
        {
            return new Rect(Padding.Left + index * (StarSize + StarSpacing), Padding.Top, StarSize, StarSize);
        }

        /// <summary>
        /// Five pointed star inside the given square, with slightly rounded joins.
        /// </summary>
        private static Geometry CreateStarGeometry(Rect rect)
        {
            Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height * 0.53);
            double outerRadius = rect.Width / 2;
            double innerRadius = outerRadius * 0.48;

            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                for (int i = 0; i < 10; i++)
                {
                    double radius = i % 2 == 0 ? outerRadius : innerRadius;
                    double angle = Math.PI / 5 * i - Math.PI / 2;
                    Point point = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));

                    if (i == 0)
                        ctx.BeginFigure(point, true, true);
                    else
                        ctx.LineTo(point, true, true);
                }
            }

            geometry.Freeze();
            return geometry;
        }
    }
}
