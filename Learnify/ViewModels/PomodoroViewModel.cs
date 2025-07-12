using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Learnify.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.ViewModels
{
    public class PomodoroViewModel : ViewModelBase
    {
        public ViewModelCommand PomodoroModeCommand { get; set; }
        public ViewModelCommand TimerModeCommand { get; set; }
        public PomodoroModeViewModel PomodoroModeVm { get; set; }
        public TimerModeViewModel TimerModeVm { get; set; }

        private ViewModelBase _currentMode;

        public ViewModelBase CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                OnPropertyChanged(nameof(CurrentMode));
            }
        }

        public PomodoroViewModel()
        {
            PomodoroModeVm = new PomodoroModeViewModel();
            TimerModeVm = new TimerModeViewModel();

            CurrentMode = TimerModeVm;

            PomodoroModeCommand = new ViewModelCommand(o => { CurrentMode = PomodoroModeVm; });
            TimerModeCommand = new ViewModelCommand(o => { CurrentMode = TimerModeVm; });
        }
    }
}