using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        /// Makes the text font the default font of TextBlock and of the text elements of documents (Run, Paragraph, ...),
        /// which draw all text of the app except text inputs. Called once at startup before the first window is created.
        /// </summary>
        /// <remarks>
        /// The default has to be set on these types themselves: property value inheritance only passes on values that are
        /// set (locally, by a style, ...), not a default value from metadata, so a default on Window doesn't reach its
        /// content. A font set explicitly on an element (icons, monospace) is still inherited by its children.
        /// Control can't be overridden (it registers its own metadata), so a Control keeps Segoe UI as its own value:
        /// styles of controls that draw text with their own FontFamily (TextBox, DatePickerTextBox) or pass it on with a
        /// TemplateBinding set Font.Text, and code that reads the FontFamily of a control uses Text() instead.
        /// </remarks>
        public static void ApplyAsDefault()
        {
            FontFamily font = Text();
            FrameworkPropertyMetadataOptions options = FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender;

            TextElement.FontFamilyProperty.OverrideMetadata(typeof(TextElement), new FrameworkPropertyMetadata(font, options));
            TextBlock.FontFamilyProperty.OverrideMetadata(typeof(TextBlock), new FrameworkPropertyMetadata(font, options));
        }
    }
}
