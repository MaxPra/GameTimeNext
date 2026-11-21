using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.BigPicture.ViewModels
{
    public enum BigPicturePivotKind
    {
        Home,
        Library,
        Search
    }

    /// <summary>
    /// Heading of the pivot bar (home, library, search), switched with LB / RB like on the Xbox 360.
    /// </summary>
    public class BigPicturePivot : UIXViewModelBase
    {
        private bool _isSelected;

        public BigPicturePivot(BigPicturePivotKind kind, string title)
        {
            Kind = kind;
            Title = title;
        }

        public BigPicturePivotKind Kind { get; }

        public string Title { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}
