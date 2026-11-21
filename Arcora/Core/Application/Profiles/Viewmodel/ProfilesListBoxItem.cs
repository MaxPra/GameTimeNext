using Arcora.Core.Framework.UI;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Viewmodel;

namespace Arcora.Core.Application.Profiles.Viewmodel
{
    public class ProfilesListBoxItem : UIXListItem, INotifyPropertyChanged
    {
        public BitmapImage? COCOVIM { get; set; }

        // Section of the cover chosen for the disc (TFPROFI.GetDiscSection); null: automatic
        public DiscSection? CODISEC { get; set; }

        public bool COISPLA { get; set; }

        public BitmapImage? COPLFPA { get; set; }

        // Favorite profile (T1PROFI.FAVO): holographic foil on the case
        public bool COISFAV { get; set; }

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

        // Added by the automatic import and not looked at yet (TFPROFI.IsMarkedNew): badge "New" on the tile
        private bool _coisnew;
        public bool COISNEW
        {
            get => _coisnew;
            set
            {
                if (_coisnew == value)
                    return;

                _coisnew = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(COISNEW)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
