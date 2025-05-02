using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.ViewModels
{
	//public HomeViewModel HomeVm { get; set; }


	public class MainViewModel : ViewModelBase
	{
        public ViewModelCommand HomeCommand { get; set; }
        public ViewModelCommand CalendarCommand { get; set; }
        public ViewModelCommand PomodoroCommand { get; set; }
        public ViewModelCommand RankingCommand { get; set; }
        public ViewModelCommand CampaignCommand { get; set; }
        public ViewModelCommand AnalystCommand { get; set; }
        public ViewModelCommand RewardCommand { get; set; }
        public ViewModelCommand SettingCommand { get; set; }

        public HomeViewModel HomeVm { get; set; }
        public CalendarViewModel CalendarVm { get; set; }
        public PomodoroViewModel PomodoroVm { get; set; }
        public RankingViewModel RankingVm { get; set; }
        public CampaignViewMoldel CampaignVm { get; set; }
        public AnalystViewModel AnalystVm { get; set; }
        public RewardViewModel RewardVm { get; set; }
        public SettingsViewModel SettingVm { get; set; }





        private ViewModelBase _currentChildView;
		
        public ViewModelBase CurrentChildView
        {
            get
            {
                return _currentChildView;
            }
            set
            {
                _currentChildView = value;
                OnPropertyChanged(nameof(CurrentChildView));
            }
        }

        public MainViewModel()
        {
            // Initialize the current child view to HomeViewModel
            HomeVm = new HomeViewModel();
            CalendarVm = new CalendarViewModel();
            PomodoroVm = new PomodoroViewModel();
            RankingVm = new RankingViewModel();
            CampaignVm = new CampaignViewMoldel();
            AnalystVm = new AnalystViewModel();
            RewardVm = new RewardViewModel();
            SettingVm = new SettingsViewModel();
            // Set the initial view model
            CurrentChildView = HomeVm;

            HomeCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = HomeVm;
            });
            CalendarCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = CalendarVm;
            });
            PomodoroCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = PomodoroVm;
            });
            RankingCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = RankingVm;
            });
            CampaignCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = CampaignVm;
            });
            AnalystCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = AnalystVm;
            });
            RewardCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = RewardVm;
            });
            SettingCommand = new ViewModelCommand(o =>
            {
                CurrentChildView = SettingVm;
            });

        }
    }
}
