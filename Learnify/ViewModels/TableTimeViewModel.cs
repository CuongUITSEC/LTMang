using Learnify.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using Learnify.Commands;
using System.Windows;
using Learnify.Services;
using System.Threading.Tasks;

namespace Learnify.ViewModels
{
    public class TableTimeViewModel : INotifyPropertyChanged
    {
        // Danh sách thời khóa biểu hiển thị
        public ObservableCollection<ScheduleItem> ScheduleItems { get; set; } = new ObservableCollection<ScheduleItem>();

        // Dữ liệu để hiển thị trong popup
        // Danh sách ngày trong tuần dạng chữ
        public List<string> DaysOfWeek { get; } = new List<string>
        {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        };
        // Danh sách tiết bắt đầu từ 1
        public List<int> Periods { get; } = Enumerable.Range(1, 6).ToList();
        public List<Brush> AvailableColors { get; } = new List<Brush>
        {
            Brushes.LightBlue,
            Brushes.LightPink,
            Brushes.LightGreen,
            Brushes.LightYellow,
            Brushes.LightCoral,
            new SolidColorBrush(Color.FromRgb(255, 179, 186)), // pastel pink
            new SolidColorBrush(Color.FromRgb(255, 223, 186)), // pastel orange
            new SolidColorBrush(Color.FromRgb(255, 255, 186)), // pastel yellow
            new SolidColorBrush(Color.FromRgb(186, 255, 201)), // pastel green
            new SolidColorBrush(Color.FromRgb(186, 225, 255)), // pastel blue
            new SolidColorBrush(Color.FromRgb(218, 198, 255)), // pastel purple
            new SolidColorBrush(Color.FromRgb(255, 198, 255)), // pastel magenta
            new SolidColorBrush(Color.FromRgb(255, 255, 240)), // pastel ivory
            new SolidColorBrush(Color.FromRgb(255, 210, 221)), // pastel rose
            new SolidColorBrush(Color.FromRgb(202, 231, 255)), // pastel sky
            new SolidColorBrush(Color.FromRgb(255, 236, 179)), // pastel gold
            new SolidColorBrush(Color.FromRgb(204, 255, 229)), // pastel mint
            new SolidColorBrush(Color.FromRgb(255, 204, 229)), // pastel pink 2
            new SolidColorBrush(Color.FromRgb(204, 229, 255)), // pastel blue 2
        };

        private string newItemDay;
        private int newItemPeriod;
        private string newItemSubject = string.Empty;
        private Brush newItemColor;

        public string NewItemDay
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
        // Command xóa tiết học
        public ICommand RemoveScheduleCommand { get; }
        // Command lưu thời khóa biểu
        public ICommand SaveScheduleCommand { get; }

        public TableTimeViewModel()
        {
            // Mặc định chọn ngày, tiết, màu
            NewItemDay = DaysOfWeek[0];
            NewItemPeriod = Periods[0];
            NewItemColor = AvailableColors.First();

            OpenAddPopupCommand = new RelayCommand(() => IsAddPopupOpen = true);
            CancelAddCommand = new RelayCommand(() => IsAddPopupOpen = false);
            AddScheduleCommand = new RelayCommand(AddSchedule, CanAddSchedule);
            // Command xóa tiết học
            RemoveScheduleCommand = new RelayCommand<ScheduleItem>(RemoveSchedule);
            SaveScheduleCommand = new RelayCommand(async () => await SaveScheduleToFirebaseAsync());

            // Tự động tải thời khóa biểu khi khởi tạo ViewModel
            _ = LoadScheduleFromFirebaseAsync();
        }

        private void AddSchedule()
        {
            ScheduleItems.Add(new ScheduleItem
            {
                // Chuyển đổi tên ngày sang số thứ tự cho DayOfWeek (Monday=1, ..., Sunday=0)
                DayOfWeek = DaysOfWeek.IndexOf(NewItemDay),
                Period = NewItemPeriod - 1, // Tiết 1 là 0 trong grid
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

        private void RemoveSchedule(ScheduleItem item)
        {
            if (item != null && ScheduleItems.Contains(item))
            {
                ScheduleItems.Remove(item);
            }
        }

        private async Task SaveScheduleToFirebaseAsync()
        {
            var firebaseService = new FirebaseService();
            string userId = AuthService.GetUserId();
            var scheduleList = ScheduleItems.ToList();
            bool result = await firebaseService.SaveScheduleAsync(userId, scheduleList);
            if (result)
                MessageBox.Show("Lưu thời khóa biểu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Lưu thời khóa biểu thất bại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public async Task LoadScheduleFromFirebaseAsync()
        {
            var firebaseService = new FirebaseService();
            string userId = AuthService.GetUserId();
            var scheduleList = await firebaseService.GetScheduleAsync(userId);
            ScheduleItems.Clear();
            if (scheduleList != null)
            {
                foreach (var item in scheduleList)
                    ScheduleItems.Add(item);
            }
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
