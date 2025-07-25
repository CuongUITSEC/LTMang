﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Models
{
    public enum FriendStatus
    {
        None,           // Chưa có quan hệ
        Pending,        // Đang chờ phản hồi (gửi lời mời)
        Incoming,       // Nhận được lời mời
        Friends,        // Đã là bạn bè
        Blocked         // Đã chặn
    }

    public enum SendFriendRequestResult
    {
        Success,        // Gửi thành công
        AlreadyFriends, // Đã là bạn bè
        ExceedsLimit,   // Vượt quá giới hạn 5 lời mời/30 phút
        HasPending,     // Đã có lời mời chờ xử lý
        Error           // Lỗi kỹ thuật
    }

    public class Friend
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; } // Đường dẫn tương đối đến ảnh
        public bool IsOnline { get; set; }
        public FriendStatus Status { get; set; } = FriendStatus.None;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastInteraction { get; set; } = DateTime.Now;
        // Thuộc tính chỉ dùng cho UI, không lưu DB
        public bool IsSelected { get; set; } = false;
    }

    public class FriendRequest
    {
        public string Id { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverEmail { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
        public string Message { get; set; } = "";
        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    }

    public enum FriendRequestStatus
    {
        Pending,    // Đang chờ phản hồi
        Accepted,   // Đã chấp nhận
        Declined,   // Đã từ chối
        Cancelled   // Đã hủy
    }

    public class FriendAcceptedNotification : Notification
    {
        public string FriendId { get; set; }
        public string FriendName { get; set; }
        public FriendAcceptedNotification(string friendId, string friendName)
        {
            FriendId = friendId;
            FriendName = friendName;
            Title = "Kết bạn thành công";
            Message = $"{friendName} đã chấp nhận lời mời kết bạn của bạn.";
            Time = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
            IsRead = false;
        }
    }
}