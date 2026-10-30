using System.ComponentModel;
using System.Runtime.CompilerServices;
using UIX.ViewController.Engine.Viewmodel;

namespace GameTimeNext.Core.Application.Screensetups.Controller
{
    public class ScreensetupMonitorDataGridRow : UIXDataGridRow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string COMOID { get; set; } = "";
        public string COMONA { get; set; } = "";
        public string CONUMB { get; set; } = "";
        public string COSTAT { get; set; } = "";
        public bool COISCON { get; set; } = false;

        public bool COMOEN { get => _comoen; set => SetProperty(ref _comoen, value); }
        public bool COMOPR { get => _comopr; set => SetProperty(ref _comopr, value); }

        private bool _comoen;
        private bool _comopr;

        private void SetProperty(ref bool backingField, bool value, [CallerMemberName] string? propertyName = null)
        {
            if (backingField == value)
                return;

            backingField = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
