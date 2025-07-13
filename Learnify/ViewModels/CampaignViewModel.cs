using System;
using System.Windows.Input;
using System.Windows.Threading;
using Learnify.Commands;
using System.Collections.ObjectModel;
using System.Windows;
using Learnify.Models;
using Learnify.Views;
using System.Linq;
using System.Collections.Generic;

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

        public ObservableCollection<Friend> Friends { get; set; } = new ObservableCollection<Friend>();
        public ICommand OpenShareWindowCommand { get; }

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
            OpenShareWindowCommand = new RelayCommand(OpenShareWindow);


            // Khởi tạo bộ đếm thời gian
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            // Tự động load chiến dịch đã lưu (nếu có)
            LoadMyCampaignFromFirebase();

            // Lấy danh sách bạn bè thực tế từ Firebase (gọi sau khi khởi tạo các thành phần khác)
            LoadFriends();
        }

        private async void LoadFriends()
        {
            var firebase = new Learnify.Services.FirebaseService();
            string userId = Learnify.Services.AuthService.GetUserId();
            var friendsList = await firebase.GetFriendsAsync(userId);
            Friends.Clear();
            if (friendsList != null)
            {
                foreach (var f in friendsList)
                    Friends.Add(f);
            }
        }


        // Lưu và tải chiến dịch cá nhân
        private async void LoadMyCampaignFromFirebase()
        {
            var firebase = new Learnify.Services.FirebaseService();
            string userId = Learnify.Services.AuthService.GetUserId();
            var url = $"users/{userId}/myCampaign.json";
            var httpClient = new System.Net.Http.HttpClient();
            var token = Learnify.Services.AuthService.GetToken();
            var fullUrl = $"https://learnify-b5cf3-default-rtdb.asia-southeast1.firebasedatabase.app/{url}?auth={token}";
            var response = await httpClient.GetAsync(fullUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content) && content != "null")
                {
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(content);
                    EventTitle = data.campaignName;
                    string dateIso = data.campaignDate;
                    if (DateTime.TryParse(dateIso, out DateTime parsedDate))
                    {
                        EventDate = parsedDate;
                        EventDateText = parsedDate.ToString("dd/MM/yyyy");
                    }
                    else
                    {
                        EventDateText = dateIso;
                    }
                    IsInputPanelVisible = false;
                    IsCountdownVisible = true;
                    UpdateCountdown();
                    _timer.Start();
                }
            }
        }

        private async void SaveMyCampaignToFirebase()
        {
            var firebase = new Learnify.Services.FirebaseService();
            string userId = Learnify.Services.AuthService.GetUserId();
            var url = $"users/{userId}/myCampaign.json";
            var httpClient = new System.Net.Http.HttpClient();
            var token = Learnify.Services.AuthService.GetToken();
            var fullUrl = $"https://learnify-b5cf3-default-rtdb.asia-southeast1.firebasedatabase.app/{url}?auth={token}";
            var data = new
            {
                campaignName = EventName,
                campaignDate = EventDate?.ToString("yyyy-MM-ddTHH:mm:ss")
            };
            var content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
            await httpClient.PutAsync(fullUrl, content);
        }
        private async void AddEvent()
        {
            if (string.IsNullOrWhiteSpace(EventName) || EventDate == null)
                return;

            EventTitle = EventName;
            EventDateText = EventDate?.ToString("dd/MM/yyyy");
            IsInputPanelVisible = false;
            IsCountdownVisible = true;

            UpdateCountdown();
            _timer.Start();

            // Lưu chiến dịch cá nhân lên Firebase
            await System.Threading.Tasks.Task.Run(() => SaveMyCampaignToFirebase());
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

        private async void ClearCountdown()
        {
            _timer.Stop();
            EventTitle = string.Empty;
            EventDateText = string.Empty;
            Days = Hours = Minutes = Seconds = 0;
            ShowInput();

            // Xóa dữ liệu chiến dịch cá nhân trên Firebase
            await DeleteMyCampaignFromFirebase();
        }

        private async System.Threading.Tasks.Task DeleteMyCampaignFromFirebase()
        {
            string userId = Learnify.Services.AuthService.GetUserId();
            var url = $"users/{userId}/myCampaign.json";
            var httpClient = new System.Net.Http.HttpClient();
            var token = Learnify.Services.AuthService.GetToken();
            var fullUrl = $"https://learnify-b5cf3-default-rtdb.asia-southeast1.firebasedatabase.app/{url}?auth={token}";
            await httpClient.DeleteAsync(fullUrl);
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

        private void OpenShareWindow()
        {
            var win = new ShareCampaignWindow(Friends);
            if (win.ShowDialog() == true)
            {
                // Lấy danh sách bạn bè đã chọn
                var selected = Friends.Where(f => f.IsSelected).ToList();
                if (selected.Count > 0)
                {
                    // Gửi chiến dịch cho các bạn này (lưu lên Firebase)
                    ShareCampaignToFriends(selected);
                }
            }
        }

        private async void ShareCampaignToFriends(List<Friend> selectedFriends)
        {
            var firebase = new Learnify.Services.FirebaseService();
            string userId = Learnify.Services.AuthService.GetUserId();
            string token = Learnify.Services.AuthService.GetToken();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Bạn cần đăng nhập lại để chia sẻ chiến dịch!", "Thiếu xác thực", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Lưu chiến dịch chia sẻ (nếu cần)
            bool result = await firebase.ShareCampaignToFriendsAsync(
                ownerId: userId,
                campaignName: EventTitle,
                campaignDate: EventDate,
                friends: selectedFriends
            );
            // Gửi request và notification cho bạn bè
            bool requestResult = await firebase.SendSharedCampaignRequestsAsync(
                ownerId: userId,
                campaignName: EventTitle,
                campaignDate: EventDate,
                friends: selectedFriends
            );
            if (result && requestResult)
            {
                MessageBox.Show($"Đã chia sẻ chiến dịch cho: {string.Join(", ", selectedFriends.Select(f => f.Name))}", "Chia sẻ thành công");
            }
            else
            {
                MessageBox.Show("Có lỗi khi chia sẻ chiến dịch hoặc gửi lời mời!", "Lỗi");
            }
            foreach (var f in Friends) f.IsSelected = false;
        }
    }
}
