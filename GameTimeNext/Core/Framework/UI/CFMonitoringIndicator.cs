using GameTimeNext.Core.Application.TimeMonitoring.Views;
using GameTimeNext.Core.Framework.Utils;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GameTimeNext.Core.Framework.UI
{
    internal static class CFMonitoringIndicator
    {
        private static MonitoringIndicator? indicatorWindow = null;
        private static bool isActive = false;

        public static void ToggleIndicator(Window owner)
        {
            if (isActive)
                Disable(owner);
            else
                Enable(owner);
        }

        private static void Enable(Window owner)
        {
            if (isActive) return;

            FnDisplay.MonitorInfoData monitor = FnDisplay.GetAllMonitors().Where(monitor => monitor.IsPrimary).First();
            indicatorWindow = BuildWindow(owner, monitor);
            indicatorWindow.Show();

            isActive = true;
        }

        private static void Disable(Window owner)
        {
            if (!isActive) return;

            Dispatcher dispatcher = owner.Dispatcher;
            if (dispatcher.CheckAccess())
                CloseWindow();
            else
                dispatcher.Invoke(CloseWindow);
                
            isActive = false;
        }

        private static MonitoringIndicator BuildWindow(Window owner, FnDisplay.MonitorInfoData targetMonitor)
        {
            MonitoringIndicator window = new MonitoringIndicator
            {
                Owner = owner,
                Left = targetMonitor.Bounds.Left,
                Top = targetMonitor.Bounds.Top,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Cursor = Cursors.None
            };

            window.PreviewKeyDown += Window_DisableInteractionEvents;
            window.PreviewKeyUp += Window_DisableInteractionEvents;
            window.PreviewMouseDown += Window_DisableInteractionEvents;
            window.PreviewMouseUp += Window_DisableInteractionEvents;
            window.PreviewMouseMove += Window_DisableInteractionEvents;

            return window;
        }

        private static void Window_DisableInteractionEvents(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private static void CloseWindow()
        {
            try
            {
                indicatorWindow?.Close();
            }
            catch { }

            indicatorWindow = null;
        }
    }
}
