using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Learnify.Models;
using Learnify.Commands;
using System.Collections.Generic;

namespace Learnify.ViewModels
{
    public class ShareCampaignViewModel : ViewModelBase
    {
        public ObservableCollection<Friend> Friends { get; set; }
        public ICommand ShareCommand { get; }
        public ICommand CancelCommand { get; }

        public event System.Action<IList<Friend>> OnShareCompleted;
        public event System.Action OnCancel;

        public ShareCampaignViewModel(ObservableCollection<Friend> friends)
        {
            Friends = friends;
            ShareCommand = new RelayCommand(Share);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Share()
        {
            var selectedFriends = Friends.Where(f => f.IsSelected).ToList();
            OnShareCompleted?.Invoke(selectedFriends);
        }

        private void Cancel()
        {
            OnCancel?.Invoke();
        }
    }
}