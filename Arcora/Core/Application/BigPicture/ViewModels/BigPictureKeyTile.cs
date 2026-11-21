using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.BigPicture.ViewModels
{
    public enum BigPictureKeyKind
    {
        Character,
        Space,
        Backspace,
        Clear,
        Done
    }

    /// <summary>
    /// Key of the on-screen keyboard. The controller places every key on the keyboard canvas (X, Y, Width, Height)
    /// and moves the selection to the nearest key in a direction, so wide keys need no special handling.
    /// </summary>
    public class BigPictureKeyTile : UIXViewModelBase
    {
        private bool _isSelected;

        public BigPictureKeyTile(BigPictureKeyKind kind, string text, string glyph)
        {
            Kind = kind;
            Text = text;
            Glyph = glyph;
        }

        public BigPictureKeyKind Kind { get; }

        /// <summary>Label; for character keys also the typed character.</summary>
        public string Text { get; }

        /// <summary>Segoe Fluent Icons glyph in front of the label, empty for none.</summary>
        public string Glyph { get; }

        public bool HasGlyph => Glyph.Length > 0;

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double Right => X + Width;

        public double Bottom => Y + Height;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
