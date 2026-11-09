using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Animations for the items of a list that is not virtualized (e.g. the tag cards).
    /// </summary>
    public static class ItemAnimations
    {
        private static readonly Duration RemoveDuration = new Duration(TimeSpan.FromMilliseconds(240));

        /// <summary>
        /// Plays the removal of the item that contains <paramref name="element"/>: it fades out and shrinks sideways, so the
        /// items behind it glide into the gap. Completes when the animation is done; removing the item is up to the caller.
        /// </summary>
        public static Task PlayRemoveAsync(FrameworkElement element)
        {
            FrameworkElement item = FindContainer(element) ?? element;
            TaskCompletionSource completion = new TaskCompletionSource();

            item.IsHitTestVisible = false;

            ScaleTransform scale = new ScaleTransform(1, 1);
            item.LayoutTransform = scale;
            item.RenderTransformOrigin = new Point(0, 0.5);

            ExponentialEase ease = new ExponentialEase { EasingMode = EasingMode.EaseIn, Exponent = 3 };

            DoubleAnimation fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(180)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation shrink = new DoubleAnimation(0, RemoveDuration) { EasingFunction = ease };
            shrink.Completed += (_, _) => completion.TrySetResult();

            item.BeginAnimation(UIElement.OpacityProperty, fade);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);

            return completion.Task;
        }

        private static FrameworkElement? FindContainer(DependencyObject element)
        {
            DependencyObject? current = element;

            while (current != null)
            {
                if (current is ListBoxItem item)
                    return item;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
