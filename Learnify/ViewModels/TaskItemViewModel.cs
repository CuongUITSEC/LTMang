using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Learnify.Models;
using Learnify.Commands;
using System.Windows.Media;

namespace Learnify.ViewModels
{
    public class TaskItemViewModel : INotifyPropertyChanged
    {
        private readonly TaskItem _task;

        public string Title => _task.Title;
        public string Description => _task.Description;
        public bool IsCompleted => _task.IsCompleted;
        public bool IsClaimed => _task.IsClaimed;

        public string ButtonText => _task.IsClaimed ? "Đã nhận" : "Nhận";
        public bool IsButtonEnabled => _task.IsCompleted && !_task.IsClaimed;

        public ICommand ClaimCommand { get; }

        public TaskItemViewModel(TaskItem task)
        {
            _task = task;
            ClaimCommand = new RelayCommand(ClaimReward, () => IsButtonEnabled);
        }

        private void ClaimReward()
        {
            if (_task.IsCompleted && !_task.IsClaimed)
            {
                _task.IsClaimed = true;

                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(IsButtonEnabled));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
