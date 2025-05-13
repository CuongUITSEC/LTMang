using System;
using System.Collections.ObjectModel;
using System.Linq;
using Learnify.Models;
using Learnify.Commands;

namespace Learnify.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Commands
        public ViewModelCommand HomeCommand { get; set; }
        public ViewModelCommand CalendarCommand { get; set; }
        public ViewModelCommand PomodoroCommand { get; set; }
        public ViewModelCommand RankingCommand { get; set; }
        public ViewModelCommand CampaignCommand { get; set; }
        public ViewModelCommand AnalystCommand { get; set; }
        public ViewModelCommand RewardCommand { get; set; }
        public ViewModelCommand SettingCommand { get; set; }
        public ViewModelCommand SearchCommand { get; set; }

        // Child ViewModels
        public HomeViewModel HomeVm { get; set; }
        public CalendarViewModel CalendarVm { get; set; }
        public PomodoroViewModel PomodoroVm { get; set; }
        public RankingViewModel RankingVm { get; set; }
        public CampaignViewModel CampaignVm { get; set; }  // Đã sửa từ CampaignViewMoldel
        public AnalystViewModel AnalystVm { get; set; }
        public RewardViewModel RewardVm { get; set; }
        public SettingsViewModel SettingVm { get; set; }

        // Friends List
        private ObservableCollection<Friend> _friendsList;
        private ObservableCollection<Friend> _allFriends; // Danh sách gốc để filter
        private string _searchText;

        public ObservableCollection<Friend> FriendsList
        {
            get => _friendsList;
            set
            {
                _friendsList = value;
                OnPropertyChanged(nameof(FriendsList));
            }
        }


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

        public MainViewModel()
        {
            InitializeViewModels();
            InitializeCommands();
            InitializeFriendsList();
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
        }


        private void InitializeCommands()
        {
            HomeCommand = new ViewModelCommand(o => CurrentChildView = HomeVm);
            CalendarCommand = new ViewModelCommand(o => CurrentChildView = CalendarVm);
            PomodoroCommand = new ViewModelCommand(o => CurrentChildView = PomodoroVm);
            RankingCommand = new ViewModelCommand(o => CurrentChildView = RankingVm);
            CampaignCommand = new ViewModelCommand(o => CurrentChildView = CampaignVm);
            AnalystCommand = new ViewModelCommand(o => CurrentChildView = AnalystVm);
            RewardCommand = new ViewModelCommand(o => CurrentChildView = RewardVm);
            SettingCommand = new ViewModelCommand(o => CurrentChildView = SettingVm);
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
        }
    }
}