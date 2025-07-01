using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Learnify.Models
{
    public class UserRanking : INotifyPropertyChanged
    {
        private string _userId;
        private string _displayName;
        private int _points;
        private int _rank;
        private string _avatar;
        private string _name;
        private string _time;
        private string _starIcon;

        public string UserId 
        { 
            get => _userId; 
            set => SetProperty(ref _userId, value); 
        }
        
        public string DisplayName 
        { 
            get => _displayName; 
            set => SetProperty(ref _displayName, value); 
        }
        
        public int Points 
        { 
            get => _points; 
            set => SetProperty(ref _points, value); 
        }
        
        public int Rank 
        { 
            get => _rank; 
            set => SetProperty(ref _rank, value); 
        }
        
        public string Avatar 
        { 
            get => _avatar; 
            set => SetProperty(ref _avatar, value); 
        }
        
        public string Name 
        { 
            get => _name; 
            set => SetProperty(ref _name, value); 
        }
        
        public string Time 
        { 
            get => _time; 
            set => SetProperty(ref _time, value); 
        }
        
        public string StarIcon 
        { 
            get => _starIcon; 
            set => SetProperty(ref _starIcon, value); 
        }

        public UserRanking()
        {
            Avatar = "/Images/avatar1.svg";
            Name = string.Empty;
            Time = string.Empty;
            StarIcon = "/Images/star.svg";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
    }
}
