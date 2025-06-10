using System;
using System.Windows.Input;

namespace Learnify.Models
{
    public class FriendRequestNotification : Notification
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public ICommand AcceptCommand { get; set; }
        public ICommand DeclineCommand { get; set; }
    }
}
