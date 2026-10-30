using System.Windows;
using System.Windows.Input;

namespace GameTimeNext.Core.Framework.UserInput
{
    public static class FnKeyboard
    {
        /// <summary>
        /// Simulates a key press in the active window of the application, as if it came from the keyboard:
        /// the focused button reacts to Enter, arrow keys move the focus, Esc cancels. Does nothing while no window
        /// of the application is active (e.g. a game is in the foreground).
        /// </summary>
        public static void PressKeyInActiveWindow(Key key)
        {
            Window? activeWindow = System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

            if (activeWindow == null)
                return;

            PresentationSource? source = PresentationSource.FromVisual(activeWindow);

            if (source == null)
                return;

            KeyEventArgs previewArgs = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent
            };

            InputManager.Current.ProcessInput(previewArgs);

            if (previewArgs.Handled)
                return;

            InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            });
        }
    }
}
