using System.Collections.ObjectModel;
using System.Windows;
using Learnify.Models;
using Learnify.ViewModels;

namespace Learnify.Views
{
    public partial class ShareCampaignWindow : Window
    {
        public ShareCampaignWindow(ObservableCollection<Friend> friends)
        {
            InitializeComponent();
            var vm = new ShareCampaignViewModel(friends);
            vm.OnShareCompleted += (selectedFriends) =>
            {
                this.DialogResult = true;
                this.Close();
            };
            vm.OnCancel += () =>
            {
                this.DialogResult = false;
                this.Close();
            };
            this.DataContext = vm;
        }
    }
}