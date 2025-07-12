using System;
using System.Threading.Tasks;
using Learnify.Services;
using Learnify.ViewModels;

namespace Learnify
{
    /// <summary>
    /// Test class để kiểm tra tính năng cập nhật real-time danh sách bạn bè
    /// </summary>
    public class TestRealTimeFriendsUpdate
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();

        /// <summary>
        /// Test cập nhật real-time khi chấp nhận lời mời kết bạn
        /// </summary>
        public async Task TestAcceptFriendRequestRealTime()
        {
            try
            {
                // Console.WriteLine("=== Test cập nhật real-time khi chấp nhận lời mời kết bạn ===");

                // Test 1: Gửi lời mời kết bạn
                // Console.WriteLine("Test 1: Gửi lời mời kết bạn");
                var result1 = await _firebaseService.SendFriendRequestAsync("user1", "User 1", "user2", "User 2");
                // Console.WriteLine($"Kết quả gửi lời mời: {result1}");

                // Test 2: Chấp nhận lời mời kết bạn
                // Console.WriteLine("Test 2: Chấp nhận lời mời kết bạn");
                var result2 = await _firebaseService.AcceptFriendRequestAsync("user1", "user2", "user1_1234567890");
                // Console.WriteLine($"Kết quả chấp nhận: {result2}");

                // Test 3: Kiểm tra danh sách bạn bè sau khi chấp nhận
                // Console.WriteLine("Test 3: Kiểm tra danh sách bạn bè sau khi chấp nhận");
                var friends1 = await _firebaseService.GetFriendsAsync("user1");
                var friends2 = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1?.Count ?? 0} bạn bè");
                // Console.WriteLine($"User2 có {friends2?.Count ?? 0} bạn bè");

                // Test 4: Kiểm tra thông báo thay đổi
                // Console.WriteLine("Test 4: Kiểm tra thông báo thay đổi");
                var hasChanged1 = await _firebaseService.HasFriendsListChangedAsync("user1", DateTime.UtcNow.AddMinutes(-1));
                var hasChanged2 = await _firebaseService.HasFriendsListChangedAsync("user2", DateTime.UtcNow.AddMinutes(-1));
                // Console.WriteLine($"User1 có thay đổi: {hasChanged1}");
                // Console.WriteLine($"User2 có thay đổi: {hasChanged2}");

                // Console.WriteLine("=== Test hoàn thành ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Test error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test cập nhật real-time khi hủy kết bạn
        /// </summary>
        public async Task TestRemoveFriendRealTime()
        {
            try
            {
                // Console.WriteLine("=== Test cập nhật real-time khi hủy kết bạn ===");

                // Test 1: Kiểm tra danh sách bạn bè trước khi hủy
                // Console.WriteLine("Test 1: Kiểm tra danh sách bạn bè trước khi hủy");
                var friends1Before = await _firebaseService.GetFriendsAsync("user1");
                var friends2Before = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1Before?.Count ?? 0} bạn bè trước khi hủy");
                // Console.WriteLine($"User2 có {friends2Before?.Count ?? 0} bạn bè trước khi hủy");

                // Test 2: Hủy kết bạn
                // Console.WriteLine("Test 2: Hủy kết bạn");
                var result = await _firebaseService.RemoveFriendAsync("user1", "user2");
                // Console.WriteLine($"Kết quả hủy kết bạn: {result}");

                // Test 3: Kiểm tra danh sách bạn bè sau khi hủy
                // Console.WriteLine("Test 3: Kiểm tra danh sách bạn bè sau khi hủy");
                var friends1After = await _firebaseService.GetFriendsAsync("user1");
                var friends2After = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1After?.Count ?? 0} bạn bè sau khi hủy");
                // Console.WriteLine($"User2 có {friends2After?.Count ?? 0} bạn bè sau khi hủy");

                // Test 4: Kiểm tra thông báo thay đổi
                // Console.WriteLine("Test 4: Kiểm tra thông báo thay đổi");
                var hasChanged1 = await _firebaseService.HasFriendsListChangedAsync("user1", DateTime.UtcNow.AddMinutes(-1));
                var hasChanged2 = await _firebaseService.HasFriendsListChangedAsync("user2", DateTime.UtcNow.AddMinutes(-1));
                // Console.WriteLine($"User1 có thay đổi: {hasChanged1}");
                // Console.WriteLine($"User2 có thay đổi: {hasChanged2}");

                // Console.WriteLine("=== Test hoàn thành ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Test error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test polling mechanism
        /// </summary>
        public async Task TestPollingMechanism()
        {
            try
            {
                // Console.WriteLine("=== Test polling mechanism ===");

                // Test 1: Bắt đầu polling
                // Console.WriteLine("Test 1: Bắt đầu polling");
                var mainViewModel = new MainViewModel();
                
                // Test 2: Chờ một khoảng thời gian để xem polling hoạt động
                // Console.WriteLine("Test 2: Chờ 10 giây để xem polling hoạt động");
                await Task.Delay(10000);
                
                // Test 3: Dừng polling
                // Console.WriteLine("Test 3: Dừng polling");
                mainViewModel.Dispose();
                
                // Console.WriteLine("=== Test polling hoàn thành ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Test error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test toàn bộ flow từ gửi lời mời đến chấp nhận và hủy kết bạn
        /// </summary>
        public async Task TestCompleteFlow()
        {
            try
            {
                // Console.WriteLine("=== Test toàn bộ flow ===");

                // Test 1: Gửi lời mời kết bạn
                // Console.WriteLine("Test 1: Gửi lời mời kết bạn");
                var sendResult = await _firebaseService.SendFriendRequestAsync("user1", "User 1", "user2", "User 2");
                // Console.WriteLine($"Kết quả gửi lời mời: {sendResult}");

                // Test 2: Chấp nhận lời mời kết bạn
                // Console.WriteLine("Test 2: Chấp nhận lời mời kết bạn");
                var acceptResult = await _firebaseService.AcceptFriendRequestAsync("user1", "user2", "user1_1234567890");
                // Console.WriteLine($"Kết quả chấp nhận: {acceptResult}");

                // Test 3: Kiểm tra danh sách bạn bè
                // Console.WriteLine("Test 3: Kiểm tra danh sách bạn bè sau khi chấp nhận");
                var friends1 = await _firebaseService.GetFriendsAsync("user1");
                var friends2 = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1?.Count ?? 0} bạn bè");
                // Console.WriteLine($"User2 có {friends2?.Count ?? 0} bạn bè");

                // Test 4: Hủy kết bạn
                // Console.WriteLine("Test 4: Hủy kết bạn");
                var removeResult = await _firebaseService.RemoveFriendAsync("user1", "user2");
                // Console.WriteLine($"Kết quả hủy kết bạn: {removeResult}");

                // Test 5: Kiểm tra danh sách bạn bè sau khi hủy
                // Console.WriteLine("Test 5: Kiểm tra danh sách bạn bè sau khi hủy");
                var friends1After = await _firebaseService.GetFriendsAsync("user1");
                var friends2After = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1After?.Count ?? 0} bạn bè sau khi hủy");
                // Console.WriteLine($"User2 có {friends2After?.Count ?? 0} bạn bè sau khi hủy");

                // Console.WriteLine("=== Test toàn bộ flow hoàn thành ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Test error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test đơn giản để kiểm tra nhanh
        /// </summary>
        public async Task QuickTest()
        {
            try
            {
                // Console.WriteLine("=== Quick Test ===");
                
                // Test 1: Kiểm tra kết nối Firebase
                // Console.WriteLine("Test 1: Kiểm tra kết nối Firebase");
                var testPermission = await _firebaseService.TestFriendsPermissionAsync();
                // Console.WriteLine($"Firebase permission test: {testPermission}");
                
                // Test 2: Kiểm tra lấy danh sách users
                // Console.WriteLine("Test 2: Kiểm tra lấy danh sách users");
                var userIds = await _firebaseService.GetAllUserIdsAsync();
                // Console.WriteLine($"Found {userIds.Count} users");
                
                // Test 3: Kiểm tra thông báo thay đổi
                // Console.WriteLine("Test 3: Kiểm tra thông báo thay đổi");
                var hasChanged = await _firebaseService.HasFriendsListChangedAsync("testUser", DateTime.UtcNow.AddMinutes(-1));
                // Console.WriteLine($"Has friends list changed: {hasChanged}");
                
                // Console.WriteLine("=== Quick Test hoàn thành ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Quick test error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test fix permission issue khi chấp nhận lời mời kết bạn
        /// </summary>
        public async Task TestPermissionFix()
        {
            try
            {
                // Console.WriteLine("=== Test Permission Fix ===");

                // Test 1: Gửi lời mời kết bạn
                // Console.WriteLine("Test 1: Gửi lời mời kết bạn");
                var sendResult = await _firebaseService.SendFriendRequestAsync("user1", "User 1", "user2", "User 2");
                // Console.WriteLine($"Kết quả gửi lời mời: {sendResult}");

                // Test 2: Chấp nhận lời mời kết bạn (với fix permission)
                // Console.WriteLine("Test 2: Chấp nhận lời mời kết bạn (với fix permission)");
                var acceptResult = await _firebaseService.AcceptFriendRequestAsync("user1", "user2", "user1_1234567890");
                // Console.WriteLine($"Kết quả chấp nhận: {acceptResult}");

                if (acceptResult)
                {
                    // Console.WriteLine("✅ Permission fix hoạt động - chấp nhận lời mời thành công!");
                }
                else
                {
                    // Console.WriteLine("❌ Permission fix chưa hoạt động - vẫn có lỗi permission");
                }

                // Test 3: Kiểm tra danh sách bạn bè
                // Console.WriteLine("Test 3: Kiểm tra danh sách bạn bè");
                var friends1 = await _firebaseService.GetFriendsAsync("user1");
                var friends2 = await _firebaseService.GetFriendsAsync("user2");
                // Console.WriteLine($"User1 có {friends1?.Count ?? 0} bạn bè");
                // Console.WriteLine($"User2 có {friends2?.Count ?? 0} bạn bè");

                // Test 4: Hủy kết bạn để test remove permission
                // Console.WriteLine("Test 4: Hủy kết bạn (test remove permission)");
                var removeResult = await _firebaseService.RemoveFriendAsync("user1", "user2");
                // Console.WriteLine($"Kết quả hủy kết bạn: {removeResult}");

                if (removeResult)
                {
                    // Console.WriteLine("✅ Remove permission fix hoạt động - hủy kết bạn thành công!");
                }
                else
                {
                    // Console.WriteLine("❌ Remove permission fix chưa hoạt động - vẫn có lỗi permission");
                }

                // Console.WriteLine("=== Test Permission Fix hoàn thành ===");
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Test permission fix error: {ex.Message}");
                // Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }
    }
} 