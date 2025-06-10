using System;
using System.Collections.ObjectModel;
using System.Linq;
using Learnify.Models;
using Learnify.Commands;
using System.Threading.Tasks;
using Learnify.Services;

namespace Learnify.ViewModels
{
    public class MainViewModel : ViewModelBase
    {        // Commands
        public ViewModelCommand HomeCommand { get; set; }
        public ViewModelCommand CalendarCommand { get; set; }
        public ViewModelCommand PomodoroCommand { get; set; }
        public ViewModelCommand RankingCommand { get; set; }
        public ViewModelCommand CampaignCommand { get; set; }
        public ViewModelCommand AnalystCommand { get; set; }
        public ViewModelCommand RewardCommand { get; set; }
        public ViewModelCommand SettingCommand { get; set; }
        public ViewModelCommand SearchCommand { get; set; }

        // Command for toggling notification panel
        private ViewModelCommand _toggleNotificationCommand;
        public ViewModelCommand ToggleNotificationCommand
        {
            get
            {
                if (_toggleNotificationCommand == null)
                {
                    _toggleNotificationCommand = new ViewModelCommand(o =>
                    {
                        IsNotificationVisible = !IsNotificationVisible;
                    });
                }
                return _toggleNotificationCommand;
            }
        }


        // Child ViewModels
        public HomeViewModel HomeVm { get; set; }
        public CalendarViewModel CalendarVm { get; set; }
        public PomodoroViewModel PomodoroVm { get; set; }
        public RankingViewModel RankingVm { get; set; }
        public CampaignViewModel CampaignVm { get; set; }  // Đã sửa từ CampaignViewMoldel
        public AnalystViewModel AnalystVm { get; set; }
        public RewardViewModel RewardVm { get; set; }
        public SettingsViewModel SettingVm { get; set; }

        // Notification ViewModel
        private NotificationViewModel _notificationVM;
        public NotificationViewModel NotificationVM
        {
            get => _notificationVM;
            set
            {
                _notificationVM = value;
                OnPropertyChanged(nameof(NotificationVM));
            }
        }

        // Friends List
        private ObservableCollection<Friend> _friendsList;
        private ObservableCollection<Friend> _allFriends; // Danh sách gốc để filter
        private string _searchText;
        private readonly FirebaseService _firebaseService = new FirebaseService();

        public ObservableCollection<Friend> FriendsList
        {
            get => _friendsList;
            set
            {
                _friendsList = value;
                OnPropertyChanged(nameof(FriendsList));
            }
        }

        // Property quản lý hiển thị Notification Panel
        private bool _isNotificationVisible;
        public bool IsNotificationVisible
        {
            get => _isNotificationVisible;
            set
            {
                _isNotificationVisible = value;
                OnPropertyChanged(nameof(IsNotificationVisible));
            }
        }

        // View hiện tại hiển thị trong MainView
        private ViewModelBase _currentChildView;
        public ViewModelBase CurrentChildView
        {
            get => _currentChildView;
            set
            {
                _currentChildView = value;
                OnPropertyChanged(nameof(CurrentChildView));
            }
        }

        private Friend _selectedFriend;
        public Friend SelectedFriend
        {
            get => _selectedFriend;
            set
            {
                if (_selectedFriend != value)
                {
                    _selectedFriend = value;
                    OnPropertyChanged(nameof(SelectedFriend));
                    if (_selectedFriend != null)
                    {
                        // Hiển thị cửa sổ thông tin bạn bè
                        ShowFriendInfo(_selectedFriend);
                    }
                }
            }        }        public MainViewModel()
        {
            InitializeViewModels();
            InitializeCommands();
            // Không dùng InitializeFriendsList tĩnh nữa

            NotificationVM = new NotificationViewModel(this); // Truyền MainViewModel để có thể reload FriendsList
            NotificationVM.StartFriendRequestPolling(); // Bắt đầu polling khi đăng nhập app
            IsNotificationVisible = true; // Ẩn notification panel khi khởi tạo

            // Tải danh sách bạn bè động từ Firebase khi khởi động
            _ = ReloadFriendsListAsync();

            // ...các logic khác giữ nguyên...
            _ = Task.Run(async () => 
            {
                // Test Firebase permissions trước
                var hasPermission = await _firebaseService.TestFriendsPermissionAsync();
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Firebase friends permission test: {hasPermission}");
                
                // Test lấy danh sách user để kiểm tra dữ liệu có tồn tại không
                var allUserIds = await _firebaseService.GetAllUserIdsAsync();
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Found {allUserIds.Count} users in Firebase");
                
                // Đồng bộ current user sang publicUsers để có thể tìm kiếm
                var currentUserId = AuthService.GetUserId();
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var syncResult = await _firebaseService.SyncUserToPublicAsync(currentUserId);
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Sync current user to public: {syncResult}");
                }
                
                // Đồng bộ tất cả users sang publicUsers (chỉ chạy 1 lần để setup)
                // Uncomment dòng này nếu muốn đồng bộ tất cả users:
                // var syncCount = await _firebaseService.SyncAllUsersToPublicAsync();
                // System.Diagnostics.Debug.WriteLine($"[MainViewModel] Synced {syncCount} users to public");
                
                if (hasPermission)
                {
                    await FixMissingFriendsDataAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Cannot fix friends data - insufficient Firebase permissions");
                }
            });
        }

