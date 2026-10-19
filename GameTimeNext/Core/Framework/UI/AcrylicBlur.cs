using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Acrylic blur behind transparent (layered) windows: popups and the splash screen.
    /// The window content draws the translucent tint on top.
    ///
    /// Windows: AcrylicBlur.IsEnabled="True" on a window with AllowsTransparency; styles switch to the translucent
    /// look via the read only AcrylicBlur.IsActive (false without blur, then keep an opaque background).
    /// Popups: see PopupGlass.
    ///
    /// The DWM system backdrop used by GTNWindow does not work for these: popups never get activated
    /// (the backdrop stays on its gray inactive fallback) and layered windows don't get it at all.
    /// </summary>
    public static class AcrylicBlur
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AcrylicBlur),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static readonly DependencyPropertyKey IsActivePropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsActive",
                typeof(bool),
                typeof(AcrylicBlur),
                new PropertyMetadata(false));

        /// <summary>True when the blur behind the window is enabled.</summary>
        public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

        public static bool GetIsActive(DependencyObject obj) => (bool)obj.GetValue(IsActiveProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window || !(bool)e.NewValue)
                return;

            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd != IntPtr.Zero)
            {
                window.SetValue(IsActivePropertyKey, TryEnable(hwnd));
                return;
            }

            // Before the window is shown: enable as soon as the handle exists (still before the first frame)
            window.SourceInitialized += OnSourceInitialized;
        }

        private static void OnSourceInitialized(object? sender, EventArgs e)
        {
            if (sender is not Window window)
                return;

            window.SourceInitialized -= OnSourceInitialized;
            window.SetValue(IsActivePropertyKey, TryEnable(new WindowInteropHelper(window).Handle));
        }

        /// <summary>Rounded window corners (and with them a rounded blur) exist from Windows 11 on.</summary>
        public static bool IsSupported => Environment.OSVersion.Version.Build >= MinBuildRoundedCorners;

        /// <summary>
        /// Enables the blur with rounded corners (system radius, 8px) for the window.
        /// Returns false if it isn't supported or the call failed.
        /// </summary>
        public static bool TryEnable(IntPtr hwnd)
        {
            if (!IsSupported || hwnd == IntPtr.Zero)
                return false;

            try
            {
                var accent = new ACCENT_POLICY
                {
                    AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    // Practically no tint from the system, the content brings its own
                    GradientColor = 0x01000000
                };

                int accentSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentSize);

                try
                {
                    Marshal.StructureToPtr(accent, accentPtr, false);

                    var data = new WINDOWCOMPOSITIONATTRIBDATA
                    {
                        Attribute = WCA_ACCENT_POLICY,
                        Data = accentPtr,
                        SizeOfData = accentSize
                    };

                    if (SetWindowCompositionAttribute(hwnd, ref data) == 0)
                        return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(accentPtr);
                }

                // Rounds the blur to the window shape
                int cornerPreference = DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        // --- Native ---

        private const int MinBuildRoundedCorners = 22000;
        private const int WCA_ACCENT_POLICY = 19;
        private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct ACCENT_POLICY
        {
            public int AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWCOMPOSITIONATTRIBDATA
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
