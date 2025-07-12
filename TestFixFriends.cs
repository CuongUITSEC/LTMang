using System;
using System.Threading.Tasks;
using Learnify.Services;

namespace Learnify
{
    /// <summary>
    /// Test class để kiểm tra tính năng cập nhật real-time danh sách bạn bè
    /// </summary>
    public class TestFixFriends
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();

        /// <summary>
        /// Test kết bạn và hủy kết bạn với cập nhật real-time
        /// </summary>
        public async Task TestFriendsRealTimeUpdate()
        {
            try
            {
                // Console.WriteLine("=== Bắt đầu test cập nhật real-time danh sách bạn bè ===");

                // Test 1: Gửi lời mời kết bạn
                // Console.WriteLine("Test 1: Gửi lời mời kết bạn");
                var result1 = await _firebaseService.SendFriendRequestAsync("user1", "User 1", "user2", "User 2");
                // Console.WriteLine($"Kết quả gửi lời mời: {result1}");

                // Test 2: Chấp nhận lời mời kết bạn
                // Console.WriteLine("Test 2: Chấp nhận lời mời kết bạn");
                var result2 = await _firebaseService.AcceptFriendRequestAsync("user1", "user2", "user1_1234567890");
                // Console.WriteLine($"Kết quả chấp nhận: {result2}");

                // Test 3: Kiểm tra danh sách bạn bè
                // Console.WriteLine("Test 3: Kiểm tra danh sách bạn bè");
                var friends1 = await _firebaseService.GetFriendsAsync("user1");
                var friends2 = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1?.Count ?? 0} bạn bè");
                // Console.WriteLine($"User2 có {friends2?.Count ?? 0} bạn bè");

                // Test 4: Hủy kết bạn
                // Console.WriteLine("Test 4: Hủy kết bạn");
                var result3 = await _firebaseService.RemoveFriendAsync("user1", "user2");
                // Console.WriteLine($"Kết quả hủy kết bạn: {result3}");

                // Test 5: Kiểm tra lại danh sách bạn bè
                // Console.WriteLine("Test 5: Kiểm tra lại danh sách bạn bè");
                var friends1After = await _firebaseService.GetFriendsAsync("user1");
                var friends2After = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1After?.Count ?? 0} bạn bè sau khi hủy");
                // Console.WriteLine($"User2 có {friends2After?.Count ?? 0} bạn bè sau khi hủy");

                // Test 6: Test thông báo thay đổi
                // Console.WriteLine("Test 6: Test thông báo thay đổi");
                var notifyResult1 = await _firebaseService.NotifyFriendsListChangeAsync("user1");
                var notifyResult2 = await _firebaseService.NotifyFriendsListChangeAsync("user2");
                // Console.WriteLine($"Thông báo cho user1: {notifyResult1}");
                // Console.WriteLine($"Thông báo cho user2: {notifyResult2}");

                // Test 7: Kiểm tra thay đổi
                // Console.WriteLine("Test 7: Kiểm tra thay đổi");
                var hasChanged1 = await _firebaseService.HasFriendsListChangedAsync("user1", DateTime.UtcNow.AddMinutes(-1));
                var hasChanged2 = await _firebaseService.HasFriendsListChangedAsync("user2", DateTime.UtcNow.AddMinutes(-1));
                // Console.WriteLine($"User1 có thay đổi: {hasChanged1}");
                // Console.WriteLine($"User2 có thay đổi: {hasChanged2}");

                // Console.WriteLine("=== Kết thúc test ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Lỗi trong test: {ex.Message}");
                // Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Test polling mechanism
        /// </summary>
        public async Task TestPollingMechanism()
        {
            try
            {
                // Console.WriteLine("=== Bắt đầu test polling mechanism ===");

                var userId = "testUser";
                var lastCheck = DateTime.UtcNow.AddMinutes(-5);

                // Thông báo thay đổi
                await _firebaseService.NotifyFriendsListChangeAsync(userId);

                // Đợi một chút
                await Task.Delay(2000);

                // Kiểm tra thay đổi
                var hasChanged = await _firebaseService.HasFriendsListChangedAsync(userId, lastCheck);
                // Console.WriteLine($"Có thay đổi: {hasChanged}");

                // Console.WriteLine("=== Kết thúc test polling ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Lỗi trong test polling: {ex.Message}");
            }
        }
    }
}
