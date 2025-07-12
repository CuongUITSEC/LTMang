using System.Windows;
using Learnify.Models;
using Learnify.ViewModels;

namespace Learnify.Views
{
    public partial class FriendInfoWindow : Window
    {
        private readonly FriendInfoWindowViewModel _viewModel;
        public FriendInfoWindow(Friend friend, NotificationViewModel notificationViewModel, MainViewModel mainViewModel = null)
        {
            InitializeComponent();
            _viewModel = new FriendInfoWindowViewModel(friend, notificationViewModel, mainViewModel);
            DataContext = _viewModel;
        }

        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine($"[FriendInfoWindow] AddFriend_Click: FriendId={_viewModel.Friend?.Id}, FriendName={_viewModel.Friend?.Name}");
            _viewModel.AddFriendCommand.Execute(null);
            this.Close();
        }
    }
}
