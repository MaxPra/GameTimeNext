using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.BigPicture.ViewModels
{
    /// <summary>
    /// Entry of the Big Picture menu or of a question (e.g. "Quit game"). Key identifies the action in the controller.
    /// </summary>
    public class BigPictureMenuOption : UIXViewModelBase
    {
        private bool _isSelected;

        public BigPictureMenuOption(string key, string text, string glyph)
        {
            Key = key;
            Text = text;
            Glyph = glyph;
        }

        public string Key { get; }

        public string Text { get; }

        /// <summary>Segoe Fluent Icons glyph.</summary>
        public string Glyph { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
