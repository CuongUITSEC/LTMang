using Learnify.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using Learnify.Commands;

namespace Learnify.ViewModels
{
    public class TableTimeViewModel : INotifyPropertyChanged
    {
        // Danh sách thời khóa biểu hiển thị
        public ObservableCollection<ScheduleItem> ScheduleItems { get; set; } = new ObservableCollection<ScheduleItem>();

        // Dữ liệu để hiển thị trong popup
        public List<int> DaysOfWeek { get; } = Enumerable.Range(0, 7).ToList();
        public List<int> Periods { get; } = Enumerable.Range(0, 6).ToList();
        public List<Brush> AvailableColors { get; } = new List<Brush>
        {
            Brushes.LightBlue,
            Brushes.LightPink,
            Brushes.LightGreen,
            Brushes.LightYellow,
            Brushes.LightCoral
        };

        private int newItemDay;
        private int newItemPeriod;
        private string newItemSubject = string.Empty;
        private Brush newItemColor;

        public int NewItemDay
        {
            get => newItemDay;
            set { newItemDay = value; OnPropertyChanged(); }
        }
        public int NewItemPeriod
        {
            get => newItemPeriod;
            set { newItemPeriod = value; OnPropertyChanged(); }
        }
        public string NewItemSubject
        {
            get => newItemSubject;
            set { newItemSubject = value; OnPropertyChanged(); }
        }
        public Brush NewItemColor
        {
            get => newItemColor;
            set { newItemColor = value; OnPropertyChanged(); }
        }

        private bool isAddPopupOpen;

        public bool IsAddPopupOpen
        {
            get => isAddPopupOpen;
            set { isAddPopupOpen = value; OnPropertyChanged(); }
        }

        // Command mở popup
        public ICommand OpenAddPopupCommand { get; }
        // Command hủy popup
        public ICommand CancelAddCommand { get; }
        // Command thêm mới môn học
        public ICommand AddScheduleCommand { get; }

        public TableTimeViewModel()
        {
            // Mặc định chọn ngày, tiết, màu
            NewItemDay = 0;
            NewItemPeriod = 0;
            NewItemColor = AvailableColors.First();

            OpenAddPopupCommand = new RelayCommand(() => IsAddPopupOpen = true);
            CancelAddCommand = new RelayCommand(() => IsAddPopupOpen = false);
            AddScheduleCommand = new RelayCommand(AddSchedule, CanAddSchedule);
        }

        private void AddSchedule()
        {
            ScheduleItems.Add(new ScheduleItem
            {
                DayOfWeek = NewItemDay,
                Period = NewItemPeriod,
                Subject = NewItemSubject,
                Color = NewItemColor
            });

            IsAddPopupOpen = false;
            NewItemSubject = string.Empty;  // Clear textbox
        }

        private bool CanAddSchedule()
        {
            return !string.IsNullOrWhiteSpace(NewItemSubject);
        }

        // Thông báo thay đổi property
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            // Update trạng thái Command nếu liên quan
            if (name == nameof(NewItemSubject))
            {
                (AddScheduleCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }
}
