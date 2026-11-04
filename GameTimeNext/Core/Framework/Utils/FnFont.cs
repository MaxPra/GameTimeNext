using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameTimeNext.Core.Framework.Utils
{
    /// <summary>
    /// Text font of the application (resource Font.Text in UI/Values/UIValues.xaml: Manrope from UI/Fonts, embedded in the
    /// assembly, Segoe UI as fallback).
    /// </summary>
    public static class FnFont
    {
        private const string TextResourceKey = "Font.Text";

        private static FontFamily? _text;

        /// <summary>
        /// Font for code that draws or creates text itself (FormattedText, Typeface); XAML uses the resource Font.Text.
        /// </summary>
        public static FontFamily Text()
        {
            return _text ??= System.Windows.Application.Current?.TryFindResource(TextResourceKey) as FontFamily
                ?? new FontFamily("Segoe UI");
        }

        /// <summary>
        /// Makes the text font the default of every window, so all content inherits it (also popups, which inherit from
        /// their owner). Called once at startup before the first window is created.
        /// </summary>
        public static void ApplyAsDefault()
        {
            Control.FontFamilyProperty.OverrideMetadata(typeof(Window),
                new FrameworkPropertyMetadata(Text(), FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
        }
    }
}
