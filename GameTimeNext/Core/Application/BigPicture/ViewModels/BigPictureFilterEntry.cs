using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.BigPicture.ViewModels
{
    public enum BigPictureFilterEntryKind
    {
        Header,
        Option,
        Tag,
        Untagged,
        ShowArchived,
        OnlyRecentlyPlayed
    }

    /// <summary>
    /// Line of the Big Picture filter: a section header or a switch. Like in the profiles filter, a checked option or
    /// tag is shown; ShowArchived / OnlyRecentlyPlayed are checked when they are switched on.
    /// </summary>
    public class BigPictureFilterEntry : UIXViewModelBase
    {
        private bool _isChecked;
        private bool _isSelected;

        public BigPictureFilterEntry(BigPictureFilterEntryKind kind, string text, bool isChecked = false, string optionKey = "", long tagId = 0)
        {
            Kind = kind;
            Text = text;
            OptionKey = optionKey;
            TagId = tagId;
            _isChecked = isChecked;
        }

        public BigPictureFilterEntryKind Kind { get; }

        public string Text { get; }

        /// <summary>Key of ProfilesFilterOptions for Kind Option.</summary>
        public string OptionKey { get; }

        /// <summary>GRID of the tag for Kind Tag.</summary>
        public long TagId { get; }

        public bool IsHeader => Kind == BigPictureFilterEntryKind.Header;

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
