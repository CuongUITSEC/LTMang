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
        public HomeViewModel HomeVm { get; set; }

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
            CurrentChildView = HomeVm;
        }
    }
}
