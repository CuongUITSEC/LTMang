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
        public ViewModelCommand TestNotificationCommand { get; set; }

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
        private DateTime _lastFriendsListCheck = DateTime.UtcNow;
        private System.Threading.CancellationTokenSource _friendsPollingCts;

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

            // Kết nối NotificationVM với FirebaseService để xử lý notifications
            _firebaseService.NotificationVM = NotificationVM;

            // Tải danh sách bạn bè động từ Firebase khi khởi động
            _ = ReloadFriendsListAsync();
            
            // Bắt đầu polling cho danh sách bạn bè
            StartFriendsListPolling();

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
            SettingCommand = new ViewModelCommand(o => CurrentChildView = SettingVm);            TestNotificationCommand = new ViewModelCommand(async o => 
            {
                var currentUserId = AuthService.GetUserId();
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Testing notification for user: {currentUserId}");
                    var result = await _firebaseService.TestNotificationAsync(currentUserId);
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Test notification result: {result}");
                }
            });
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
                var currentUserId = AuthService.GetUserId();
                if (string.IsNullOrWhiteSpace(uid)) 
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] UID is empty, showing all friends");
                    FriendsList = new ObservableCollection<Friend>(_allFriends);
                    return;
                }
                
                // Kiểm tra nếu tìm chính mình thì không cho phép
                if (uid == currentUserId)
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Cannot search for yourself, showing empty results");
                    FriendsList = new ObservableCollection<Friend>();
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Searching for user with UID: {uid}");
                // Lấy thông tin user từ Firebase
                var user = await _firebaseService.GetUserByUidAsync(uid);
                if (user != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Found user: {user.Name} ({user.Id})");
                    
                    // Kiểm tra xem có phải là bạn bè không để set trạng thái
                    var isAlreadyFriend = await _firebaseService.AreAlreadyFriendsAsync(currentUserId, uid);
                    
                    if (isAlreadyFriend)
                    {
                        user.Status = FriendStatus.Friends;
                        System.Diagnostics.Debug.WriteLine($"[MainViewModel] User {uid} is already a friend");
                    }
                    else
                    {
                        // Kiểm tra xem đã gửi lời mời chưa
                        var hasPendingRequest = await _firebaseService.HasPendingRequestAsync(currentUserId, uid);
                        if (hasPendingRequest)
                        {
                            user.Status = FriendStatus.Pending;
                            System.Diagnostics.Debug.WriteLine($"[MainViewModel] User {uid} has pending friend request");
                        }
                        else
                        {
                            user.Status = FriendStatus.None;
                            System.Diagnostics.Debug.WriteLine($"[MainViewModel] User {uid} is not a friend, can send request");
                        }
                    }
                    
                    FriendsList = new ObservableCollection<Friend> { user };
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] User {uid} not found in database");
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
        }        // Reload danh sách bạn bè từ Firebase
        public async Task ReloadFriendsListAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Starting ReloadFriendsListAsync for user: {AuthService.GetUserId()}");
                var friends = await _firebaseService.GetFriendsAsync(AuthService.GetUserId());
                if (friends != null && friends.Count > 0)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        _allFriends = new ObservableCollection<Friend>(friends);
                        FriendsList = new ObservableCollection<Friend>(_allFriends);
                        System.Diagnostics.Debug.WriteLine($"[MainViewModel] ReloadFriendsListAsync: Loaded {friends.Count} friends");
                        OnPropertyChanged(nameof(FriendsList));
                    });
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        _allFriends = new ObservableCollection<Friend>();
                        FriendsList = new ObservableCollection<Friend>();
                        System.Diagnostics.Debug.WriteLine("[MainViewModel] ReloadFriendsListAsync: No friends found, cleared list");
                        OnPropertyChanged(nameof(FriendsList));
                    });
                }
                System.Diagnostics.Debug.WriteLine("[MainViewModel] ReloadFriendsListAsync completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] ReloadFriendsListAsync error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] StackTrace: {ex.StackTrace}");
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (_allFriends == null)
                        _allFriends = new ObservableCollection<Friend>();
                    FriendsList = new ObservableCollection<Friend>(_allFriends);
                    OnPropertyChanged(nameof(FriendsList));
                });
            }
        }

        // Force reload danh sách bạn bè và cập nhật timestamp
        public async Task ForceReloadFriendsListAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainViewModel] Force reloading friends list...");
                _lastFriendsListCheck = DateTime.UtcNow; // Reset timestamp
                await ReloadFriendsListAsync();
                System.Diagnostics.Debug.WriteLine("[MainViewModel] Force reload completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] ForceReloadFriendsListAsync error: {ex.Message}");
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
            var infoWindow = new Learnify.Views.FriendInfoWindow(friend, NotificationVM, this);
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

        private void StartFriendsListPolling()
        {
            _friendsPollingCts = new System.Threading.CancellationTokenSource();
            Task.Run(() => PollFriendsListChangesAsync(_friendsPollingCts.Token));
        }

        private void StopFriendsListPolling()
        {
            _friendsPollingCts?.Cancel();
        }

        private async Task PollFriendsListChangesAsync(System.Threading.CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentUserId = AuthService.GetUserId();
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Polling for user: {currentUserId} at {DateTime.UtcNow}");
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        var hasChanged = await _firebaseService.HasFriendsListChangedAsync(currentUserId, _lastFriendsListCheck);
                        if (hasChanged)
                        {
                            System.Diagnostics.Debug.WriteLine("[MainViewModel] Friends list change detected, reloading...");
                            await ReloadFriendsListAsync();
                            _lastFriendsListCheck = DateTime.UtcNow;
                            System.Diagnostics.Debug.WriteLine("[MainViewModel] Friends list reloaded successfully from polling");
                        }
                        // DEPRECATED: CheckUnfriendNotificationsAsync - now handled in SyncFromAcceptedRequestsAsync
                        // await _firebaseService.CheckUnfriendNotificationsAsync(currentUserId);
                        // Kiểm tra notification chấp nhận kết bạn
                        await _firebaseService.CheckFriendAcceptedNotificationsAsync(currentUserId);
                        // Kiểm tra và đồng bộ danh sách bạn bè
                        await _firebaseService.CheckAndSyncFriendsListAsync(currentUserId);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] PollFriendsListChangesAsync error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] StackTrace: {ex.StackTrace}");
                }

                // Poll mỗi 3 giây để phản hồi nhanh hơn
                await Task.Delay(3000, cancellationToken);
            }
        }

        // Phương thức để dừng polling khi đóng ứng dụng
        public void Dispose()
        {
            try
            {
                StopFriendsListPolling();
                NotificationVM?.StopFriendRequestPolling();
                System.Diagnostics.Debug.WriteLine("[MainViewModel] Disposed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Dispose error: {ex.Message}");
            }
        }
    }
}
