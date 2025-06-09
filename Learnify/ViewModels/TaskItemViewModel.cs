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
        private TaskItem _task;        public TaskItem Task
        {
            get => _task;
            set
            {
                _task = value;
                OnPropertyChanged();
                // Trigger property changed for all dependent properties
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(IsClaimed));
                OnPropertyChanged(nameof(Reward));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(IsButtonEnabled));
            }
        }

        public string Title => Task.Title;
        public string Description => Task.Description;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsClaimed => Task.IsClaimed;
        public string Reward => Task.Reward;
        public string ButtonText => Task.IsClaimed ? "Đã nhận" : "Nhận";
        public bool IsButtonEnabled => Task.IsCompleted && !Task.IsClaimed;

        public ICommand ClaimCommand { get; }

        public TaskItemViewModel(TaskItem task)
        {
            Task = task;
            ClaimCommand = new RelayCommand(ClaimReward, () => IsButtonEnabled);
        }

        private void ClaimReward()
        {
            if (Task.IsCompleted && !Task.IsClaimed)
            {
                Task.IsClaimed = true;

                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(IsButtonEnabled));
            }
        }        public void Refresh()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsClaimed));
            OnPropertyChanged(nameof(Reward));
            OnPropertyChanged(nameof(ButtonText));
            OnPropertyChanged(nameof(IsButtonEnabled));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
