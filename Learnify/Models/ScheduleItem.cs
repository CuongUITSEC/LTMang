using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Learnify.Models
{
    public class ScheduleItem : INotifyPropertyChanged
    {
        private int _period;
        public int Period
        {
            get => _period;
            set
            {
                if (_period != value)
                {
                    _period = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _dayOfWeek;
        public int DayOfWeek
        {
            get => _dayOfWeek;
            set
            {
                if (_dayOfWeek != value)
                {
                    _dayOfWeek = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _subject;
        public string Subject
        {
            get => _subject;
            set
            {
                if (_subject != value)
                {
                    _subject = value;
                    OnPropertyChanged();
                }
            }
        }

        private Brush _color;
        public Brush Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
