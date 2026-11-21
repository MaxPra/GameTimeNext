using System.Windows;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.Utils
{
    /// <summary>
    /// Visual tree search helpers.
    /// </summary>
    internal static class FnVisualTree
    {
        /// <summary>
        /// Returns the first descendant of type <typeparamref name="T"/> (depth-first), or null.
        /// </summary>
        public static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T match)
                    return match;

                if (FindDescendant<T>(child) is T nested)
                    return nested;
            }

            return null;
        }
    }
}
