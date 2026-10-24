using System.Windows;
using System.Windows.Media;
using UIX.ViewController.Engine.FrameworkElements.UserControls;

namespace GameTimeNext.Core.Application.Profiles.Views
{
    /// <summary>
    /// Interaction logic for ProfileDetailSubView.xaml
    /// </summary>
    public partial class ProfilesDetailView : UIXUserControlBase
    {
        private const double CoverAspectRatio = 2d / 3d;

        /// <summary>Share of the panel width the header cover takes (the rest is title + total playtime).</summary>
        private const double CoverWidthShare = 0.36;

        public ProfilesDetailView()
        {
            InitializeComponent();

            svDetail.SizeChanged += (_, args) =>
            {
                if (args.WidthChanged)
                    UpdateCoverSize();
            };
        }

        /// <summary>
        /// The header cover grows with the panel (bigger on large screens), within ProfileDetail.HeaderCoverMin/MaxWidth.
        /// Only depends on the width, so it can't feed back into the layout.
        /// </summary>
        private void UpdateCoverSize()
        {
            double minWidth = TryFindResource("ProfileDetail.HeaderCoverMinWidth") is double min ? min : 96;
            double maxWidth = TryFindResource("ProfileDetail.HeaderCoverMaxWidth") is double max ? max : 150;

            double width = Math.Floor(Math.Clamp(svDetail.ActualWidth * CoverWidthShare, minWidth, maxWidth));
            double height = Math.Floor(width / CoverAspectRatio);

            if (Math.Abs(bdCoverFrame.Width - width) < 0.5 && Math.Abs(bdCoverFrame.Height - height) < 0.5)
                return;

            bdCoverFrame.Width = width;
            bdCoverFrame.Height = height;

            UpdateCoverClip(width, height);
        }

        private void UpdateCoverClip(double coverWidth, double coverHeight)
        {
            Thickness border = bdCoverFrame.BorderThickness;
            double width = Math.Max(0, coverWidth - border.Left - border.Right);
            double height = Math.Max(0, coverHeight - border.Top - border.Bottom);
            double radius = Math.Max(0, bdCoverFrame.CornerRadius.TopLeft - border.Left);

            imgProfileCover.Clip = new RectangleGeometry(new Rect(0, 0, width, height), radius, radius);
        }
    }
}
