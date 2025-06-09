using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel; // Added this using directive
using System.Runtime.CompilerServices;

namespace Learnify.ViewModels
{
    public interface IViewActivated
    {
        void OnViewActivated();
    }

    public abstract class ViewModelBase : INotifyPropertyChanged, IViewActivated
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public virtual void OnViewActivated()
        {
            // Mặc định không làm gì cả
        }
    }
}
