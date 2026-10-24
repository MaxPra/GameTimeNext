using GameTimeNext.Core.Framework.ViewModelsBase;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    /// <summary>
    /// A single checkable entry of the library filter (tag or fixed option)
    /// </summary>
    public class ProfilesFilterItem : GTNViewModelBase
    {
        private bool _isChecked = true;
        private bool _isVisible = true;

        /// <summary>
        /// GRID for tags, 0 for fixed options
        /// </summary>
        public long TagId { get; set; }

        /// <summary>
        /// Option key for fixed options (see ProfilesFilterOptions)
        /// </summary>
        public string OptionKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Special entry that is not a real tag (e.g. "Untagged games"), shown separated from the others
        /// </summary>
        public bool IsSpecial { get; set; }

        public string? ToolTip { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        /// <summary>
        /// False when hidden by the tag search
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
    }
}