        private void InitializeViewModels()
        {
            HomeVm = new HomeViewModel();
            CalendarVm = new CalendarViewModel();
            PomodoroVm = new PomodoroViewModel();
            RankingVm = new RankingViewModel();
            CampaignVm = new CampaignViewModel(); // Ensure CampaignViewModel inherits from ViewModelBase
            AnalystVm = new AnalystViewModel();
            RewardVm = new RewardViewModel();
            SettingVm = new SettingsViewModel();
            CurrentChildView = HomeVm;
        }        private void InitializeCommands()
        {
            HomeCommand = new ViewModelCommand(o => CurrentChildView = HomeVm);
            CalendarCommand = new ViewModelCommand(o => CurrentChildView = CalendarVm);
            PomodoroCommand = new ViewModelCommand(o => CurrentChildView = PomodoroVm);
            RankingCommand = new ViewModelCommand(o => 
            {
                CurrentChildView = RankingVm;
                ((RankingViewModel)RankingVm).OnViewActivated();
            });
            CampaignCommand = new ViewModelCommand(o => CurrentChildView = CampaignVm);
            AnalystCommand = new ViewModelCommand(o => CurrentChildView = AnalystVm);
            RewardCommand = new ViewModelCommand(o => CurrentChildView = RewardVm);
            SettingCommand = new ViewModelCommand(o => CurrentChildView = SettingVm);
            // Đã xóa TestSearchCommand khỏi khởi tạo
            // SearchCommand nếu có thể khởi tạo tương tự ở đây
        }

        private void InitializeFriendsList()
        {
            _allFriends = new ObservableCollection<Friend>
            {
                new Friend { Name = "Phuong Tuan", Avatar = "\\Images\\avtPhuongTuan.jpg", IsOnline = true },
                new Friend { Name = "Meo Meo", Avatar = "\\Images\\avtPhuongTuan.jpg", IsOnline = true },
                new Friend { Name = "Trịnh Tổng", Avatar = "\\Images\\avtPhuongTuan.jpg", IsOnline = false },
            };

            FriendsList = new ObservableCollection<Friend>(_allFriends);
        }        public async Task SearchByUidAsync(string uid)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] SearchByUidAsync called with: '{uid}'");
                
                if (string.IsNullOrWhiteSpace(uid)) 
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] UID is empty, showing all friends");
                    FriendsList = new ObservableCollection<Friend>(_allFriends);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Searching for user with UID: {uid}");
                var friend = await _firebaseService.GetUserByUidAsync(uid);
                
                if (friend != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Found friend: {friend.Name} ({friend.Id})");
                    FriendsList = new ObservableCollection<Friend> { friend };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] No friend found");
                    FriendsList = new ObservableCollection<Friend>();
                }
                
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] FriendsList count after search: {FriendsList.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] SearchByUidAsync exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] StackTrace: {ex.StackTrace}");
                FriendsList = new ObservableCollection<Friend>();
            }
        }// Reload danh sách bạn bè từ Firebase
        public async Task ReloadFriendsListAsync()
        {
            try
            {
                // TODO: Thêm hàm GetFriendsAsync vào FirebaseService để lấy danh sách bạn bè thực từ Firebase
                var friends = await _firebaseService.GetFriendsAsync(AuthService.GetUserId());
                if (friends != null && friends.Count > 0)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        _allFriends = new ObservableCollection<Friend>(friends);
                        FriendsList = new ObservableCollection<Friend>(_allFriends);
                        System.Diagnostics.Debug.WriteLine($"[MainViewModel] ReloadFriendsListAsync: Loaded {friends.Count} friends");
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] ReloadFriendsListAsync: No friends found or empty list");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] ReloadFriendsListAsync error: {ex.Message}");
            }
        }        public string SearchText
        {
            get => _searchText;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] SearchText setter called with: '{value}'");
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] SearchText updated to: '{_searchText}', calling SearchByUidAsync");
                    // Gọi tìm kiếm khi thay đổi
                    _ = SearchByUidAsync(_searchText);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] SearchText value unchanged, skipping search");
                }
            }
        }

        private void ShowFriendInfo(Friend friend)
        {
            // Tạo và hiển thị cửa sổ thông tin bạn bè
            var infoWindow = new Learnify.Views.FriendInfoWindow(friend, NotificationVM);
            infoWindow.ShowDialog();
        }

        // Sửa chữa dữ liệu bạn bè bị thiếu
        public async Task FixMissingFriendsDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainViewModel] Starting to fix missing friends data...");
                var fixedCount = await _firebaseService.FixMissingFriendsDataAsync();
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Fixed {fixedCount} missing friends relationships");
                
                // Reload lại danh sách bạn bè sau khi fix
                if (fixedCount > 0)
                {
                    await ReloadFriendsListAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] FixMissingFriendsDataAsync error: {ex.Message}");
            }
        }
    }
}
