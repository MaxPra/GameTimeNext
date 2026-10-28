using System.ComponentModel;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Profiles.Viewmodel
{
    public class ProfilesListBoxItem : UIXListItem, INotifyPropertyChanged
    {
        public BitmapImage? COCOVIM { get; set; }
        public bool COISPLA { get; set; }

        public BitmapImage? COPLFPA { get; set; }

        // Calculated Playtime
        public string COCPLTI { get; set; }

        // Still shrink-wrapped (TFPROFI.IsSealed); turning it off on a shown tile plays the unwrap animation
        private bool _coissea;
        public bool COISSEA
        {
            get => _coissea;
            set
            {
                if (_coissea == value)
                    return;

                _coissea = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(COISSEA)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
