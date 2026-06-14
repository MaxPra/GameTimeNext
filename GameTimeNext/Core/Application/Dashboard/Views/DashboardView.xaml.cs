using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UIX.ViewController.Engine.FrameworkElements.UserControls;

namespace GameTimeNext.Core.Application.Dashboard.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UIXUserControlBase
    {
        private readonly Dictionary<DependencyObject, double> _baseFontSizes = new();
        private double _currentFontScale = 1.0;

        public DashboardView()
        {
            InitializeComponent();

            Loaded += DashboardView_Loaded;
            SizeChanged += DashboardView_SizeChanged;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyResponsiveFontScale();
        }

        private void DashboardView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveFontScale();
        }

        private void ApplyResponsiveFontScale()
        {
            double targetScale = GetScaleFactor(ActualWidth);

            if (targetScale.Equals(_currentFontScale) && _baseFontSizes.Count > 0)
                return;

            CaptureFontBaselines();

            foreach (var entry in _baseFontSizes)
            {
                if (entry.Key is TextBlock textBlock)
                    textBlock.FontSize = entry.Value * targetScale;
                else if (entry.Key is Control control)
                    control.FontSize = entry.Value * targetScale;
            }

            _currentFontScale = targetScale;
        }

        private void CaptureFontBaselines()
        {
            foreach (DependencyObject element in EnumerateVisualTree(this))
            {
                if (_baseFontSizes.ContainsKey(element))
                    continue;

                if (element is TextBlock textBlock)
                {
                    _baseFontSizes[element] = textBlock.FontSize / _currentFontScale;
                }
                else if (element is Control control)
                {
                    _baseFontSizes[element] = control.FontSize / _currentFontScale;
                }
            }
        }

        private static double GetScaleFactor(double width)
        {
            if (width >= 2200)
                return 1.25;
            if (width >= 1800)
                return 1.16;
            if (width >= 1500)
                return 1.10;
            if (width <= 1100)
                return 0.95;

            return 1.0;
        }

        private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
        {
            if (root == null)
                yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                DependencyObject current = queue.Dequeue();
                yield return current;

                int childCount = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < childCount; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(current, i));
                }
            }
        }
    }
}
