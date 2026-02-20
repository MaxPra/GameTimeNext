using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameTimeNext.Core.Framework.ViewModelsBase
{
    public class GTNViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField,
                                      T value,
                                      Action<T>? onChanged = null,
                                      [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);

            onChanged?.Invoke(value);
            return true;
        }

    }
}
