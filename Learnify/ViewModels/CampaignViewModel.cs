using System;
using System.Windows.Input;
using System.Windows.Threading;
using Learnify.Commands;

namespace Learnify.ViewModels
{
    public class CampaignViewModel : ViewModelBase
    {
        // Thuộc tính
        private string _eventName;
        public string EventName
        {
            get => _eventName;
            set
            {
                _eventName = value;
                OnPropertyChanged(nameof(EventName));
            }
        }

        private DateTime? _eventDate;
        public DateTime? EventDate
        {
            get => _eventDate;
            set
            {
                _eventDate = value;
                OnPropertyChanged(nameof(EventDate));
                UpdateCountdown();
            }
        }

        private bool _isInputPanelVisible = true;
        public bool IsInputPanelVisible
        {
            get => _isInputPanelVisible;
            set
            {
                _isInputPanelVisible = value;
                OnPropertyChanged(nameof(IsInputPanelVisible));
            }
        }

        private bool _isCountdownVisible;
        public bool IsCountdownVisible
        {
            get => _isCountdownVisible;
            set
            {
                _isCountdownVisible = value;
                OnPropertyChanged(nameof(IsCountdownVisible));
            }
        }

        private string _eventTitle;
        public string EventTitle
        {
            get => _eventTitle;
            set
            {
                _eventTitle = value;
                OnPropertyChanged(nameof(EventTitle));
            }
        }

        private string _eventDateText;
        public string EventDateText
        {
            get => _eventDateText;
            set
            {
                _eventDateText = value;
                OnPropertyChanged(nameof(EventDateText));
            }
        }

        private int _days;
        public int Days
        {
            get => _days;
            set
            {
                _days = value;
                OnPropertyChanged(nameof(Days));
            }
        }

        private int _hours;
        public int Hours
        {
            get => _hours;
            set
            {
                _hours = value;
                OnPropertyChanged(nameof(Hours));
            }
        }

        private int _minutes;
        public int Minutes
        {
            get => _minutes;
            set
            {
                _minutes = value;
                OnPropertyChanged(nameof(Minutes));
            }
        }

        private int _seconds;
        public int Seconds
        {
            get => _seconds;
            set
            {
                _seconds = value;
                OnPropertyChanged(nameof(Seconds));
            }
        }

        // Lệnh
        public ICommand AddEventCommand { get; }
        public ICommand CancelEventCommand { get; }
        public ICommand ShowInputCommand { get; }
        public ICommand ClearCountdownCommand { get; }

        // Bộ đếm thời gian
        private readonly DispatcherTimer _timer;

        public CampaignViewModel()
        {
            // Khởi tạo lệnh
            AddEventCommand = new RelayCommand(AddEvent);
            CancelEventCommand = new RelayCommand(CancelEvent);
            ShowInputCommand = new RelayCommand(ShowInput);
            ClearCountdownCommand = new RelayCommand(ClearCountdown);

            // Khởi tạo bộ đếm thời gian
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        private void AddEvent()
        {
            if (string.IsNullOrWhiteSpace(EventName) || EventDate == null)
                return;

            EventTitle = EventName;
            EventDateText = EventDate?.ToString("dd/MM/yyyy HH:mm");
            IsInputPanelVisible = false;
            IsCountdownVisible = true;

            UpdateCountdown();
            _timer.Start();
        }

        private void CancelEvent()
        {
            EventName = string.Empty;
            EventDate = null;
            IsInputPanelVisible = true;
            IsCountdownVisible = false;
        }

        private void ShowInput()
        {
            IsInputPanelVisible = true;
            IsCountdownVisible = false;
        }

        private void ClearCountdown()
        {
            _timer.Stop();
            EventTitle = string.Empty;
            EventDateText = string.Empty;
            Days = Hours = Minutes = Seconds = 0;
            ShowInput();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (EventDate == null)
                return;

            var timeRemaining = EventDate.Value - DateTime.Now;
            if (timeRemaining.TotalSeconds <= 0)
            {
                ClearCountdown();
                return;
            }

            Days = timeRemaining.Days;
            Hours = timeRemaining.Hours;
            Minutes = timeRemaining.Minutes;
            Seconds = timeRemaining.Seconds;
        }
    }
}
