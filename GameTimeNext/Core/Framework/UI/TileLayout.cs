using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Sizes the tiles of a wrapping ItemsControl so each row fills the available width (no leftover gap on the right).
    ///
    /// Usage: TileLayout.AutoSize="True" on the ListBox. It picks the column count closest to the base tile size
    /// (resources TileWidth / TileHeight) and writes the resulting size to the inherited TileLayout.TileWidth / TileHeight,
    /// which the item template binds to. The aspect ratio of the base size is kept.
    /// </summary>
    public static class TileLayout
    {
        private const double DefaultTileWidth = 210;
        private const double DefaultTileHeight = 300;

        /// <summary>Tiles never shrink below this share of the base size; one column less is used instead.</summary>
        private const double MinScale = 0.8;

        public static readonly DependencyProperty TileWidthProperty =
            DependencyProperty.RegisterAttached(
                "TileWidth",
                typeof(double),
                typeof(TileLayout),
                new FrameworkPropertyMetadata(DefaultTileWidth, FrameworkPropertyMetadataOptions.Inherits));

        public static double GetTileWidth(DependencyObject obj)
            => (double)obj.GetValue(TileWidthProperty);

        public static void SetTileWidth(DependencyObject obj, double value)
            => obj.SetValue(TileWidthProperty, value);

        public static readonly DependencyProperty TileHeightProperty =
            DependencyProperty.RegisterAttached(
                "TileHeight",
                typeof(double),
                typeof(TileLayout),
                new FrameworkPropertyMetadata(DefaultTileHeight, FrameworkPropertyMetadataOptions.Inherits));

        public static double GetTileHeight(DependencyObject obj)
            => (double)obj.GetValue(TileHeightProperty);

        public static void SetTileHeight(DependencyObject obj, double value)
            => obj.SetValue(TileHeightProperty, value);

        public static readonly DependencyProperty AutoSizeProperty =
            DependencyProperty.RegisterAttached(
                "AutoSize",
                typeof(bool),
                typeof(TileLayout),
                new PropertyMetadata(false, OnAutoSizeChanged));

        public static bool GetAutoSize(DependencyObject obj)
            => (bool)obj.GetValue(AutoSizeProperty);

        public static void SetAutoSize(DependencyObject obj, bool value)
            => obj.SetValue(AutoSizeProperty, value);

        private static readonly DependencyProperty IsUpdatePendingProperty =
            DependencyProperty.RegisterAttached("IsUpdatePending", typeof(bool), typeof(TileLayout), new PropertyMetadata(false));

        private static void OnAutoSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ItemsControl itemsControl || !(bool)e.NewValue)
                return;

            itemsControl.Loaded += (_, _) =>
            {
                ScrollViewer? scrollViewer = FnVisualTree.FindDescendant<ScrollViewer>(itemsControl);
                if (scrollViewer == null)
                    return;

                // Only the outer width matters; it doesn't depend on the tile size, so this can't feed back
                scrollViewer.SizeChanged += (_, args) =>
                {
                    if (args.WidthChanged)
                        RequestUpdate(itemsControl, scrollViewer);
                };

                // Containers exist only after the items have been generated (initial load, filter)
                itemsControl.ItemContainerGenerator.StatusChanged += (_, _) => RequestUpdate(itemsControl, scrollViewer);

                RequestUpdate(itemsControl, scrollViewer);
            };
        }

        /// <summary>
        /// Coalesces requests and runs after the current layout pass, so measurements are never half-updated.
        /// </summary>
        private static void RequestUpdate(ItemsControl itemsControl, ScrollViewer scrollViewer)
        {
            if ((bool)itemsControl.GetValue(IsUpdatePendingProperty))
                return;

            itemsControl.SetValue(IsUpdatePendingProperty, true);
            itemsControl.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                itemsControl.SetValue(IsUpdatePendingProperty, false);
                UpdateTileSize(itemsControl, scrollViewer);
            });
        }

        private static void UpdateTileSize(ItemsControl itemsControl, ScrollViewer scrollViewer)
        {
            // Always reserve the vertical scrollbar: otherwise bigger tiles -> scrollbar appears -> smaller tiles -> scrollbar disappears -> ...
            double availableWidth = scrollViewer.ActualWidth - GetScrollBarWidth(scrollViewer);
            if (availableWidth <= 0)
                return;

            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(0) is not ListBoxItem container
                || container.Template?.FindName("Root", container) is not FrameworkElement tile
                || container.ActualWidth <= 0 || tile.ActualWidth <= 0)
                return;

            double baseWidth = itemsControl.TryFindResource("TileWidth") is double w ? w : DefaultTileWidth;
            double baseHeight = itemsControl.TryFindResource("TileHeight") is double h ? h : DefaultTileHeight;

            // Everything around the tile itself (template padding for tilt/shadow + item margin), both from the same layout pass
            double chromeWidth = container.ActualWidth + container.Margin.Left + container.Margin.Right - tile.ActualWidth;

            int columns = Math.Max(1, (int)Math.Round(availableWidth / (baseWidth + chromeWidth)));
            double tileWidth = Math.Floor(availableWidth / columns - chromeWidth);

            if (tileWidth < baseWidth * MinScale && columns > 1)
            {
                columns--;
                tileWidth = Math.Floor(availableWidth / columns - chromeWidth);
            }

            tileWidth = Math.Max(tileWidth, 1);

            if (Math.Abs(GetTileWidth(itemsControl) - tileWidth) < 1)
                return;

            SetTileWidth(itemsControl, tileWidth);
            SetTileHeight(itemsControl, Math.Round(tileWidth * baseHeight / baseWidth));
        }

        private static readonly DependencyProperty KnownScrollBarWidthProperty =
            DependencyProperty.RegisterAttached("KnownScrollBarWidth", typeof(double), typeof(TileLayout), new PropertyMetadata(0d));

        /// <summary>
        /// Width of the vertical scrollbar, also while it's hidden. The width is remembered once seen,
        /// so the value doesn't change whenever the scrollbar is shown or hidden.
        /// </summary>
        private static double GetScrollBarWidth(ScrollViewer scrollViewer)
        {
            double knownWidth = (double)scrollViewer.GetValue(KnownScrollBarWidthProperty);

            if (scrollViewer.Template?.FindName("PART_VerticalScrollBar", scrollViewer) is FrameworkElement scrollBar
                && scrollBar.ActualWidth > knownWidth)
            {
                knownWidth = scrollBar.ActualWidth;
                scrollViewer.SetValue(KnownScrollBarWidthProperty, knownWidth);
            }

            return knownWidth > 0 ? knownWidth : SystemParameters.VerticalScrollBarWidth;
        }
    }
}
