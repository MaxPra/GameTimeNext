// ProfilesCropImageViewController.cs
using GameTimeNext.Core.Application.Profiles.Views;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Controller;
using UIX.ViewController.Engine.Events;
using UIX.ViewController.Engine.FrameworkElements.UserControls;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Profiles.Controller
{
    public class ProfilesCropImageViewController : UIXViewControllerBase<ProfilesCropImageApp, ProfilesCropImageView>
    {
        private const double CROP_RATIO = 600.0 / 900.0;
        private const double MIN_CROP_WIDTH = 90.0;
        private const double INITIAL_CROP_WIDTH_FACTOR = 0.48;

        private bool _isInitialized = false;

        private double _hostWidth = 0.0;
        private double _hostHeight = 0.0;

        private double _displayImageX = 0.0;
        private double _displayImageY = 0.0;
        private double _displayImageWidth = 0.0;
        private double _displayImageHeight = 0.0;

        private double _cropX = 0.0;
        private double _cropY = 0.0;
        private double _cropWidth = 0.0;
        private double _cropHeight = 0.0;

        private double _lastHorizontalChange = 0.0;
        private double _lastVerticalChange = 0.0;

        public ProfilesCropImageViewController(UIXApplication app) : base(app)
        {
        }

        public class ProfilesCropImageViewReturn : UIXViewReturn
        {
            public BitmapImage? CroppedImage { get; set; }
        }

        protected override void Init()
        {
            ViewReturn = new ProfilesCropImageViewReturn();
        }

        protected override void BuildFirstImpl()
        {
            GetView().thbMove.DragDelta += ThbMove_DragDelta;
            GetView().thbTopLeft.DragDelta += ThbTopLeft_DragDelta;
            GetView().thbTopRight.DragDelta += ThbTopRight_DragDelta;
            GetView().thbBottomLeft.DragDelta += ThbBottomLeft_DragDelta;
            GetView().thbBottomRight.DragDelta += ThbBottomRight_DragDelta;
            GetView().grdImageHost.SizeChanged += GrdImageHost_SizeChanged;
            GetView().brdCropArea.SizeChanged += BrdCropArea_SizeChanged;

            LoadSourceImage();
            UpdateLayoutMetrics();
            InitializeCropArea();
            RefreshCropArea();

            _isInitialized = true;
        }

        protected override void BuildImpl()
        {
        }

        protected override void CheckImpl()
        {
        }

        protected override void DataWrapperSelectionChangedImpl(Selector source)
        {
        }

        protected override void Event_Closing()
        {
        }

        protected override void Event_Maximize()
        {
        }

        protected override void Event_Minimize()
        {
        }

        protected override void UnsavedChangesImpl(UIXUnsavedChangesArgs args)
        {
        }

        protected override void FillDBOImpl()
        {
        }

        protected override void FillViewImpl()
        {
        }

        protected override void SaveDBOImpl()
        {
        }

        protected override void TriggeredEvent(FrameworkElement source, string eventName)
        {
            if (source == GetView().thbMove && eventName == UIXEventNames.Thumb.DragDelta)
            {
                HandleMoveDrag();
                return;
            }

            if (source == GetView().thbTopLeft && eventName == UIXEventNames.Thumb.DragDelta)
            {
                HandleResizeTopLeft();
                return;
            }

            if (source == GetView().thbTopRight && eventName == UIXEventNames.Thumb.DragDelta)
            {
                HandleResizeTopRight();
                return;
            }

            if (source == GetView().thbBottomLeft && eventName == UIXEventNames.Thumb.DragDelta)
            {
                HandleResizeBottomLeft();
                return;
            }

            if (source == GetView().thbBottomRight && eventName == UIXEventNames.Thumb.DragDelta)
            {
                HandleResizeBottomRight();
                return;
            }
        }

        private void GrdImageHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isInitialized)
                return;

            double oldImageX = _displayImageX;
            double oldImageY = _displayImageY;
            double oldImageWidth = _displayImageWidth;
            double oldImageHeight = _displayImageHeight;

            UpdateLayoutMetrics();

            if (oldImageWidth <= 0 || oldImageHeight <= 0)
            {
                InitializeCropArea();
                RefreshCropArea();
                return;
            }

            double relativeLeft = (_cropX - oldImageX) / oldImageWidth;
            double relativeTop = (_cropY - oldImageY) / oldImageHeight;
            double relativeWidth = _cropWidth / oldImageWidth;
            double relativeHeight = _cropHeight / oldImageHeight;

            _cropX = _displayImageX + (relativeLeft * _displayImageWidth);
            _cropY = _displayImageY + (relativeTop * _displayImageHeight);
            _cropWidth = relativeWidth * _displayImageWidth;
            _cropHeight = relativeHeight * _displayImageHeight;

            ClampCropRect();
            RefreshCropArea();
        }

        private void BrdCropArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGuideLines();
        }

        private void ThbMove_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _lastHorizontalChange = e.HorizontalChange;
            _lastVerticalChange = e.VerticalChange;

            TriggeredEvent(GetView().thbMove, UIXEventNames.Thumb.DragDelta);
        }

        private void ThbTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _lastHorizontalChange = e.HorizontalChange;
            _lastVerticalChange = e.VerticalChange;

            TriggeredEvent(GetView().thbTopLeft, UIXEventNames.Thumb.DragDelta);
        }

        private void ThbTopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _lastHorizontalChange = e.HorizontalChange;
            _lastVerticalChange = e.VerticalChange;

            TriggeredEvent(GetView().thbTopRight, UIXEventNames.Thumb.DragDelta);
        }

        private void ThbBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _lastHorizontalChange = e.HorizontalChange;
            _lastVerticalChange = e.VerticalChange;

            TriggeredEvent(GetView().thbBottomLeft, UIXEventNames.Thumb.DragDelta);
        }

        private void ThbBottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _lastHorizontalChange = e.HorizontalChange;
            _lastVerticalChange = e.VerticalChange;

            TriggeredEvent(GetView().thbBottomRight, UIXEventNames.Thumb.DragDelta);
        }

        private void LoadSourceImage()
        {
            GetView().imgSource.Source = GetApp().SourceImage;
        }

        private void UpdateLayoutMetrics()
        {
            _hostWidth = GetView().grdImageHost.ActualWidth;
            _hostHeight = GetView().grdImageHost.ActualHeight;

            if (_hostWidth <= 0)
                _hostWidth = GetView().grdImageHost.Width;

            if (_hostHeight <= 0)
                _hostHeight = GetView().grdImageHost.Height;

            GetView().cnvOverlay.Width = _hostWidth;
            GetView().cnvOverlay.Height = _hostHeight;

            if (GetApp().SourceImage == null || GetApp().SourceImage.PixelWidth <= 0 || GetApp().SourceImage.PixelHeight <= 0)
            {
                _displayImageX = 0;
                _displayImageY = 0;
                _displayImageWidth = _hostWidth;
                _displayImageHeight = _hostHeight;
                return;
            }

            double imageRatio = (double)GetApp().SourceImage.PixelWidth / GetApp().SourceImage.PixelHeight;
            double hostRatio = _hostWidth / _hostHeight;

            if (imageRatio > hostRatio)
            {
                _displayImageWidth = _hostWidth;
                _displayImageHeight = _hostWidth / imageRatio;
                _displayImageX = 0;
                _displayImageY = (_hostHeight - _displayImageHeight) / 2.0;
            }
            else
            {
                _displayImageHeight = _hostHeight;
                _displayImageWidth = _hostHeight * imageRatio;
                _displayImageX = (_hostWidth - _displayImageWidth) / 2.0;
                _displayImageY = 0;
            }

            GetView().imgSource.Width = _displayImageWidth;
            GetView().imgSource.Height = _displayImageHeight;
        }

        private void InitializeCropArea()
        {
            if (_displayImageWidth <= 0 || _displayImageHeight <= 0)
                return;

            double cropWidth = _displayImageWidth * INITIAL_CROP_WIDTH_FACTOR;
            double cropHeight = cropWidth / CROP_RATIO;

            if (cropHeight > _displayImageHeight * 0.82)
            {
                cropHeight = _displayImageHeight * 0.82;
                cropWidth = cropHeight * CROP_RATIO;
            }

            if (cropWidth < MIN_CROP_WIDTH)
            {
                cropWidth = MIN_CROP_WIDTH;
                cropHeight = cropWidth / CROP_RATIO;
            }

            _cropWidth = cropWidth;
            _cropHeight = cropHeight;
            _cropX = _displayImageX + ((_displayImageWidth - _cropWidth) / 2.0);
            _cropY = _displayImageY + ((_displayImageHeight - _cropHeight) / 2.0);

            ClampCropRect();
        }

        private void HandleMoveDrag()
        {
            _cropX += _lastHorizontalChange;
            _cropY += _lastVerticalChange;

            ClampCropRect();
            RefreshCropArea();
        }

        private void HandleResizeTopLeft()
        {
            double right = _cropX + _cropWidth;
            double bottom = _cropY + _cropHeight;

            double newWidth = _cropWidth - _lastHorizontalChange;
            ApplyResizeTopLeft(right, bottom, newWidth);
        }

        private void HandleResizeTopRight()
        {
            double left = _cropX;
            double bottom = _cropY + _cropHeight;

            double newWidth = _cropWidth + _lastHorizontalChange;
            ApplyResizeTopRight(left, bottom, newWidth);
        }

        private void HandleResizeBottomLeft()
        {
            double right = _cropX + _cropWidth;
            double top = _cropY;

            double newWidth = _cropWidth - _lastHorizontalChange;
            ApplyResizeBottomLeft(right, top, newWidth);
        }

        private void HandleResizeBottomRight()
        {
            double left = _cropX;
            double top = _cropY;

            double newWidth = _cropWidth + _lastHorizontalChange;
            ApplyResizeBottomRight(left, top, newWidth);
        }

        private void ApplyResizeTopLeft(double right, double bottom, double width)
        {
            width = NormalizeWidth(width, right - _displayImageX, bottom - _displayImageY);

            _cropWidth = width;
            _cropHeight = width / CROP_RATIO;
            _cropX = right - _cropWidth;
            _cropY = bottom - _cropHeight;

            ClampCropRect();
            RefreshCropArea();
        }

        private void ApplyResizeTopRight(double left, double bottom, double width)
        {
            width = NormalizeWidth(width, (_displayImageX + _displayImageWidth) - left, bottom - _displayImageY);

            _cropWidth = width;
            _cropHeight = width / CROP_RATIO;
            _cropX = left;
            _cropY = bottom - _cropHeight;

            ClampCropRect();
            RefreshCropArea();
        }

        private void ApplyResizeBottomLeft(double right, double top, double width)
        {
            width = NormalizeWidth(width, right - _displayImageX, (_displayImageY + _displayImageHeight) - top);

            _cropWidth = width;
            _cropHeight = width / CROP_RATIO;
            _cropX = right - _cropWidth;
            _cropY = top;

            ClampCropRect();
            RefreshCropArea();
        }

        private void ApplyResizeBottomRight(double left, double top, double width)
        {
            width = NormalizeWidth(width, (_displayImageX + _displayImageWidth) - left, (_displayImageY + _displayImageHeight) - top);

            _cropWidth = width;
            _cropHeight = width / CROP_RATIO;
            _cropX = left;
            _cropY = top;

            ClampCropRect();
            RefreshCropArea();
        }

        private double NormalizeWidth(double width, double maxHorizontal, double maxVertical)
        {
            if (width < MIN_CROP_WIDTH)
                width = MIN_CROP_WIDTH;

            double maxWidth = System.Math.Min(maxHorizontal, maxVertical * CROP_RATIO);

            if (width > maxWidth)
                width = maxWidth;

            return width;
        }

        private void ClampCropRect()
        {
            if (_cropWidth < MIN_CROP_WIDTH)
            {
                _cropWidth = MIN_CROP_WIDTH;
                _cropHeight = _cropWidth / CROP_RATIO;
            }

            if (_cropX < _displayImageX)
                _cropX = _displayImageX;

            if (_cropY < _displayImageY)
                _cropY = _displayImageY;

            if (_cropX + _cropWidth > _displayImageX + _displayImageWidth)
                _cropX = (_displayImageX + _displayImageWidth) - _cropWidth;

            if (_cropY + _cropHeight > _displayImageY + _displayImageHeight)
                _cropY = (_displayImageY + _displayImageHeight) - _cropHeight;

            if (_cropX < _displayImageX)
                _cropX = _displayImageX;

            if (_cropY < _displayImageY)
                _cropY = _displayImageY;
        }

        private void RefreshCropArea()
        {
            Canvas.SetLeft(GetView().brdCropArea, _cropX);
            Canvas.SetTop(GetView().brdCropArea, _cropY);

            GetView().brdCropArea.Width = _cropWidth;
            GetView().brdCropArea.Height = _cropHeight;

            UpdateOverlay();
            UpdateGuideLines();
        }

        private void UpdateOverlay()
        {
            RectangleGeometry outerGeometry = new RectangleGeometry(new Rect(0, 0, _hostWidth, _hostHeight));
            RectangleGeometry innerGeometry = new RectangleGeometry(new Rect(_cropX, _cropY, _cropWidth, _cropHeight));

            CombinedGeometry combinedGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, outerGeometry, innerGeometry);
            GetView().pthOverlay.Data = combinedGeometry;
        }

        private void UpdateGuideLines()
        {
            double width = GetView().brdCropArea.Width;
            double height = GetView().brdCropArea.Height;

            double v1 = width / 3.0;
            double v2 = (width / 3.0) * 2.0;
            double h1 = height / 3.0;
            double h2 = (height / 3.0) * 2.0;

            GetView().lnV1.X1 = v1;
            GetView().lnV1.X2 = v1;
            GetView().lnV1.Y1 = 0;
            GetView().lnV1.Y2 = height;

            GetView().lnV2.X1 = v2;
            GetView().lnV2.X2 = v2;
            GetView().lnV2.Y1 = 0;
            GetView().lnV2.Y2 = height;

            GetView().lnH1.X1 = 0;
            GetView().lnH1.X2 = width;
            GetView().lnH1.Y1 = h1;
            GetView().lnH1.Y2 = h1;

            GetView().lnH2.X1 = 0;
            GetView().lnH2.X2 = width;
            GetView().lnH2.Y1 = h2;
            GetView().lnH2.Y2 = h2;
        }

        private BitmapImage? BuildCroppedBitmapImage()
        {
            if (GetApp().SourceImage == null)
                return null;

            if (_displayImageWidth <= 0 || _displayImageHeight <= 0)
                return null;

            double scaleX = (double)GetApp().SourceImage.PixelWidth / _displayImageWidth;
            double scaleY = (double)GetApp().SourceImage.PixelHeight / _displayImageHeight;

            int x = (int)System.Math.Round((_cropX - _displayImageX) * scaleX);
            int y = (int)System.Math.Round((_cropY - _displayImageY) * scaleY);
            int width = (int)System.Math.Round(_cropWidth * scaleX);
            int height = (int)System.Math.Round(_cropHeight * scaleY);

            if (x < 0)
                x = 0;

            if (y < 0)
                y = 0;

            if (x + width > GetApp().SourceImage.PixelWidth)
                width = GetApp().SourceImage.PixelWidth - x;

            if (y + height > GetApp().SourceImage.PixelHeight)
                height = GetApp().SourceImage.PixelHeight - y;

            if (width <= 0 || height <= 0)
                return null;

            CroppedBitmap croppedBitmap = new CroppedBitmap(GetApp().SourceImage, new Int32Rect(x, y, width, height));

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

            using MemoryStream stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        protected void EV_btnApply()
        {
            GetViewReturn<ProfilesCropImageViewReturn>().Canceled = false;
            GetViewReturn<ProfilesCropImageViewReturn>().CroppedImage = BuildCroppedBitmapImage();
            Exit(true);
        }

        protected void EV_btnCancel()
        {
            Exit(true);
        }
    }
}
