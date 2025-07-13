using System.Windows.Media;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using Learnify.ViewModels;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Learnify.Models;


namespace Learnify.Services
{
    public class FirebaseService
    {
        // Gửi request chia sẻ chiến dịch cho từng bạn bè và tạo notification
        public async Task<bool> SendSharedCampaignRequestsAsync(string ownerId, string campaignName, DateTime? campaignDate, List<Friend> friends)
        {
            try
            {
                var tasks = new List<Task<bool>>();
                foreach (var friend in friends)
                {
                    var requestId = Guid.NewGuid().ToString();
                    var requestData = new
                    {
                        fromUserId = ownerId,
                        campaignName = campaignName,
                        campaignDate = campaignDate?.ToString("yyyy-MM-dd"),
                        sentAt = DateTime.UtcNow.ToString("o"),
                        status = "Pending"
                    };
                    // Ghi request vào sharedCampaignRequests/{friendId}/{requestId}
                    var requestUrl = GetAuthenticatedUrl($"sharedCampaignRequests/{friend.Id}/{requestId}.json");
                    var requestContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
                    var requestTask = _httpClient.PutAsync(requestUrl, requestContent).ContinueWith(t => t.Result.IsSuccessStatusCode);
                    tasks.Add(requestTask);

                    // Gửi notification cho bạn bè
                    var notificationId = Guid.NewGuid().ToString();
                    var notificationData = new
                    {
                        id = notificationId,
                        type = "SharedCampaign",
                        title = "Chiến dịch mới được chia sẻ",
                        message = $"Bạn nhận được lời mời tham gia chiến dịch '{campaignName}' từ bạn bè.",
                        time = DateTime.Now.ToString("HH:mm"),
                        timestamp = DateTime.UtcNow.ToString("o"),
                        isRead = false,
                        fromUserId = ownerId,
                        requestId = requestId
                    };
                    var notiUrl = GetAuthenticatedUrl($"notifications/{friend.Id}/{notificationId}.json");
                    var notiContent = new StringContent(JsonConvert.SerializeObject(notificationData), Encoding.UTF8, "application/json");
                    var notiTask = _httpClient.PutAsync(notiUrl, notiContent).ContinueWith(t => t.Result.IsSuccessStatusCode);
                    tasks.Add(notiTask);
                }
                var results = await Task.WhenAll(tasks);
                return results.All(r => r);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending shared campaign requests: {ex.Message}");
                return false;
            }
        }
        private static readonly HttpClient _httpClient;
        public static HttpClient SharedHttpClient => _httpClient;

        // Cache để tránh loop vô hạn trong CheckAndSyncFriendsListAsync
        private static readonly HashSet<string> _syncCache = new HashSet<string>();

        // Cache user IDs từ successful operations để fallback khi cần
        private static readonly HashSet<string> _processedMarkers = new HashSet<string>();
        private static readonly HashSet<string> _userIdCache = new HashSet<string>();

        static FirebaseService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // Lưu thông tin chia sẻ chiến dịch cho bạn bè lên Firebase
        public async Task<bool> ShareCampaignToFriendsAsync(string ownerId, string campaignName, DateTime? campaignDate, List<Friend> friends)
        {
            try
            {
                var url = GetAuthenticatedUrl($"sharedCampaigns/{ownerId}.json");
                var data = new
                {
                    campaignName = campaignName,
                    campaignDate = campaignDate?.ToString("yyyy-MM-dd"),
                    sharedTo = friends.Select(f => new { f.Id, f.Name, f.Email }).ToList(),
                    sharedAt = DateTime.UtcNow.ToString("o")
                };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var resp = await response.Content.ReadAsStringAsync();
                    System.Windows.MessageBox.Show($"Lỗi chia sẻ campaign: {response.StatusCode}\n{resp}", "Lỗi Firebase");
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception khi chia sẻ campaign: {ex}", "Lỗi Exception");
                Debug.WriteLine($"Error sharing campaign: {ex.Message}");
                return false;
            }
        }

        private const string FirebaseUrl = "https://learnify-b5cf3-default-rtdb.asia-southeast1.firebasedatabase.app/";

        public FirebaseService()
        {
            // Không cần thiết lập timeout ở đây nữa vì đã được thiết lập trong static constructor
        }

        public string GetAuthenticatedUrl(string path)
        {
            var token = AuthService.GetToken();
            return $"{FirebaseUrl}{path}?auth={token}";
        }

        public async Task<Dictionary<string, object>> GetUserDataAsync(string userId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        return JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Exception khi lấy user data: {ex}", "Lỗi Exception");
                Debug.WriteLine($"Error getting user data: {ex.Message}");
                return null;
            }
        }

        public async Task<StudyTimeData> GetStudyTimeDataAsync(string userId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/studyTime.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var data = JObject.Parse(content);
                        var studyTimeData = new StudyTimeData
                        {
                            TotalMinutes = data["totalMinutes"]?.Value<double>() ?? 0,
                            LastUpdated = data["lastUpdated"]?.ToString(),
                            Sessions = new List<StudySession>()
                        };

                        var sessions = data["sessions"] as JObject;
                        if (sessions != null)
                        {
                            foreach (var session in sessions)
                            {
                                studyTimeData.Sessions.Add(new StudySession
                                {
                                    Duration = session.Value["duration"]?.Value<double>() ?? 0,
                                    Timestamp = session.Value["timestamp"]?.ToString()
                                });
                            }
                        }

                        return studyTimeData;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Error getting study time data: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetUsernameAsync(string userId)
        {
            try
            {
                Debug.WriteLine($"[GetUsernameAsync] Getting username for user: {userId}");

                // Thử lấy từ publicUsers trước (có thể đọc được)
                var publicUrl = GetAuthenticatedUrl($"publicUsers/{userId}.json");
                Debug.WriteLine($"[GetUsernameAsync] Request URL (public): {publicUrl}");
                var response = await _httpClient.GetAsync(publicUrl);
                Debug.WriteLine($"[GetUsernameAsync] Response status (public): {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[GetUsernameAsync] Response content (public): {content}");

                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var data = JObject.Parse(content);
                        var username = data["username"]?.ToString();

                        if (!string.IsNullOrEmpty(username) && username != "null")
                        {
                            Debug.WriteLine($"[GetUsernameAsync] Found username in publicUsers: {username}");
                            return username;
                        }
                    }
                }

                // Fallback: Thử lấy từ users (chỉ cho chính mình)
                if (userId == AuthService.GetUserId())
                {
                    var privateUrl = GetAuthenticatedUrl($"users/{userId}/username.json");
                    Debug.WriteLine($"[GetUsernameAsync] Request URL (private): {privateUrl}");
                    var privateResponse = await _httpClient.GetAsync(privateUrl);
                    Debug.WriteLine($"[GetUsernameAsync] Response status (private): {privateResponse.StatusCode}");

                    if (privateResponse.IsSuccessStatusCode)
                    {
                        var content = await privateResponse.Content.ReadAsStringAsync();
                        var username = content.Trim('"');
                        if (!string.IsNullOrEmpty(username) && username != "null")
                        {
                            Debug.WriteLine($"[GetUsernameAsync] Found username in users: {username}");
                            return username;
                        }
                    }
                }

                Debug.WriteLine($"[GetUsernameAsync] No username found for {userId}, returning UID");
                return userId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetUsernameAsync] Exception: {ex.Message}");
                return userId;
            }
        }

        public async Task<bool> SaveUsernameAsync(string userId, string username)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/username.json");
                var content = new StringContent(JsonConvert.SerializeObject(username));
                var response = await _httpClient.PutAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Error saving username: {ex.Message}");
                return false;
            }
        }

        // Lưu thời gian học của người dùng (theo giờ Việt Nam)
        public async Task<bool> SaveStudyTimeAsync(string userId, TimeSpan studyTime)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    // Debug.WriteLine("Error: User ID is null or empty");
                    return false;
                }

                // Kiểm tra token
                var token = AuthService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    // Debug.WriteLine("Error: Authentication token is missing");
                    return false;
                }

                // Debug.WriteLine($"Attempting to save study time for user {userId}");
                // Debug.WriteLine($"Current study time: {studyTime.TotalMinutes} minutes");

                // Kiểm tra và cập nhật username nếu chưa có
                var username = await GetUsernameAsync(userId);
                if (string.IsNullOrEmpty(username) || username == "null")
                {
                    // Lấy tên thật của user từ AuthService nếu có
                    var realUsername = AuthService.GetUsername();
                    if (string.IsNullOrEmpty(realUsername))
                        realUsername = userId;
                    await SaveUsernameAsync(userId, realUsername);
                }

                // Tạo timestamp cho phiên học mới (giờ Việt Nam, không có hậu tố Z)
                var nowVN = DateTime.UtcNow.AddHours(7);
                var timestamp = nowVN.ToString("yyyyMMddHHmmss");
                var newSession = new
                {
                    duration = studyTime.TotalMinutes,
                    timestamp = nowVN.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") // Không có Z, Kind = Unspecified
                };

                // Lấy dữ liệu hiện tại từ Firebase
                var url = GetAuthenticatedUrl($"users/{userId}/studyTime.json");
                // Debug.WriteLine($"Fetching current data from: {url}");

                var getResponse = await _httpClient.GetAsync(url);
                // Debug.WriteLine($"GET Response Status: {getResponse.StatusCode}");

                var totalMinutes = studyTime.TotalMinutes;
                var sessions = new Dictionary<string, object>();

                if (getResponse.IsSuccessStatusCode)
                {
                    var responseContent = await getResponse.Content.ReadAsStringAsync();
                    // Debug.WriteLine($"Current data: {responseContent}");

                    if (!string.IsNullOrEmpty(responseContent) && responseContent != "null")
                    {
                        try
                        {
                            var existingData = JObject.Parse(responseContent);
                            var existingTotalMinutes = existingData["totalMinutes"]?.Value<double>() ?? 0;
                            // Debug.WriteLine($"Existing total minutes: {existingTotalMinutes}");
                            totalMinutes += existingTotalMinutes;
                            // Debug.WriteLine($"New total minutes: {totalMinutes}");

                            var existingSessions = existingData["sessions"] as JObject;
                            if (existingSessions != null)
                            {
                                sessions = existingSessions.ToObject<Dictionary<string, object>>();
                                // Debug.WriteLine($"Existing sessions count: {sessions.Count}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error parsing existing data: {ex.Message}");
                            // Nếu có lỗi khi parse, tạo dữ liệu mới
                            totalMinutes = studyTime.TotalMinutes;
                            sessions = new Dictionary<string, object>();
                        }
                    }
                }
                else
                {
                    var errorContent = await getResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error fetching current data: {errorContent}");
                }

                // Thêm phiên học mới vào sessions
                sessions[timestamp] = newSession;
                Debug.WriteLine($"Added new session. Total sessions: {sessions.Count}");

                // Tạo dữ liệu học tập mới
                var studyData = new
                {
                    totalMinutes = totalMinutes,
                    lastUpdated = nowVN.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                    sessions = sessions
                };

                // Cập nhật dữ liệu
                var json = JsonConvert.SerializeObject(studyData);
                Debug.WriteLine($"Saving data: {json}");

                var putContent = new StringContent(json, Encoding.UTF8, "application/json");
                var putResponse = await _httpClient.PutAsync(url, putContent);
                Debug.WriteLine($"PUT Response Status: {putResponse.StatusCode}");

                if (!putResponse.IsSuccessStatusCode)
                {
                    var errorContent = await putResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error saving study time: {errorContent}");
                    return false;
                }

                Debug.WriteLine("Study time saved successfully");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Network error while saving study time: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving study time: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Cleanup expired friend requests để tránh spam database
        /// </summary>
        public async Task<int> CleanupExpiredFriendRequestsAsync(int maxAgeDays = 30)
        {
            try
            {
                Debug.WriteLine($"[CleanupExpiredFriendRequests] Starting cleanup of requests older than {maxAgeDays} days");

                var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);
                var cleanedCount = 0;

                // Lấy tất cả friendRequests
                var url = GetAuthenticatedUrl("friendRequests.json");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[CleanupExpiredFriendRequests] Failed to get friendRequests: {response.StatusCode}");
                    return 0;
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    Debug.WriteLine("[CleanupExpiredFriendRequests] No friend requests found");
                    return 0;
                }

                var allRequests = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(content);
                Debug.WriteLine($"[CleanupExpiredFriendRequests] Found {allRequests.Count} receivers with requests");

                foreach (var receiverData in allRequests)
                {
                    var receiverId = receiverData.Key;
                    var requests = receiverData.Value;

                    var requestsToDelete = new List<string>();

                    foreach (var requestData in requests)
                    {
                        try
                        {
                            var requestId = requestData.Key;
                            var request = requestData.Value;

                            var sentAtStr = request.sentAt?.ToString();
                            DateTime sentAt = DateTime.UtcNow;
                            if (!string.IsNullOrEmpty(sentAtStr))
                            {
                                DateTime parsedSentAt;
                                if (DateTime.TryParse(sentAtStr, out parsedSentAt))
                                {
                                    sentAt = parsedSentAt;
                                }
                            }
                            var status = request.status?.ToString();

                            if (!string.IsNullOrEmpty(sentAtStr))
                            {
                                // Xóa request cũ hoặc đã declined/accepted
                                if (sentAt < cutoffDate || status == "Declined")
                                {
                                    requestsToDelete.Add(requestId);
                                }
                            }
                            else if (status == "Declined")
                            {
                                // Xóa request declined ngay cả khi không parse được timestamp
                                requestsToDelete.Add(requestId);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[CleanupExpiredFriendRequests] Error processing request: {ex.Message}");
                        }
                    }

                    // Xóa các requests cũ
                    foreach (var requestId in requestsToDelete)
                    {
                        try
                        {
                            var deleteUrl = GetAuthenticatedUrl($"friendRequests/{receiverId}/{requestId}.json");
                            var deleteResponse = await _httpClient.DeleteAsync(deleteUrl);

                            if (deleteResponse.IsSuccessStatusCode)
                            {
                                cleanedCount++;
                                Debug.WriteLine($"[CleanupExpiredFriendRequests] Deleted request {requestId} for receiver {receiverId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[CleanupExpiredFriendRequests] Error deleting request {requestId}: {ex.Message}");
                        }

                        // Small delay để tránh spam
                        await Task.Delay(50);
                    }
                }

                Debug.WriteLine($"[CleanupExpiredFriendRequests] Cleaned up {cleanedCount} expired requests");
                return cleanedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CleanupExpiredFriendRequests] Exception: {ex.Message}");
                Debug.WriteLine($"[CleanupExpiredFriendRequests] StackTrace: {ex.StackTrace}");
                return 0;
            }
        }

        /// <summary>
        /// Kiểm tra và fix data consistency cho friends list
        /// </summary>
        public async Task<int> ValidateAndFixFriendsConsistencyAsync()
        {
            try
            {
                Debug.WriteLine("[ValidateAndFixFriendsConsistency] Starting friends data consistency check");
                var fixedCount = 0;

                // Lấy tất cả users
                var userIds = await GetAllUserIdsAsync();
                Debug.WriteLine($"[ValidateAndFixFriendsConsistency] Checking {userIds.Count} users");

                foreach (var userId in userIds)
                {
                    try
                    {
                        var friends = await GetFriendsAsync(userId);

                        foreach (var friend in friends)
                        {
                            // Kiểm tra tính nhất quán 2 chiều
                            var isFriendBack = await AreAlreadyFriendsAsync(friend.Id, userId);

                            if (!isFriendBack)
                            {
                                Debug.WriteLine($"[ValidateAndFixFriendsConsistency] Inconsistency found: {userId} has {friend.Id} as friend but not vice versa");

                                // Fix bằng cách thêm lại mối quan hệ 2 chiều
                                var friendData = new Dictionary<string, object>
                                {
                                    ["status"] = "Friends",
                                    ["since"] = DateTime.UtcNow.ToString("o"),
                                    ["fixedAt"] = DateTime.UtcNow.ToString("o")
                                };

                                var friendUrl = GetAuthenticatedUrl($"friends/{friend.Id}/{userId}.json");
                                var friendContent = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                                var friendResponse = await _httpClient.PutAsync(friendUrl, friendContent);
                                friendContent.Dispose();

                                if (friendResponse.IsSuccessStatusCode)
                                {
                                    fixedCount++;
                                    Debug.WriteLine($"[ValidateAndFixFriendsConsistency] Fixed: Added {userId} to {friend.Id}'s friends list");
                                }
                            }
                        }

                        // Small delay để tránh spam requests
                        await Task.Delay(100);
                    }
                    catch (Exception userEx)
                    {
                        Debug.WriteLine($"[ValidateAndFixFriendsConsistency] Error processing user {userId}: {userEx.Message}");
                    }
                }

                Debug.WriteLine($"[ValidateAndFixFriendsConsistency] Fixed {fixedCount} consistency issues");
                return fixedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ValidateAndFixFriendsConsistency] Exception: {ex.Message}");
                return 0;
            }
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    Debug.WriteLine("Checking internet connection...");
                    var response = await client.GetAsync("https://www.google.com");
                    var isConnected = response.IsSuccessStatusCode;
                    Debug.WriteLine($"Internet connection check result: {isConnected}");
                    return isConnected;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking internet connection: {ex.Message}");
                return false;
            }
        }

        // Lấy thời gian học của người dùng
        public async Task<TimeSpan> GetStudyTimeAsync(string userId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/studyTime.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var data = JObject.Parse(content);
                        var totalMinutes = data["totalMinutes"]?.Value<double>() ?? 0;
                        return TimeSpan.FromMinutes(totalMinutes);
                    }
                }
                return TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting study time: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        public class StudyTimeData
        {
            public double TotalMinutes { get; set; }
            public string LastUpdated { get; set; }
            public List<StudySession> Sessions { get; set; }
        }

        public class StudySession
        {
            public double Duration { get; set; }
            public string Timestamp { get; set; }
        }

        public async Task<List<StudyLog>> GetUserStudyLogsAsync(string userId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/studyLogs.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var logs = JsonConvert.DeserializeObject<Dictionary<string, StudyLog>>(content);
                        return logs.Values.ToList();
                    }
                }
                return new List<StudyLog>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting study logs: {ex.Message}");
                return new List<StudyLog>();
            }
        }

        public async Task<bool> SaveStudyLogAsync(string userId, StudyLog log)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/studyLogs.json");
                var content = new StringContent(JsonConvert.SerializeObject(log));
                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving study log: {ex.Message}");
                return false;
            }
        }

        public async Task FixMissingUsernamesAsync()
        {
            try
            {
                var url = GetAuthenticatedUrl("users.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var users = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(content);
                    if (users != null)
                    {
                        foreach (var user in users)
                        {
                            try
                            {
                                var userId = user.Key;
                                var username = user.Value["username"]?.ToString();

                                // Kiểm tra nếu username không tồn tại hoặc không hợp lệ
                                if (string.IsNullOrEmpty(username) || username == "null" || username == userId)
                                {
                                    Debug.WriteLine($"Fixing username for user {userId}");

                                    // Thử lấy email từ AuthService nếu có
                                    var email = AuthService.GetUsername();
                                    if (!string.IsNullOrEmpty(email) && email.Contains("@"))
                                    {
                                        username = email.Split('@')[0];
                                    }
                                    else
                                    {
                                        // Nếu không có email, tạo username từ userId
                                        username = $"user_{userId.Substring(0, Math.Min(8, userId.Length))}";
                                    }

                                    // Lưu username
                                    var saveResult = await SaveUsernameAsync(userId, username);
                                    if (!saveResult)
                                    {
                                        Debug.WriteLine($"Failed to save username for user {userId}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error fixing username for user {user.Key}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fixing missing usernames: {ex.Message}");
            }
        }

        public async Task<Friend> GetUserByUidAsync(string uid)
        {
            try
            {
                Debug.WriteLine($"[GetUserByUid] Searching for user: {uid}");

                if (string.IsNullOrWhiteSpace(uid))
                {
                    Debug.WriteLine("[GetUserByUid] UID is null or empty");
                    return null;
                }

                // Thử tìm trong publicUsers trước (cho phép tìm kiếm tất cả user)
                var publicUrl = GetAuthenticatedUrl($"publicUsers/{uid}.json");
                Debug.WriteLine($"[GetUserByUid] Request URL (public): {publicUrl}");

                var response = await _httpClient.GetAsync(publicUrl);
                Debug.WriteLine($"[GetUserByUid] Response status (public): {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[GetUserByUid] Response content (public): {content}");

                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var data = JObject.Parse(content);
                        var username = data["username"]?.ToString() ?? uid;
                        var avatar = data["avatarUrl"]?.ToString() ?? "/Images/avatar1.svg";
                        var email = data["email"]?.ToString() ?? "";

                        var friend = new Friend
                        {
                            Name = username,
                            Avatar = avatar,
                            IsOnline = false, // Public data không chứa trạng thái online
                            Id = uid,
                            Email = email
                        };
                        Debug.WriteLine($"[GetUserByUid] Friend object: Id={friend.Id}, Name={friend.Name}, Email={friend.Email}, Avatar={friend.Avatar}, IsOnline={friend.IsOnline}");

                        Debug.WriteLine($"[GetUserByUid] Found user in publicUsers: {username} ({uid})");
                        return friend;
                    }
                }

                // Nếu không tìm thấy trong publicUsers, thử tìm trong users (chỉ cho chính mình)
                if (uid == AuthService.GetUserId())
                {
                    var privateUrl = GetAuthenticatedUrl($"users/{uid}.json");
                    Debug.WriteLine($"[GetUserByUid] Request URL (private): {privateUrl}");

                    var privateResponse = await _httpClient.GetAsync(privateUrl);
                    Debug.WriteLine($"[GetUserByUid] Response status (private): {privateResponse.StatusCode}");

                    if (privateResponse.IsSuccessStatusCode)
                    {
                        var content = await privateResponse.Content.ReadAsStringAsync();
                        Debug.WriteLine($"[GetUserByUid] Response content (private): {content}");

                        if (!string.IsNullOrEmpty(content) && content != "null")
                        {
                            var data = JObject.Parse(content);
                            var username = data["username"]?.ToString() ?? uid;
                            var avatar = data["avatarUrl"]?.ToString() ?? "/Images/avatar1.svg";
                            var isOnline = data["isOnline"]?.Value<bool>() ?? false;

                            var friend = new Friend
                            {
                                Name = username,
                                Avatar = avatar,
                                IsOnline = isOnline,
                                Id = uid,
                                Email = data["email"]?.ToString() ?? ""
                            };
                            Debug.WriteLine($"[GetUserByUid] Friend object (private): Id={friend.Id}, Name={friend.Name}, Email={friend.Email}, Avatar={friend.Avatar}, IsOnline={friend.IsOnline}");

                            Debug.WriteLine($"[GetUserByUid] Found user in users: {username} ({uid})");
                            return friend;
                        }
                    }
                    else
                    {
                        var errorContent = await privateResponse.Content.ReadAsStringAsync();
                        Debug.WriteLine($"[GetUserByUid] HTTP Error (private): {privateResponse.StatusCode}, Content: {errorContent}");
                    }
                }

                Debug.WriteLine("[GetUserByUid] User not found in both publicUsers and users");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetUserByUid] Exception: {ex.Message}");
                Debug.WriteLine($"[GetUserByUid] StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<User> GetUserInfoAsync(string userId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}.json");
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[FIREBASE] GetUserInfoAsync content: {content}");
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var userData = JObject.Parse(content);

                        var user = new User
                        {
                            Id = userId,
                            Username = userData["username"]?.Value<string>() ?? userId,
                            Email = userData["email"]?.Value<string>() ?? "",
                            PhoneNumber = userData["phoneNumber"]?.Value<string>() ?? "",
                            Country = userData["country"]?.Value<string>() ?? "Việt Nam",
                            AvatarUrl = userData["avatarUrl"]?.Value<string>() ?? "",
                            CreatedAt = DateTime.TryParse(userData["createdAt"]?.Value<string>(), out var created) ? created : DateTime.Now,
                            LastLoginAt = DateTime.TryParse(userData["lastLoginAt"]?.Value<string>(), out var lastLogin) ? lastLogin : DateTime.Now
                        };

                        return user;
                    }
                }

                // Nếu không có dữ liệu, tạo user mới với thông tin cơ bản
                return new User
                {
                    Id = userId,
                    Username = userId,
                    Email = "",
                    PhoneNumber = "",
                    Country = "Việt Nam",
                    AvatarUrl = "",
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting user info: {ex.Message}");
                // Trả về user với thông tin cơ bản
                return new User
                {
                    Id = userId,
                    Username = userId,
                    Email = "",
                    PhoneNumber = "",
                    Country = "Việt Nam",
                    AvatarUrl = "",
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now
                };
            }
        }

        public async Task<bool> UpdateUserInfoAsync(string userId, User user)
        {
            try
            {
                var userData = new
                {
                    username = user.Username,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    country = user.Country,
                    avatarUrl = user.AvatarUrl,
                    lastLoginAt = DateTime.UtcNow.ToString("o")
                };

                var url = GetAuthenticatedUrl($"users/{userId}.json");
                var json = JsonConvert.SerializeObject(userData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating user info: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveUserAuthInfoAsync(string userId, string email, string username = null)
        {
            try
            {
                var userData = new
                {
                    email = email
                };

                var url = GetAuthenticatedUrl($"users/{userId}.json");
                var json = JsonConvert.SerializeObject(userData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Sử dụng PATCH thay vì PUT để chỉ cập nhật trường email
                var response = await _httpClient.PatchAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user auth info: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> ChangePasswordAsync(string newPassword)
        {
            try
            {
                const string apiKey = "AIzaSyAhTPGYk6qxu_t-RXT3F3LOxgBk65LicIY";
                var url = $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={apiKey}";

                var payload = new
                {
                    idToken = AuthService.GetToken(),
                    password = newPassword,
                    returnSecureToken = true
                };

                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // Update token if returned
                        try
                        {
                            dynamic resultObj = JsonConvert.DeserializeObject(responseString);
                            if (resultObj?.idToken != null)
                            {
                                AuthService.SetToken((string)resultObj.idToken);
                            }
                        }
                        catch
                        {
                            // Token update failed but password change succeeded
                        }
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"Password change failed: {responseString}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing password: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                // First verify the current password by attempting to sign in
                const string apiKey = "AIzaSyAhTPGYk6qxu_t-RXT3F3LOxgBk65LicIY";
                var verifyUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

                // Get current user email from AuthService (not from database)
                var email = AuthService.GetUsername();
                if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                {
                    Debug.WriteLine("Could not get current user email for password verification");
                    return false;
                }

                var verifyPayload = new
                {
                    email = email,
                    password = currentPassword,
                    returnSecureToken = true
                };

                using (var client = new HttpClient())
                {
                    var verifyContent = new StringContent(JsonConvert.SerializeObject(verifyPayload), Encoding.UTF8, "application/json");
                    var verifyResponse = await client.PostAsync(verifyUrl, verifyContent);
                    var verifyResponseString = await verifyResponse.Content.ReadAsStringAsync();

                    if (!verifyResponse.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"Current password verification failed: {verifyResponseString}");
                        return false;
                    }

                    // If verification successful, proceed with password change
                    return await ChangePasswordAsync(newPassword);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error verifying current password: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserRanking>> GetTopUsersAsync(int count)
        {
            try
            {
                var url = GetAuthenticatedUrl("users.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var users = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(content);
                        var rankings = new List<UserRanking>();

                        foreach (var user in users)
                        {
                            var displayName = user.Value["displayName"]?.ToString();
                            var points = user.Value["points"]?.Value<int>() ?? 0;

                            if (!string.IsNullOrEmpty(displayName))
                            {
                                rankings.Add(new UserRanking
                                {
                                    UserId = user.Key,
                                    DisplayName = displayName,
                                    Points = points
                                });
                            }
                        }

                        return rankings.OrderByDescending(r => r.Points).Take(count).ToList();
                    }
                }
                return new List<UserRanking>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting top users: {ex.Message}");
                return new List<UserRanking>();
            }
        }

        public async Task<List<(string UserId, TimeSpan Time)>> GetStudyTimeRankingsAsync()
        {
            try
            {
                Debug.WriteLine("[FIREBASE] Getting weekly study time rankings for friends only...");
                var currentUserId = AuthService.GetUserId();
                Debug.WriteLine($"[FIREBASE] Current user: {currentUserId}");

                // Tính toán tuần hiện tại (thứ 2 đến chủ nhật) theo giờ Việt Nam (UTC+7)
                var nowVN = DateTime.UtcNow.AddHours(7);
                var today = nowVN.Date;
                int dayOfWeek = (int)today.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Chủ nhật = 7 thay vì 0
                var startOfWeek = today.AddDays(-(dayOfWeek - 1)); // Lùi về thứ 2
                var endOfWeek = startOfWeek.AddDays(6); // Chủ nhật của tuần
                Debug.WriteLine($"[FIREBASE] Current week (VN): {startOfWeek:yyyy-MM-dd} to {endOfWeek:yyyy-MM-dd}");

                // 1. Lấy danh sách bạn bè của current user
                Debug.WriteLine("[FIREBASE] Step 1: Getting friends list...");
                var friends = await GetFriendsAsync(currentUserId);
                Debug.WriteLine($"[FIREBASE] Found {friends.Count} friends");

                if (friends.Count == 0)
                {
                    Debug.WriteLine("[FIREBASE] No friends found, returning empty rankings");
                    return new List<(string UserId, TimeSpan Time)>();
                }

                // 2. Thêm current user vào danh sách để tính toán
                var allUserIds = new List<string> { currentUserId };
                allUserIds.AddRange(friends.Select(f => f.Id));
                Debug.WriteLine($"[FIREBASE] Processing {allUserIds.Count} users (including self)");

                var rankings = new List<(string UserId, TimeSpan Time)>();

                // 3. Lấy thời gian học của từng user (chỉ bạn bè + bản thân)
                foreach (var userId in allUserIds)
                {
                    try
                    {
                        Debug.WriteLine($"[FIREBASE] Processing user: {userId}");

                        // Lấy thời gian học của user này
                        var studyTimeData = await GetStudyTimeDataAsync(userId);
                        if (studyTimeData != null && studyTimeData.Sessions != null)
                        {
                            double weeklyMinutes = 0;
                            Debug.WriteLine($"[FIREBASE] User {userId}: Processing {studyTimeData.Sessions.Count} sessions");

                            foreach (var session in studyTimeData.Sessions)
                            {
                                try
                                {
                                    var timestampStr = session.Timestamp;
                                    var duration = session.Duration;
                                    DateTime sessionTimeVN = DateTime.MinValue;
                                    bool parsed = false;

                                    if (!string.IsNullOrEmpty(timestampStr))
                                    {
                                        // Try ISO format first (for new sessions)
                                        parsed = DateTime.TryParseExact(
                                            timestampStr,
                                            "yyyy-MM-ddTHH:mm:ss.fffffff",
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.None,
                                            out sessionTimeVN
                                        );
                                        // Try current format MM/dd/yyyy hh:mm:ss tt (with AM/PM)
                                        if (!parsed)
                                        {
                                            parsed = DateTime.TryParseExact(
                                                timestampStr,
                                                "MM/dd/yyyy hh:mm:ss tt",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out sessionTimeVN
                                            );
                                        }
                                        // Try 24-hour format MM/dd/yyyy HH:mm:ss
                                        if (!parsed)
                                        {
                                            parsed = DateTime.TryParseExact(
                                                timestampStr,
                                                "MM/dd/yyyy HH:mm:ss",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out sessionTimeVN
                                            );
                                        }
                                        // Try legacy format if all else fails
                                        if (!parsed)
                                        {
                                            parsed = DateTime.TryParseExact(
                                                timestampStr,
                                                "MM/dd/yyyy HH:mm:ss",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out sessionTimeVN
                                            );
                                        }
                                        // Final fallback: use DateTime.TryParse
                                        if (!parsed)
                                        {
                                            parsed = DateTime.TryParse(timestampStr, out sessionTimeVN);
                                        }
                                    }

                                    if (parsed)
                                    {
                                        var sessionDate = sessionTimeVN.Date;
                                        Debug.WriteLine($"[FIREBASE][DEBUG] User {userId}: Session raw timestamp = '{timestampStr}', parsed = {sessionTimeVN:yyyy-MM-dd HH:mm:ss}, sessionDate = {sessionDate:yyyy-MM-dd}");

                                        // Kiểm tra session có trong tuần hiện tại không
                                        if (sessionDate >= startOfWeek && sessionDate <= endOfWeek)
                                        {
                                            weeklyMinutes += duration;
                                            Debug.WriteLine($"[FIREBASE] User {userId}: Session {sessionDate:yyyy-MM-dd} = {duration} minutes (weekly total: {weeklyMinutes})");
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"[FIREBASE][DEBUG] User {userId}: Session {sessionDate:yyyy-MM-dd} is OUTSIDE week {startOfWeek:yyyy-MM-dd} to {endOfWeek:yyyy-MM-dd}");
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[FIREBASE][DEBUG] User {userId}: Could not parse timestamp '{timestampStr}' with any format");
                                    }
                                }
                                catch (Exception sessionEx)
                                {
                                    Debug.WriteLine($"[FIREBASE] Error processing session for user {userId}: {sessionEx.Message}");
                                }
                            }

                            Debug.WriteLine($"[FIREBASE] User {userId}: Weekly total = {weeklyMinutes} minutes");
                            if (weeklyMinutes > 0)
                            {
                                rankings.Add((userId, TimeSpan.FromMinutes(weeklyMinutes)));
                                Debug.WriteLine($"[FIREBASE] Added {userId} to rankings with {weeklyMinutes} minutes");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[FIREBASE] User {userId}: No study time data");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[FIREBASE] Error processing user {userId}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"[FIREBASE] Final weekly rankings count: {rankings.Count}");
                return rankings.OrderByDescending(r => r.Time.TotalMinutes).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FIREBASE] Error getting weekly study time rankings: {ex.Message}");
                Debug.WriteLine($"[FIREBASE] StackTrace: {ex.StackTrace}");
                return new List<(string UserId, TimeSpan Time)>();
            }
        }

        // Lưu thời khóa biểu cho user
        public async Task<bool> SaveScheduleAsync(string userId, List<ScheduleItem> schedule)
        {
            try
            {
                // Chuyển đổi Color (Brush) sang string để lưu
                var serializableSchedule = schedule.Select(item => new
                {
                    item.DayOfWeek,
                    item.Period,
                    item.Subject,
                    Color = item.Color is SolidColorBrush brush ? brush.Color.ToString() : item.Color?.ToString()
                }).ToList();

                var url = GetAuthenticatedUrl($"users/{userId}/schedule.json");
                var json = JsonConvert.SerializeObject(serializableSchedule);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving schedule: {ex.Message}");
                return false;
            }
        }

        // Lấy thời khóa biểu cho user
        public async Task<List<ScheduleItem>> GetScheduleAsync(string userId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/schedule.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var rawList = JsonConvert.DeserializeObject<List<ScheduleItemRaw>>(content);
                        return rawList.Select(item => new ScheduleItem
                        {
                            DayOfWeek = item.DayOfWeek,
                            Period = item.Period,
                            Subject = item.Subject,
                            Color = (SolidColorBrush)(new BrushConverter().ConvertFromString(item.Color))
                        }).ToList();
                    }
                }
                return new List<ScheduleItem>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading schedule: {ex.Message}");
                return new List<ScheduleItem>();
            }
        }

        // Lớp phụ trợ để deserialize
        private class ScheduleItemRaw
        {
            public int DayOfWeek { get; set; }
            public int Period { get; set; }
            public string Subject { get; set; }
            public string Color { get; set; }
        }

        public async Task<bool> UpdateUserOnlineStatusAsync(string userId, bool isOnline)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/isOnline.json");
                var content = new StringContent(JsonConvert.SerializeObject(isOnline));
                var response = await _httpClient.PutAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating user online status: {ex.Message}");
                return false;
            }
        }

        // Chấp nhận lời mời kết bạn - CẢI THIỆN: Single-side accept + Acceptance marker strategy
        public async Task<bool> AcceptFriendRequestAsync(string senderId, string receiverId, string requestId)
        {
            try
            {
                Debug.WriteLine($"[AcceptFriendRequest] 🎯 STARTING FRIEND ACCEPTANCE: {senderId} -> {receiverId}");
                Debug.WriteLine($"[AcceptFriendRequest] 📋 Request ID: {requestId}");
                Debug.WriteLine($"[AcceptFriendRequest] 🕐 Current time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                // 1. Cập nhật trạng thái FriendRequest thành Accepted
                var requestUpdate = new Dictionary<string, object>
                {
                    ["status"] = "Accepted",
                    ["acceptedAt"] = DateTime.UtcNow.ToString("o")
                };
                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}/{requestId}.json");
                var content = new StringContent(JsonConvert.SerializeObject(requestUpdate), Encoding.UTF8, "application/json");

                Debug.WriteLine($"[AcceptFriendRequest] 📍 Step 1: Updating request status to Accepted...");
                Debug.WriteLine($"[AcceptFriendRequest] 🌐 URL: {url}");

                var res1 = await _httpClient.PatchAsync(url, content);
                content.Dispose();
                Debug.WriteLine($"[AcceptFriendRequest] ✅ Step 1 Result: {res1.StatusCode}");

                if (!res1.IsSuccessStatusCode)
                {
                    var error1 = await res1.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[AcceptFriendRequest] ❌ Step 1 FAILED: {error1}");
                    return false; // Fail fast nếu không update được request status
                }

                // 2. CHỈ thêm vào danh sách của receiver (người chấp nhận) - có quyền write
                var friendData = new Dictionary<string, object>
                {
                    ["status"] = "Friends",
                    ["since"] = DateTime.UtcNow.ToString("o"),
                    ["acceptedAt"] = DateTime.UtcNow.ToString("o"),
                    ["acceptedBy"] = receiverId
                };

                var urlFriendReceiver = GetAuthenticatedUrl($"friends/{receiverId}/{senderId}.json");
                var contentReceiver = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");

                Debug.WriteLine($"[AcceptFriendRequest] 📍 Step 2: Adding {senderId} to {receiverId}'s friends list...");
                Debug.WriteLine($"[AcceptFriendRequest] 🌐 URL: {urlFriendReceiver}");

                var res2 = await _httpClient.PutAsync(urlFriendReceiver, contentReceiver);
                contentReceiver.Dispose();
                Debug.WriteLine($"[AcceptFriendRequest] ✅ Step 2 Result: {res2.StatusCode}");

                if (!res2.IsSuccessStatusCode)
                {
                    var error2 = await res2.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[AcceptFriendRequest] ❌ Step 2 FAILED: {error2}");
                    // Rollback request status
                    var rollbackUpdate = new Dictionary<string, object> { ["status"] = "Pending" };
                    var rollbackContent = new StringContent(JsonConvert.SerializeObject(rollbackUpdate), Encoding.UTF8, "application/json");
                    await _httpClient.PatchAsync(url, rollbackContent);
                    rollbackContent.Dispose();
                    return false;
                }

                // 3. Tạo acceptance marker để sender có thể detect và tự thêm mình qua polling
                Debug.WriteLine($"[AcceptFriendRequest] 📍 Step 3: Creating acceptance marker for {senderId}...");
                await CreateFriendAcceptanceMarkerAsync(senderId, receiverId);
                Debug.WriteLine($"[AcceptFriendRequest] ✅ Step 3 COMPLETED: Created acceptance marker for {senderId}");

                // 4. Thông báo thay đổi cho receiver
                Debug.WriteLine($"[AcceptFriendRequest] 📍 Step 4: Notifying friends list change for {receiverId}...");
                await NotifyFriendsListChangeWithRetryAsync(receiverId);
                Debug.WriteLine($"[AcceptFriendRequest] ✅ Step 4 COMPLETED: Notified {receiverId}");

                Debug.WriteLine($"[AcceptFriendRequest] 🎉 SUCCESS: {receiverId} accepted {senderId}'s request");
                Debug.WriteLine($"[AcceptFriendRequest] 📌 {senderId} should detect acceptance marker via polling and update their friends list");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AcceptFriendRequest] Exception: {ex.Message}");
                Debug.WriteLine($"[AcceptFriendRequest] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Tạo acceptance marker để sender có thể detect qua polling (alternative to blocked notifications)
        /// </summary>
        private async Task CreateFriendAcceptanceMarkerAsync(string senderId, string receiverId)
        {
            try
            {
                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 🎯 CREATING ACCEPTANCE MARKER");
                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 👤 Sender (will receive marker): {senderId}");
                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 👤 Receiver (who accepted): {receiverId}");

                // Tạo marker trong friendRequests của sender với special status
                var markerData = new Dictionary<string, object>
                {
                    ["senderId"] = receiverId, // Reverse - ai accept
                    ["receiverId"] = senderId, // Ai được thông báo
                    ["status"] = "NotifyAccepted", // Special status để detect
                    ["acceptedAt"] = DateTime.UtcNow.ToString("o"),
                    ["markerType"] = "FriendAcceptance"
                };

                // Tạo unique marker ID
                var markerId = $"acceptance_{receiverId}_{DateTime.UtcNow.Ticks}";
                var markerUrl = GetAuthenticatedUrl($"friendRequests/{senderId}/{markerId}.json");

                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 📋 Marker ID: {markerId}");
                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 🌐 Target URL: {markerUrl}");
                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 📄 Marker data: {JsonConvert.SerializeObject(markerData)}");

                var markerContent = new StringContent(JsonConvert.SerializeObject(markerData), Encoding.UTF8, "application/json");
                var markerResponse = await _httpClient.PutAsync(markerUrl, markerContent);
                markerContent.Dispose();

                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 🌐 HTTP Response: {markerResponse.StatusCode}");

                if (markerResponse.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[CreateFriendAcceptanceMarker] ✅ SUCCESS: Marker {markerId} created for {senderId}");
                    Debug.WriteLine($"[CreateFriendAcceptanceMarker] 📌 {senderId} should detect this via polling friendRequests");
                }
                else
                {
                    var error = await markerResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[CreateFriendAcceptanceMarker] ❌ FAILED: {markerResponse.StatusCode} - {error}");
                    Debug.WriteLine($"[CreateFriendAcceptanceMarker] 🚨 CRITICAL: {senderId} will NOT be notified of acceptance!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 💥 EXCEPTION: {ex.Message}");
                Debug.WriteLine($"[CreateFriendAcceptanceMarker] 🚨 CRITICAL: Acceptance marker creation failed!");
            }
        }

        /// <summary>
        /// Tạo unfriend marker để target user có thể detect qua polling (alternative to blocked notifications)
        /// </summary>
        private async Task CreateUnfriendMarkerAsync(string removerId, string targetUserId)
        {
            try
            {
                Debug.WriteLine($"[CreateUnfriendMarker] 🎯 CREATING MARKER: {removerId} unfriended {targetUserId}");

                // Tạo marker trong friendRequests của target user với special status
                var markerData = new Dictionary<string, object>
                {
                    ["senderId"] = removerId, // Ai unfriend
                    ["receiverId"] = targetUserId, // Ai được thông báo
                    ["status"] = "NotifyUnfriend", // Special status để detect
                    ["unfriendedAt"] = DateTime.UtcNow.ToString("o"),
                    ["markerType"] = "UnfriendNotification"
                };

                // Tạo unique marker ID
                var markerId = $"unfriend_{removerId}_{DateTime.UtcNow.Ticks}";
                var markerUrl = GetAuthenticatedUrl($"friendRequests/{targetUserId}/{markerId}.json");

                Debug.WriteLine($"[CreateUnfriendMarker] 📍 Target URL: {markerUrl}");
                Debug.WriteLine($"[CreateUnfriendMarker] 📄 Marker data: {JsonConvert.SerializeObject(markerData)}");

                var markerContent = new StringContent(JsonConvert.SerializeObject(markerData), Encoding.UTF8, "application/json");
                var markerResponse = await _httpClient.PutAsync(markerUrl, markerContent);
                markerContent.Dispose();

                Debug.WriteLine($"[CreateUnfriendMarker] 🌐 HTTP response: {markerResponse.StatusCode}");

                if (markerResponse.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[CreateUnfriendMarker] ✅ SUCCESS: Marker {markerId} created for {targetUserId}");
                    Debug.WriteLine($"[CreateUnfriendMarker] 📌 {targetUserId} should detect this marker via polling friendRequests");
                }
                else
                {
                    var error = await markerResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[CreateUnfriendMarker] ❌ FAILED: {markerResponse.StatusCode} - {error}");
                    Debug.WriteLine($"[CreateUnfriendMarker] 🚨 CRITICAL: {targetUserId} will NOT be notified of unfriend!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CreateUnfriendMarker] 💥 EXCEPTION: {ex.Message}");
                Debug.WriteLine($"[CreateUnfriendMarker] 🚨 CRITICAL: Unfriend marker creation failed!");
            }
        }

        // Từ chối lời mời kết bạn - CẢI THIỆN: Better error handling và logging
        public async Task<bool> DeclineFriendRequestAsync(string receiverId, string requestId)
        {
            try
            {
                Debug.WriteLine($"[DeclineFriendRequest] Declining request {requestId} for receiver {receiverId}");

                // Validation
                if (string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(requestId))
                {
                    Debug.WriteLine($"[DeclineFriendRequest] Invalid input parameters");
                    return false;
                }

                // Cập nhật trạng thái FriendRequest thành Declined
                var requestUpdate = new Dictionary<string, object>
                {
                    ["status"] = "Declined",
                    ["declinedAt"] = DateTime.UtcNow.ToString("o")
                };

                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}/{requestId}.json");
                var content = new StringContent(JsonConvert.SerializeObject(requestUpdate), Encoding.UTF8, "application/json");
                var res = await _httpClient.PatchAsync(url, content);
                content.Dispose();

                var success = res.IsSuccessStatusCode;
                Debug.WriteLine($"[DeclineFriendRequest] Decline result: {success} ({res.StatusCode})");

                if (!success)
                {
                    var errorContent = await res.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[DeclineFriendRequest] Error: {errorContent}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeclineFriendRequest] Exception: {ex.Message}");
                Debug.WriteLine($"[DeclineFriendRequest] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Gửi lời mời kết bạn - CẢI THIỆN: Thêm better validation và error handling
        public async Task<SendFriendRequestResult> SendFriendRequestAsync(string senderId, string senderName, string receiverId, string receiverName)
        {
            try
            {
                Debug.WriteLine($"[SendFriendRequest] Starting send process: {senderId} -> {receiverId}");

                // Validation đầu vào
                if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
                {
                    Debug.WriteLine($"[SendFriendRequest] Invalid user IDs");
                    return SendFriendRequestResult.Error;
                }

                if (senderId == receiverId)
                {
                    Debug.WriteLine($"[SendFriendRequest] Cannot send friend request to self");
                    return SendFriendRequestResult.Error;
                }

                // 1. Kiểm tra đã là bạn bè chưa
                if (await AreAlreadyFriendsAsync(senderId, receiverId))
                {
                    Debug.WriteLine($"[SendFriendRequest] Users {senderId} and {receiverId} are already friends");
                    return SendFriendRequestResult.AlreadyFriends;
                }

                // 2. Kiểm tra đã có lời mời Pending chưa (cả 2 chiều)
                if (await HasPendingRequestAsync(senderId, receiverId) || await HasPendingRequestAsync(receiverId, senderId))
                {
                    Debug.WriteLine($"[SendFriendRequest] Already has pending request between {senderId} and {receiverId}");
                    return SendFriendRequestResult.HasPending;
                }

                // 3. Kiểm tra giới hạn 5 lời mời/30 phút
                if (await ExceedsRequestLimitAsync(senderId, receiverId))
                {
                    Debug.WriteLine($"[SendFriendRequest] User {senderId} exceeded request limit to {receiverId}");
                    return SendFriendRequestResult.ExceedsLimit;
                }

                // 4. Gửi lời mời kết bạn
                var requestId = $"{senderId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                var request = new
                {
                    senderId = senderId,
                    senderName = senderName,
                    receiverId = receiverId,
                    receiverName = receiverName,
                    status = "Pending",
                    sentAt = DateTime.UtcNow.ToString("o"),
                    requestId = requestId
                };

                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}/{requestId}.json");
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync(url, content);
                content.Dispose();

                if (res.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[SendFriendRequest] Request sent successfully from {senderId} to {receiverId}");
                    return SendFriendRequestResult.Success;
                }
                else
                {
                    var errorContent = await res.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[SendFriendRequest] Failed to send request: {res.StatusCode}, Error: {errorContent}");
                    return SendFriendRequestResult.Error;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendFriendRequest] Exception: {ex.Message}");
                Debug.WriteLine($"[SendFriendRequest] StackTrace: {ex.StackTrace}");
                return SendFriendRequestResult.Error;
            }
        }

        // Kiểm tra 2 user đã là bạn bè chưa
        public async Task<bool> AreAlreadyFriendsAsync(string userId1, string userId2)
        {
            try
            {
                var url = GetAuthenticatedUrl($"friends/{userId1}/{userId2}.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return !string.IsNullOrEmpty(content) && content != "null";
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Kiểm tra vượt quá giới hạn 5 lời mời/30 phút - CẢI THIỆN: Better error handling và logging
        private async Task<bool> ExceedsRequestLimitAsync(string senderId, string receiverId)
        {
            try
            {
                Debug.WriteLine($"[ExceedsRequestLimit] Checking limit for {senderId} -> {receiverId}");

                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}.json");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[ExceedsRequestLimit] HTTP Error: {response.StatusCode}");
                    return false; // Cho phép gửi nếu không check được
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    Debug.WriteLine($"[ExceedsRequestLimit] No requests found, allowing send");
                    return false;
                }

                var requests = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
                var recentRequestsCount = 0; foreach (var kv in requests)
                {
                    try
                    {
                        var request = kv.Value;
                        var requestSenderId = request.senderId?.ToString();
                        var sentAtStr = request.sentAt?.ToString();
                        DateTime sentAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(sentAtStr))
                        {
                            DateTime parsedSentAt;
                            if (DateTime.TryParse(sentAtStr, out parsedSentAt))
                            {
                                sentAt = parsedSentAt;
                            }
                        }

                        if (requestSenderId == senderId)
                        {
                            if (!string.IsNullOrEmpty(sentAtStr))
                            {
                                if (sentAt >= thirtyMinutesAgo)
                                {
                                    recentRequestsCount++;
                                    Debug.WriteLine($"[ExceedsRequestLimit] Found recent request at {sentAt} (count: {recentRequestsCount})");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[ExceedsRequestLimit] Could not parse timestamp '{sentAtStr}' for request from {senderId}");
                            }
                        }
                    }
                    catch (Exception requestEx)
                    {
                        Debug.WriteLine($"[ExceedsRequestLimit] Error processing request {kv.Key}: {requestEx.Message}");
                    }
                }

                var exceeds = recentRequestsCount >= 5;
                Debug.WriteLine($"[ExceedsRequestLimit] User {senderId} sent {recentRequestsCount}/5 requests to {receiverId} in last 30 minutes. Exceeds: {exceeds}");
                return exceeds;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExceedsRequestLimit] Exception: {ex.Message}");
                Debug.WriteLine($"[ExceedsRequestLimit] StackTrace: {ex.StackTrace}");
                return false; // Cho phép gửi nếu có lỗi trong việc check
            }
        }

        // Kiểm tra đã có lời mời Pending chưa - CẢI THIỆN: Better performance và error handling
        public async Task<bool> HasPendingRequestAsync(string senderId, string receiverId)
        {
            try
            {
                Debug.WriteLine($"[HasPendingRequest] Checking pending request: {senderId} -> {receiverId}");

                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}.json");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[HasPendingRequest] HTTP Error: {response.StatusCode}");
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    Debug.WriteLine($"[HasPendingRequest] No requests found for receiver {receiverId}");
                    return false;
                }

                var requests = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                Debug.WriteLine($"[HasPendingRequest] Found {requests.Count} requests for receiver {receiverId}");

                foreach (var kv in requests)
                {
                    try
                    {
                        var request = kv.Value;
                        var requestSenderId = request.senderId?.ToString();
                        var status = request.status?.ToString();

                        if (requestSenderId == senderId && status == "Pending")
                        {
                            Debug.WriteLine($"[HasPendingRequest] Found pending request from {senderId} to {receiverId}");
                            return true;
                        }
                    }
                    catch (Exception requestEx)
                    {
                        Debug.WriteLine($"[HasPendingRequest] Error processing request {kv.Key}: {requestEx.Message}");
                    }
                }

                Debug.WriteLine($"[HasPendingRequest] No pending request found from {senderId} to {receiverId}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HasPendingRequest] Exception: {ex.Message}");
                Debug.WriteLine($"[HasPendingRequest] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Lấy danh sách bạn bè của user từ Firebase
        public async Task<List<Friend>> GetFriendsAsync(string userId)
        {
            try
            {
                Debug.WriteLine($"[GetFriendsAsync] Loading friends for user: {userId}");
                var url = GetAuthenticatedUrl($"friends/{userId}.json");
                var response = await _httpClient.GetAsync(url);
                Debug.WriteLine($"[GetFriendsAsync] Response status: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[GetFriendsAsync] Response content: {content}");
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var friendsDict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                        var friends = new List<Friend>();
                        Debug.WriteLine($"[GetFriendsAsync] Found {friendsDict.Count} friends");
                        foreach (var kv in friendsDict)
                        {
                            string friendId = kv.Key;
                            Debug.WriteLine($"[GetFriendsAsync] Loading friend info for: {friendId}");
                            // Lấy thông tin chi tiết của bạn bè
                            var friendInfo = await GetUserByUidAsync(friendId);
                            if (friendInfo != null)
                            {
                                friendInfo.Status = FriendStatus.Friends;
                                friends.Add(friendInfo);
                            }
                        }
                        Debug.WriteLine($"[GetFriendsAsync] Total loaded friends: {friends.Count}");
                        return friends;
                    }
                    else
                    {
                        Debug.WriteLine($"[GetFriendsAsync] No friends found for user: {userId}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[GetFriendsAsync] HTTP error: {response.StatusCode}");
                }
                return new List<Friend>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetFriendsAsync] Error: {ex.Message}");
                return new List<Friend>();
            }
        }

        // Sửa chữa dữ liệu bạn bè bị thiếu từ các friendRequests đã Accepted
        public async Task<int> FixMissingFriendsDataAsync()
        {
            try
            {
                Debug.WriteLine("[FixMissingFriendsData] Starting to fix missing friends data...");
                int fixedCount = 0;

                // 1. Lấy tất cả friendRequests
                var url = GetAuthenticatedUrl("friendRequests.json");
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[FixMissingFriendsData] Failed to get friendRequests: {response.StatusCode}");
                    return 0;
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    Debug.WriteLine("[FixMissingFriendsData] No friendRequests found");
                    return 0;
                }

                var allRequests = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(content);
                Debug.WriteLine($"[FixMissingFriendsData] Found {allRequests.Count} receivers with requests");

                // 2. Duyệt qua tất cả các request đã Accepted
                foreach (var receiverData in allRequests)
                {
                    var receiverId = receiverData.Key;
                    var requests = receiverData.Value;

                    foreach (var requestData in requests)
                    {
                        var requestId = requestData.Key;
                        var request = requestData.Value;

                        var status = request.status?.ToString();
                        if (status == "Accepted")
                        {
                            var senderId = request.senderId?.ToString();
                            var sentAtStr = request.sentAt?.ToString();
                            DateTime sentAt = DateTime.UtcNow;
                            if (!string.IsNullOrEmpty(sentAtStr))
                            {
                                DateTime parsedSentAt;
                                if (DateTime.TryParse(sentAtStr, out parsedSentAt))
                                {
                                    sentAt = parsedSentAt;
                                }
                            }

                            Debug.WriteLine($"[FixMissingFriendsData] Found accepted request: {senderId} -> {receiverId}");

                            // 3. Kiểm tra xem đã có trong friends chưa
                            if (!await AreAlreadyFriendsAsync(senderId, receiverId))
                            {
                                Debug.WriteLine($"[FixMissingFriendsData] Missing friends data, fixing...");

                                // 4. Tạo dữ liệu bạn bè
                                var friendData = new Dictionary<string, object>
                                {
                                    ["status"] = "Friends",
                                    ["since"] = sentAtStr ?? DateTime.UtcNow.ToString("o")
                                };

                                var urlFriend1 = GetAuthenticatedUrl($"friends/{senderId}/{receiverId}.json");
                                var urlFriend2 = GetAuthenticatedUrl($"friends/{receiverId}/{senderId}.json");

                                var contentFriend1 = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                                var contentFriend2 = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");

                                var res1 = await _httpClient.PutAsync(urlFriend1, contentFriend1);
                                var res2 = await _httpClient.PutAsync(urlFriend2, contentFriend2);

                                contentFriend1.Dispose();
                                contentFriend2.Dispose();

                                if (res1.IsSuccessStatusCode && res2.IsSuccessStatusCode)
                                {
                                    fixedCount++;
                                    Debug.WriteLine($"[FixMissingFriendsData] Successfully fixed: {senderId} <-> {receiverId}");
                                }
                                else
                                {
                                    Debug.WriteLine($"[FixMissingFriendsData] Failed to fix: {senderId} <-> {receiverId}");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[FixMissingFriendsData] Friends data already exists: {senderId} <-> {receiverId}");
                            }
                        }
                    }
                }

                Debug.WriteLine($"[FixMissingFriendsData] Fixed {fixedCount} missing friends relationships");
                return fixedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FixMissingFriendsData] Error: {ex.Message}");
                Debug.WriteLine($"[FixMissingFriendsData] StackTrace: {ex.StackTrace}");
                return 0;
            }
        }

        // Test Firebase Database Rules cho node friends
        public async Task<bool> TestFriendsPermissionAsync()
        {
            try
            {
                var currentUserId = AuthService.GetUserId();
                var testData = new Dictionary<string, object> { ["test"] = "permission_check" };
                var url = GetAuthenticatedUrl($"friends/{currentUserId}/test.json");
                var content = new StringContent(JsonConvert.SerializeObject(testData), Encoding.UTF8, "application/json");

                Debug.WriteLine($"[TestFriendsPermission] Testing write permission to: {url}");
                var response = await _httpClient.PutAsync(url, content);
                content.Dispose();

                Debug.WriteLine($"[TestFriendsPermission] Result: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[TestFriendsPermission] Error: {error}");
                }

                // Xóa dữ liệu test nếu thành công
                if (response.IsSuccessStatusCode)
                {
                    var deleteUrl = GetAuthenticatedUrl($"friends/{currentUserId}/test.json");
                    await _httpClient.DeleteAsync(deleteUrl);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TestFriendsPermission] Exception: {ex.Message}");
                return false;
            }
        }

        // Test lấy danh sách tất cả user để kiểm tra dữ liệu
        public async Task<List<string>> GetAllUserIdsAsync()
        {
            try
            {
                Debug.WriteLine("[GetAllUserIds] Getting all user IDs from Firebase...");
                var url = GetAuthenticatedUrl("users.json");
                var response = await _httpClient.GetAsync(url);

                Debug.WriteLine($"[GetAllUserIds] Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[GetAllUserIds] Response content length: {content?.Length ?? 0}");

                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var users = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(content);
                        var userIds = users.Keys.ToList();

                        Debug.WriteLine($"[GetAllUserIds] Found {userIds.Count} users:");
                        foreach (var uid in userIds.Take(10)) // Log only first 10 for brevity
                        {
                            var username = users[uid]["username"]?.ToString() ?? "No username";
                            Debug.WriteLine($"[GetAllUserIds] - {uid}: {username}");
                        }

                        if (userIds.Count > 10)
                        {
                            Debug.WriteLine($"[GetAllUserIds] ... and {userIds.Count - 10} more users");
                        }

                        return userIds;
                    }
                    else
                    {
                        Debug.WriteLine("[GetAllUserIds] Empty or null response content");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[GetAllUserIds] HTTP Error: {response.StatusCode}, Content: {errorContent}");
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetAllUserIds] Exception: {ex.Message}");
                Debug.WriteLine($"[GetAllUserIds] StackTrace: {ex.StackTrace}");
                return new List<string>();
            }
        }

        // Đồng bộ dữ liệu user từ private sang public để cho phép tìm kiếm
        public async Task<bool> SyncUserToPublicAsync(string userId)
        {
            try
            {
                Debug.WriteLine($"[SyncUserToPublic] Syncing user {userId} to publicUsers");

                // 1. Lấy dữ liệu từ users (private)
                var privateUrl = GetAuthenticatedUrl($"users/{userId}.json");
                var response = await _httpClient.GetAsync(privateUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[SyncUserToPublic] Failed to get private data: {response.StatusCode}");
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    Debug.WriteLine("[SyncUserToPublic] No private data found");
                    return false;
                }

                var userData = JObject.Parse(content);

                // 2. Tạo dữ liệu public (chỉ các trường an toàn)
                var publicData = new
                {
                    username = userData["username"]?.ToString() ?? userId,
                    avatarUrl = userData["avatarUrl"]?.ToString() ?? "/Images/avatar1.svg",
                    email = userData["email"]?.ToString() ?? "" // Có thể bỏ email nếu muốn bảo mật hơn
                };

                // 3. Lưu vào publicUsers
                var publicUrl = GetAuthenticatedUrl($"publicUsers/{userId}.json");
                var json = JsonConvert.SerializeObject(publicData);
                var putContent = new StringContent(json, Encoding.UTF8, "application/json");

                var putResponse = await _httpClient.PutAsync(publicUrl, putContent);
                putContent.Dispose();

                var success = putResponse.IsSuccessStatusCode;
                Debug.WriteLine($"[SyncUserToPublic] Sync result: {success} ({putResponse.StatusCode})");

                if (!success)
                {
                    var error = await putResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[SyncUserToPublic] Error: {error}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SyncUserToPublic] Exception: {ex.Message}");
                return false;
            }
        }

        // Đồng bộ tất cả user hiện tại sang publicUsers
        public async Task<int> SyncAllUsersToPublicAsync()
        {
            try
            {
                Debug.WriteLine("[SyncAllUsersToPublic] Starting sync all users...");
                var userIds = await GetAllUserIdsAsync();
                var syncedCount = 0;

                foreach (var userId in userIds)
                {
                    if (await SyncUserToPublicAsync(userId))
                    {
                        syncedCount++;
                    }

                    // Delay nhỏ để tránh spam requests
                    await Task.Delay(100);
                }

                Debug.WriteLine($"[SyncAllUsersToPublic] Synced {syncedCount}/{userIds.Count} users");
                return syncedCount;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SyncAllUsersToPublic] Exception: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Hủy kết bạn - CẢI THIỆN: Single-side remove + Unfriend marker strategy
        /// </summary>
        public async Task<bool> RemoveFriendAsync(string userId1, string userId2)
        {
            try
            {
                Debug.WriteLine($"[RemoveFriendAsync] 🔥 STARTING UNFRIEND: {userId1} removing {userId2}");
                Debug.WriteLine($"[RemoveFriendAsync] Current time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

                // 1. CHỈ xóa khỏi danh sách bạn bè của user hiện tại (userId1) - có quyền write
                var friendsUrl = GetAuthenticatedUrl($"friends/{userId1}/{userId2}.json");
                Debug.WriteLine($"[RemoveFriendAsync] DELETE friends url: {friendsUrl}");

                var friendsRes = await _httpClient.DeleteAsync(friendsUrl);
                Debug.WriteLine($"[RemoveFriendAsync] DELETE friends result: {friendsRes.StatusCode}");

                if (!friendsRes.IsSuccessStatusCode)
                {
                    var error = await friendsRes.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[RemoveFriendAsync] ❌ Failed to remove {userId2} from {userId1}'s friends list: {error}");
                    return false;
                }

                Debug.WriteLine($"[RemoveFriendAsync] ✅ Step 1 SUCCESS: Removed {userId2} from {userId1}'s friends list");

                // 2. Tạo unfriend marker để userId2 có thể detect và tự xóa mình
                Debug.WriteLine($"[RemoveFriendAsync] 📍 Step 2: Creating unfriend marker for {userId2}...");
                await CreateUnfriendMarkerAsync(userId1, userId2);
                Debug.WriteLine($"[RemoveFriendAsync] ✅ Step 2 COMPLETED: Created unfriend marker for {userId2}");

                // 2b. Thông báo thay đổi cho user bị hủy kết bạn (đồng bộ UI)
                Debug.WriteLine($"[RemoveFriendAsync] 🔄 Step 2b: Notifying friends list change for {userId2}...");
                await NotifyFriendsListChangeWithRetryAsync(userId2);
                Debug.WriteLine($"[RemoveFriendAsync] ✅ Step 2b COMPLETED: Notified friends list change for {userId2}");

                // 3. Xóa tất cả friendRequests liên quan (cả 2 chiều) để tránh auto-re-add
                Debug.WriteLine($"[RemoveFriendAsync] 🧹 Step 3: Cleaning up friend requests...");
                await CleanupFriendRequestsAsync(userId1, userId2);
                await CleanupFriendRequestsAsync(userId2, userId1);
                Debug.WriteLine($"[RemoveFriendAsync] ✅ Step 3 COMPLETED: Cleaned up friend requests");

                // 4. Thông báo thay đổi cho user hiện tại
                Debug.WriteLine($"[RemoveFriendAsync] 🔄 Step 4: Notifying friends list change for {userId1}...");
                await NotifyFriendsListChangeWithRetryAsync(userId1);
                Debug.WriteLine($"[RemoveFriendAsync] ✅ Step 4 COMPLETED: Notified friends list change");

                Debug.WriteLine($"[RemoveFriendAsync] 🎉 ALL STEPS COMPLETED: {userId1} unfriended {userId2}");
                Debug.WriteLine($"[RemoveFriendAsync] 📌 {userId2} should detect unfriend marker via polling");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RemoveFriendAsync] Exception: {ex.Message}");
                Debug.WriteLine($"[RemoveFriendAsync] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Cleanup friendRequests giữa 2 users để tránh auto-re-add sau khi unfriend
        /// </summary>
        private async Task CleanupFriendRequestsAsync(string fromUserId, string toUserId)
        {
            try
            {
                Debug.WriteLine($"[CleanupFriendRequestsAsync] Cleaning up requests from {fromUserId} to {toUserId}");

                // Lấy tất cả requests của toUserId
                var url = GetAuthenticatedUrl($"friendRequests/{toUserId}.json");
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var requests = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                        Debug.WriteLine($"[CleanupFriendRequestsAsync] Found {requests.Count} requests for {toUserId}");

                        // Tìm và xóa tất cả requests từ fromUserId
                        foreach (var kv in requests)
                        {
                            var requestId = kv.Key;
                            var request = kv.Value;
                            var senderId = request.senderId?.ToString();
                            var status = request.status?.ToString();

                            if (senderId == fromUserId)
                            {
                                Debug.WriteLine($"[CleanupFriendRequestsAsync] Found request {requestId} from {senderId} with status {status}, deleting...");

                                var deleteUrl = GetAuthenticatedUrl($"friendRequests/{toUserId}/{requestId}.json");
                                var deleteResponse = await _httpClient.DeleteAsync(deleteUrl);

                                Debug.WriteLine($"[CleanupFriendRequestsAsync] Delete request result: {deleteResponse.StatusCode}");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[CleanupFriendRequestsAsync] No requests found for {toUserId}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[CleanupFriendRequestsAsync] Failed to get requests for {toUserId}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CleanupFriendRequestsAsync] Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Thông báo thay đổi trong danh sách bạn bè để cập nhật real-time
        /// </summary>
        public async Task<bool> NotifyFriendsListChangeAsync(string userId)
        {
            try
            {
                Debug.WriteLine($"[NotifyFriendsListChange] Notifying friends list change for user: {userId}");

                // Kiểm tra token trước
                var token = AuthService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    Debug.WriteLine($"[NotifyFriendsListChange] Error: No authentication token for user {userId}");
                    return false;
                }

                // Tạo một marker thời gian để thông báo thay đổi
                var changeMarker = new Dictionary<string, object>
                {
                    ["lastUpdated"] = DateTime.UtcNow.ToString("o"),
                    ["userId"] = userId
                };

                var url = GetAuthenticatedUrl($"friendsListChanges/{userId}.json");
                Debug.WriteLine($"[NotifyFriendsListChange] URL: {url}");

                var content = new StringContent(JsonConvert.SerializeObject(changeMarker), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, content);
                content.Dispose();

                var success = response.IsSuccessStatusCode;
                Debug.WriteLine($"[NotifyFriendsListChange] Response status: {response.StatusCode}");

                if (!success)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[NotifyFriendsListChange] Error response: {errorContent}");
                }

                Debug.WriteLine($"[NotifyFriendsListChange] Notification result: {success}");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotifyFriendsListChange] Exception: {ex.Message}");
                Debug.WriteLine($"[NotifyFriendsListChange] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem có thay đổi nào trong danh sách bạn bè không
        /// </summary>
        public async Task<bool> HasFriendsListChangedAsync(string userId, DateTime lastCheck)
        {
            try
            {
                var url = GetAuthenticatedUrl($"friendsListChanges/{userId}.json");
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var changeData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                        if (changeData.ContainsKey("lastUpdated"))
                        {
                            var lastUpdatedStr = changeData["lastUpdated"].ToString();
                            if (DateTime.TryParse(lastUpdatedStr, out DateTime lastUpdated))
                            {
                                return lastUpdated > lastCheck;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HasFriendsListChanged] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Thông báo thay đổi với retry mechanism để đảm bảo thông báo được gửi
        /// </summary>
        private async Task NotifyFriendsListChangeWithRetryAsync(string userId, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] Attempt {attempt} for user: {userId}");

                    // Kiểm tra kết nối internet trước
                    if (!await CheckInternetConnectionAsync())
                    {
                        Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] No internet connection on attempt {attempt}");
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(2000); // Delay dài hơn nếu không có internet
                            continue;
                        }
                    }

                    var success = await NotifyFriendsListChangeAsync(userId);
                    if (success)
                    {
                        Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] Successfully notified user {userId} on attempt {attempt}");
                        return;
                    }
                    else
                    {
                        Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] Failed to notify user {userId} on attempt {attempt}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] Exception on attempt {attempt} for user {userId}: {ex.Message}");
                    Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] Stack trace: {ex.StackTrace}");
                }

                // Delay trước khi retry (exponential backoff)
                if (attempt < maxRetries)
                {
                    var delay = Math.Min(1000 * attempt, 3000); // 1s, 2s, 3s
                    Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] Waiting {delay}ms before retry...");
                    await Task.Delay(delay);
                }
            }

            Debug.WriteLine($"[NotifyFriendsListChangeWithRetry] Failed to notify user {userId} after {maxRetries} attempts");
        }

        /// <summary>
        /// Test notification để debug
        /// </summary>
        public async Task<bool> TestNotificationAsync(string userId)
        {
            try
            {
                Debug.WriteLine($"[TestNotification] Testing notification for user: {userId}");

                // Kiểm tra token
                var token = AuthService.GetToken();
                Debug.WriteLine($"[TestNotification] Token: {(string.IsNullOrEmpty(token) ? "NULL" : "EXISTS")}");

                // Test URL
                var url = GetAuthenticatedUrl($"friendsListChanges/{userId}.json");
                Debug.WriteLine($"[TestNotification] Test URL: {url}");

                // Test GET request trước
                var getResponse = await _httpClient.GetAsync(url);
                Debug.WriteLine($"[TestNotification] GET response: {getResponse.StatusCode}");

                if (!getResponse.IsSuccessStatusCode)
                {
                    var getError = await getResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[TestNotification] GET error: {getError}");
                }

                // Test PUT request
                var testData = new Dictionary<string, object>
                {
                    ["test"] = true,
                    ["timestamp"] = DateTime.UtcNow.ToString("o")
                };

                var content = new StringContent(JsonConvert.SerializeObject(testData), Encoding.UTF8, "application/json");
                var putResponse = await _httpClient.PutAsync(url, content);
                content.Dispose();

                Debug.WriteLine($"[TestNotification] PUT response: {putResponse.StatusCode}");

                if (!putResponse.IsSuccessStatusCode)
                {
                    var putError = await putResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[TestNotification] PUT error: {putError}");
                }

                var success = putResponse.IsSuccessStatusCode;
                Debug.WriteLine($"[TestNotification] Test result: {success}");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TestNotification] Exception: {ex.Message}");
                Debug.WriteLine($"[TestNotification] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// DEPRECATED: Gửi notification hủy kết bạn cho user kia (Firebase có thể block endpoint này)
        /// Replaced by CreateUnfriendMarkerAsync + polling detection
        /// </summary>
        [Obsolete("Use CreateUnfriendMarkerAsync instead for Firebase security compliance")]
        public Task NotifyUnfriendAsync(string fromUserId, string toUserId)
        {
            // Không sử dụng nữa - replaced by marker system
            Debug.WriteLine($"[NotifyUnfriendAsync] ⚠️ DEPRECATED: Use CreateUnfriendMarkerAsync instead");
            return Task.CompletedTask;
        }

        /// <summary>
        /// DEPRECATED: Polling kiểm tra notification hủy kết bạn (Firebase có thể block unfriendNotifications endpoint)
        /// Replaced by unified SyncFromAcceptedRequestsAsync with unfriend marker detection
        /// </summary>
        [Obsolete("Unfriend detection now handled by SyncFromAcceptedRequestsAsync with markers")]
        public Task CheckUnfriendNotificationsAsync(string myUserId)
        {
            // Không sử dụng nữa - unfriend detection được handle trong SyncFromAcceptedRequestsAsync
            Debug.WriteLine($"[CheckUnfriendNotificationsAsync] ⚠️ DEPRECATED: Unfriend detection now in SyncFromAcceptedRequestsAsync");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gửi notification chấp nhận kết bạn cho user kia
        /// </summary>
        public async Task NotifyFriendAcceptedAsync(string fromUserId, string toUserId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"friendAcceptedNotifications/{toUserId}/{fromUserId}.json");
                var data = new { timestamp = DateTime.UtcNow.ToString("o") };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, content);
                content.Dispose();
                System.Diagnostics.Debug.WriteLine($"[NotifyFriendAcceptedAsync] Sent friend accepted notification from {fromUserId} to {toUserId}: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotifyFriendAcceptedAsync] Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Polling kiểm tra notification chấp nhận kết bạn và tự động thêm bạn vào danh sách của mình
        /// DISABLED: Firebase security rules block friendAcceptedNotifications endpoint
        /// </summary>
        public Task CheckFriendAcceptedNotificationsAsync(string myUserId)
        {
            try
            {
                // SKIP: Firebase blocks friendAcceptedNotifications endpoint
                System.Diagnostics.Debug.WriteLine($"[CheckFriendAcceptedNotificationsAsync] ⚠️ SKIPPED: Firebase blocks friendAcceptedNotifications endpoint");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckFriendAcceptedNotificationsAsync] Exception: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Force sync toàn bộ friend list - gọi khi cần đồng bộ ngay lập tức
        /// </summary>
        public async Task<string> ForceSyncAllFriendsAsync(string myUserId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ForceSyncAllFriends] Starting force sync for {myUserId}");

                var report = new List<string>();
                var totalChanges = 0;

                // Clear cache để force sync
                _syncCache.Clear();

                // Phase 1: Sync from accepted requests
                var changes1 = await SyncFromAcceptedRequestsAsync(myUserId);
                if (changes1)
                {
                    totalChanges++;
                    report.Add("✅ Synced from accepted requests");
                }

                // Phase 2: Bidirectional sync
                var changes2 = await SyncBidirectionalFriendsAsync(myUserId);
                if (changes2)
                {
                    totalChanges++;
                    report.Add("✅ Fixed bidirectional links");
                }

                // Phase 3: Cross-check consistency
                var changes3 = await CrossCheckFriendsConsistencyAsync(myUserId);
                if (changes3)
                {
                    totalChanges++;
                    report.Add("✅ Cross-synced missing friends");
                }

                // Phase 4: Bidirectional unfriend cleanup
                var changes4 = await SyncBidirectionalUnfriendAsync(myUserId);
                if (changes4)
                {
                    totalChanges++;
                    report.Add("✅ Cleaned up bidirectional unfriends");
                }

                // Phase 5: Fix missing friends data from all accepted requests
                var fixedCount = await FixMissingFriendsDataAsync();
                if (fixedCount > 0)
                {
                    totalChanges++;
                    report.Add($"✅ Fixed {fixedCount} missing friend relationships");
                }

                // Force UI reload
                if (totalChanges > 0 && NotificationVM != null)
                {
                    NotificationVM.TriggerFriendsListReload();
                    report.Add("🔄 UI reloaded");
                }

                var summary = totalChanges > 0 ?
                    $"🎉 Force sync completed! {totalChanges} improvements made:\n" + string.Join("\n", report) :
                    "✨ Friend list is already in perfect sync!";

                System.Diagnostics.Debug.WriteLine($"[ForceSyncAllFriends] {summary}");
                return summary;
            }
            catch (Exception ex)
            {
                var error = $"❌ Force sync failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[ForceSyncAllFriends] {error}");
                return error;
            }
        }

        /// <summary>
        /// Kiểm tra và đồng bộ danh sách bạn bè khi polling - ENHANCED VERSION với Acceptance Markers
        /// Focus vào Phase 1 với acceptance marker detection để notify sender
        /// </summary>
        public async Task CheckAndSyncFriendsListAsync(string myUserId)
        {
            try
            {
                // Simplified debouncing - chỉ tránh spam trong cùng 1 giây
                var cacheKey = $"sync_{myUserId}_{DateTime.UtcNow.Second}";
                if (_syncCache.Contains(cacheKey))
                {
                    return;
                }
                _syncCache.Add(cacheKey);

                // Cleanup cache mỗi 30 giây
                if (DateTime.UtcNow.Second % 30 == 0)
                {
                    _syncCache.Clear();
                    // Cleanup processed markers cache để tránh memory leak
                    if (_processedMarkers.Count > 100)
                    {
                        _processedMarkers.Clear();
                        System.Diagnostics.Debug.WriteLine($"[CheckAndSyncFriendsListAsync] 🧹 Cleaned up processed markers cache");
                    }
                }

                var hasChanges = false;
                System.Diagnostics.Debug.WriteLine($"[CheckAndSyncFriendsListAsync] 🔥 ENHANCED sync for {myUserId} - with acceptance markers");

                // === ONLY PHASE 1: Sync từ Accepted Requests + Acceptance Markers ===
                // Phase này bây giờ có thể detect khi ai đó accept request của mình
                hasChanges |= await SyncFromAcceptedRequestsAsync(myUserId);

                // === SKIP Phase 2, 3, 4 vì cần bulk user scanning (bị block) ===
                System.Diagnostics.Debug.WriteLine($"[CheckAndSyncFriendsListAsync] ⚠️ Skipping Phase 2-4 due to Firebase security restrictions");
                System.Diagnostics.Debug.WriteLine($"[CheckAndSyncFriendsListAsync] 📌 Phase 1 enhanced with acceptance markers for sender notifications");

                // Reload UI if any changes
                if (hasChanges)
                {
                    System.Diagnostics.Debug.WriteLine($"[CheckAndSyncFriendsListAsync] ✅ Changes detected, triggering UI reload for {myUserId}");
                    if (NotificationVM != null)
                    {
                        NotificationVM.TriggerFriendsListReload();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[CheckAndSyncFriendsListAsync] ℹ️ No changes detected for {myUserId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckAndSyncFriendsListAsync] Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// PHASE 1: Đồng bộ từ các requests đã Accepted - ENHANCED VERSION với Acceptance Markers
        /// Chỉ focus vào những gì work được: individual friendRequests endpoints + acceptance markers
        /// </summary>
        private async Task<bool> SyncFromAcceptedRequestsAsync(string myUserId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ENHANCED: Checking accepted requests for {myUserId}");

                var hasChanges = false;

                // === 1. Check requests TO me (received) ===
                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] Checking requests TO {myUserId}");
                var toMeUrl = GetAuthenticatedUrl($"friendRequests/{myUserId}.json");
                var toMeResponse = await _httpClient.GetAsync(toMeUrl);

                if (toMeResponse.IsSuccessStatusCode)
                {
                    var toMeContent = await toMeResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(toMeContent) && toMeContent != "null")
                    {
                        var toMeRequests = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(toMeContent);
                        System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 📨 Found {toMeRequests.Count} requests/markers TO {myUserId}");

                        foreach (var kv in toMeRequests)
                        {
                            var requestId = kv.Key;
                            var request = kv.Value;
                            var status = request.status?.ToString();
                            var senderId = request.senderId?.ToString();
                            var markerType = request.markerType?.ToString();

                            System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🔍 Processing: {requestId} | status={status} | sender={senderId} | type={markerType}");

                            // Cache user ID khi tìm thấy
                            if (!string.IsNullOrEmpty(senderId))
                            {
                                _userIdCache.Add(senderId);
                            }

                            // === CASE 1: Normal Accepted Request (RECEIVER SIDE) ===
                            if (status == "Accepted" && !string.IsNullOrEmpty(senderId) && markerType != "FriendAcceptance")
                            {
                                // Kiểm tra đã có trong friends chưa
                                if (!await AreAlreadyFriendsAsync(myUserId, senderId))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ⭐ Found ACCEPTED request: {senderId} -> {myUserId}, adding friendship (RECEIVER SIDE)");

                                    // Chỉ add friend cho receiver (người accept), KHÔNG show notification
                                    // Notification sẽ được hiển thị ở CASE 2 cho sender
                                    if (await AddSingleSideFriendshipAsync(myUserId, senderId))
                                    {
                                        hasChanges = true;
                                        System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ✅ SUCCESS: Added {senderId} as friend for {myUserId} (RECEIVER SIDE)");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ℹ️ Already friends: {myUserId} <-> {senderId}");
                                }
                            }
                            // === CASE 2: Friend Acceptance Marker (SENDER SIDE) ===
                            else if (status == "NotifyAccepted" && markerType == "FriendAcceptance")
                            {
                                var accepterId = senderId; // senderId in marker = who accepted
                                var markerKey = $"accept_{accepterId}_{myUserId}_{requestId}";

                                // Kiểm tra đã process marker này chưa để tránh duplicate
                                if (_processedMarkers.Contains(markerKey))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ⚠️ SKIP: Already processed acceptance marker {requestId}");
                                    continue;
                                }
                                _processedMarkers.Add(markerKey);

                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🎉 Found ACCEPTANCE MARKER: {accepterId} accepted my request! (SENDER SIDE)");

                                // Add friendship cho sender (Firebase chỉ cho phép write vào friends của chính mình)
                                if (!await AreAlreadyFriendsAsync(myUserId, accepterId))
                                {
                                    var friendData = new Dictionary<string, object>
                                    {
                                        ["status"] = "Friends",
                                        ["since"] = DateTime.UtcNow.ToString("o"),
                                        ["acceptedAt"] = DateTime.UtcNow.ToString("o"),
                                        ["acceptedBy"] = accepterId
                                    };

                                    var friendUrl = GetAuthenticatedUrl($"friends/{myUserId}/{accepterId}.json");
                                    var friendContent = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                                    var friendResponse = await _httpClient.PutAsync(friendUrl, friendContent);
                                    friendContent.Dispose();

                                    if (friendResponse.IsSuccessStatusCode)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ✅ Added {accepterId} to my friends list (SENDER SIDE)");
                                        hasChanges = true;

                                        // Trigger friends list reload for sender
                                        if (NotificationVM != null)
                                        {
                                            NotificationVM.TriggerFriendsListReload();
                                        }
                                    }
                                    else
                                    {
                                        var error = await friendResponse.Content.ReadAsStringAsync();
                                        System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ❌ Failed to add friend: {error}");
                                    }
                                }

                                // Show notification for successful acceptance (CHỈ CHO SENDER)
                                if (NotificationVM != null)
                                {
                                    var friend = await GetUserByUidAsync(accepterId);
                                    var friendName = friend?.Name ?? accepterId;
                                    NotificationVM.AddFriendAcceptedNotification(accepterId, friendName);
                                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 📱 Showed acceptance notification: {friendName} accepted your request (SENDER SIDE)");
                                }

                                // Xóa marker sau khi process
                                var deleteMarkerUrl = GetAuthenticatedUrl($"friendRequests/{myUserId}/{requestId}.json");
                                await _httpClient.DeleteAsync(deleteMarkerUrl);
                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🗑️ Cleaned up acceptance marker {requestId}");
                            }
                            // === CASE 3: Unfriend Marker (SOMEONE REMOVED ME) ===
                            else if (status == "NotifyUnfriend" && markerType == "UnfriendNotification")
                            {
                                var removerId = senderId; // senderId in marker = who removed me
                                var markerKey = $"unfriend_{removerId}_{myUserId}_{requestId}";

                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🔥 DETECTED UNFRIEND MARKER! {removerId} unfriended {myUserId}");
                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 📋 Marker details: requestId={requestId}, removerId={removerId}");

                                // Kiểm tra đã process marker này chưa để tránh duplicate
                                if (_processedMarkers.Contains(markerKey))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ⚠️ SKIP: Already processed unfriend marker {requestId}");
                                    continue;
                                }
                                _processedMarkers.Add(markerKey);

                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 💔 PROCESSING UNFRIEND: {removerId} removed me from friends!");

                                // DEBUG: Check current friends list status
                                var isFriend = await AreAlreadyFriendsAsync(myUserId, removerId);
                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🔍 DEBUG: Is {removerId} still in my friends? {isFriend}");

                                // Tự xóa removerId khỏi friends list của mình
                                if (await AreAlreadyFriendsAsync(myUserId, removerId))
                                {
                                    var removeFriendUrl = GetAuthenticatedUrl($"friends/{myUserId}/{removerId}.json");
                                    var removeResponse = await _httpClient.DeleteAsync(removeFriendUrl);

                                    if (removeResponse.IsSuccessStatusCode)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ✅ Removed {removerId} from my friends list");
                                        hasChanges = true;

                                        // FORCE UI refresh ngay lập tức để user thấy friend đã bị xóa
                                        if (NotificationVM != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🔄 Force refreshing friends list after unfriend");
                                            NotificationVM.TriggerFriendsListReload();
                                        }
                                    }
                                    else
                                    {
                                        var error = await removeResponse.Content.ReadAsStringAsync();
                                        System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ❌ Failed to remove friend: {error}");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ⚠️ {removerId} was not in friends list (already removed?)");
                                }

                                // Cleanup friendRequests để tránh auto-re-add
                                await CleanupFriendRequestsAsync(removerId, myUserId);
                                await CleanupFriendRequestsAsync(myUserId, removerId);
                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🧹 Cleaned up friend requests with {removerId}");

                                // Show notification for unfriend AFTER removing from friends list
                                if (NotificationVM != null)
                                {
                                    var friend = await GetUserByUidAsync(removerId);
                                    var friendName = friend?.Name ?? removerId;

                                    // Delay nhỏ để đảm bảo UI đã refresh friends list trước khi hiện notification
                                    await Task.Delay(500);

                                    NotificationVM.AddUnfriendNotification(removerId, friendName);
                                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 📱 Showed unfriend notification: {friendName} removed you from friends (after UI refresh)");
                                }

                                // Xóa marker sau khi process
                                var deleteUnfriendMarkerUrl = GetAuthenticatedUrl($"friendRequests/{myUserId}/{requestId}.json");
                                await _httpClient.DeleteAsync(deleteUnfriendMarkerUrl);
                                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🗑️ Cleaned up unfriend marker {requestId}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 📭 No requests/markers found for {myUserId} (content: {toMeContent?.Substring(0, Math.Min(50, toMeContent.Length))})");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🚨 Failed to get requests for {myUserId}: {toMeResponse.StatusCode}");
                    var errorContent = await toMeResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] 🚨 Error details: {errorContent}");
                }

                // === 2. SKIP scanning FROM me vì tất cả bulk endpoints đều bị block ===
                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ⚠️ Skipping FROM-me scanning due to Firebase restrictions");

                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] ENHANCED scan completed, hasChanges: {hasChanges}");
                return hasChanges;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncFromAcceptedRequests] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 2: Đồng bộ 2 chiều - nếu A có B làm bạn thì B cũng phải có A
        /// </summary>
        private async Task<bool> SyncBidirectionalFriendsAsync(string myUserId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalFriends] Checking bidirectional sync for {myUserId}");

                var hasChanges = false;
                var myFriends = await GetFriendsAsync(myUserId);

                foreach (var friend in myFriends)
                {
                    // Kiểm tra xem friend có mình trong danh sách không
                    if (!await AreAlreadyFriendsAsync(friend.Id, myUserId))
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalFriends] Missing bidirectional link: {friend.Id} doesn't have {myUserId}");

                        // Thêm mình vào danh sách của friend
                        var friendData = new Dictionary<string, object>
                        {
                            ["status"] = "Friends",
                            ["since"] = DateTime.UtcNow.ToString("o"),
                            ["syncedAt"] = DateTime.UtcNow.ToString("o")
                        };

                        var url = GetAuthenticatedUrl($"friends/{friend.Id}/{myUserId}.json");
                        var content = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                        var response = await _httpClient.PutAsync(url, content);
                        content.Dispose();

                        if (response.IsSuccessStatusCode)
                        {
                            hasChanges = true;
                            System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalFriends] Fixed bidirectional link: Added {myUserId} to {friend.Id}'s friends");
                        }
                    }
                }

                return hasChanges;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalFriends] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 3: Cross-check với other users để tìm missing friends
        /// </summary>
        private async Task<bool> CrossCheckFriendsConsistencyAsync(string myUserId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] Cross-checking friends consistency for {myUserId}");

                var hasChanges = false;

                // Lấy sample users thông qua GetLimitedUserIdsAsync (có nhiều fallback strategies)
                var sampleUsers = await GetLimitedUserIdsAsync(10); // Limit để tránh quá tải

                if (sampleUsers.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] ⚠️ No users available for cross-check, skipping phase");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] Cross-checking with {sampleUsers.Count} users");

                foreach (var otherUserId in sampleUsers)
                {
                    if (otherUserId == myUserId) continue;

                    try
                    {
                        // Kiểm tra xem user khác có mình trong friends list không
                        if (await AreAlreadyFriendsAsync(otherUserId, myUserId))
                        {
                            // Nếu họ có mình nhưng mình không có họ
                            if (!await AreAlreadyFriendsAsync(myUserId, otherUserId))
                            {
                                System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] Found missing friend: {otherUserId} has {myUserId} but not vice versa");

                                // Thêm họ vào danh sách của mình
                                var friendData = new Dictionary<string, object>
                                {
                                    ["status"] = "Friends",
                                    ["since"] = DateTime.UtcNow.ToString("o"),
                                    ["crossSyncedAt"] = DateTime.UtcNow.ToString("o")
                                };

                                var url = GetAuthenticatedUrl($"friends/{myUserId}/{otherUserId}.json");
                                var content = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                                var response = await _httpClient.PutAsync(url, content);
                                content.Dispose();

                                if (response.IsSuccessStatusCode)
                                {
                                    hasChanges = true;
                                    System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] Cross-synced: Added {otherUserId} to {myUserId}'s friends");

                                    // NOTE: KHÔNG hiển thị notification ở đây vì đây chỉ là sync recovery, 
                                    // không phải acceptance event thực sự từ user
                                    System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] ℹ️ No notification shown - this is sync recovery, not real acceptance");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] Error checking {otherUserId}: {ex.Message}");
                        // Continue với user khác
                    }
                }

                return hasChanges;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrossCheckFriendsConsistency] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 4: Bidirectional Unfriend Cleanup - Đồng bộ hủy kết bạn 2 chiều
        /// Nếu A không có B nhưng B vẫn có A, thì xóa A khỏi danh sách của B
        /// </summary>
        private async Task<bool> SyncBidirectionalUnfriendAsync(string myUserId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Checking bidirectional unfriend sync for {myUserId}");

                var hasChanges = false;

                // Lấy danh sách users thông qua GetLimitedUserIdsAsync (có nhiều fallback strategies)
                var sampleUsers = await GetLimitedUserIdsAsync(15); // Limit để tránh quá tải

                if (sampleUsers.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] ⚠️ No users available for unfriend cleanup, skipping phase");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Checking unfriend sync with {sampleUsers.Count} users");

                foreach (var otherUserId in sampleUsers)
                {
                    if (otherUserId == myUserId) continue;

                    try
                    {
                        // Case 1: Nếu user khác có mình làm bạn nhưng mình không có họ
                        // → Có thể mình đã unfriend họ, cần sync lại
                        if (await AreAlreadyFriendsAsync(otherUserId, myUserId) &&
                            !await AreAlreadyFriendsAsync(myUserId, otherUserId))
                        {
                            // Kiểm tra xem có phải do unfriend không bằng cách check friendRequests
                            var hasActiveRequest = await HasActiveRequestBetweenUsersAsync(myUserId, otherUserId);

                            if (!hasActiveRequest)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Detected unfriend: {myUserId} unfriended {otherUserId}, removing bidirectional link");

                                // Xóa mình khỏi danh sách của user kia
                                var unfriendUrl = GetAuthenticatedUrl($"friends/{otherUserId}/{myUserId}.json");
                                var unfriendResponse = await _httpClient.DeleteAsync(unfriendUrl);

                                if (unfriendResponse.IsSuccessStatusCode)
                                {
                                    hasChanges = true;
                                    System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Successfully removed {myUserId} from {otherUserId}'s friends list");

                                    // Cleanup friendRequests liên quan
                                    await CleanupFriendRequestsAsync(myUserId, otherUserId);
                                    await CleanupFriendRequestsAsync(otherUserId, myUserId);
                                }
                            }
                        }

                        // Case 2: Nếu mình có user khác làm bạn nhưng họ không có mình
                        // → Có thể họ đã unfriend mình, cần sync lại
                        if (await AreAlreadyFriendsAsync(myUserId, otherUserId) &&
                            !await AreAlreadyFriendsAsync(otherUserId, myUserId))
                        {
                            var hasActiveRequest = await HasActiveRequestBetweenUsersAsync(otherUserId, myUserId);

                            if (!hasActiveRequest)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Detected unfriend: {otherUserId} unfriended {myUserId}, removing from my list");

                                // Xóa họ khỏi danh sách của mình
                                var unfriendUrl = GetAuthenticatedUrl($"friends/{myUserId}/{otherUserId}.json");
                                var unfriendResponse = await _httpClient.DeleteAsync(unfriendUrl);

                                if (unfriendResponse.IsSuccessStatusCode)
                                {
                                    hasChanges = true;
                                    System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Successfully removed {otherUserId} from {myUserId}'s friends list");

                                    // Cleanup friendRequests liên quan
                                    await CleanupFriendRequestsAsync(myUserId, otherUserId);
                                    await CleanupFriendRequestsAsync(otherUserId, myUserId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Error checking {otherUserId}: {ex.Message}");
                        // Continue với user khác
                    }
                }

                return hasChanges;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncBidirectionalUnfriend] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem có request đang active (Pending/Accepted) giữa 2 users không
        /// </summary>
        private async Task<bool> HasActiveRequestBetweenUsersAsync(string senderId, string receiverId)
        {
            try
            {
                // Check senderId -> receiverId
                var hasRequest1 = await HasPendingRequestAsync(senderId, receiverId);

                // Check receiverId -> senderId  
                var hasRequest2 = await HasPendingRequestAsync(receiverId, senderId);

                // Check for accepted requests
                var hasAccepted1 = await HasAcceptedRequestAsync(senderId, receiverId);
                var hasAccepted2 = await HasAcceptedRequestAsync(receiverId, senderId);

                return hasRequest1 || hasRequest2 || hasAccepted1 || hasAccepted2;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HasActiveRequestBetweenUsers] Exception: {ex.Message}");
                return false; // Safe default
            }
        }

        /// <summary>
        /// Kiểm tra có request Accepted giữa 2 users không
        /// </summary>
        private async Task<bool> HasAcceptedRequestAsync(string senderId, string receiverId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}.json");
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var requests = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);

                        foreach (var kv in requests)
                        {
                            var request = kv.Value;
                            var requestSenderId = request.senderId?.ToString();
                            var status = request.status?.ToString();

                            if (requestSenderId == senderId && status == "Accepted")
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HasAcceptedRequest] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách giới hạn user IDs để scan (fallback khi GetAllUserIds bị block)
        /// </summary>
        private async Task<List<string>> GetLimitedUserIdsAsync(int limit = 20)
        {
            try
            {
                // Thử lấy từ GetAllUserIds trước
                var allIds = await GetAllUserIdsAsync();
                if (allIds.Count > 0)
                {
                    return allIds.Take(limit).ToList();
                }

                // Fallback Strategy 1: Direct scan friendRequests.json (thường ít restrictive hơn)
                System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] Primary GetAllUserIds failed, trying direct friendRequests scan...");
                var requestsUrl = GetAuthenticatedUrl("friendRequests.json");
                var requestsResponse = await _httpClient.GetAsync(requestsUrl);

                if (requestsResponse.IsSuccessStatusCode)
                {
                    var requestsContent = await requestsResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(requestsContent) && requestsContent != "null")
                    {
                        var allRequests = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(requestsContent);
                        var userIds = new HashSet<string>();

                        // Lấy receiver IDs
                        foreach (var receiverId in allRequests.Keys)
                        {
                            userIds.Add(receiverId);
                            _userIdCache.Add(receiverId); // Cache ngay
                        }

                        // Lấy sender IDs từ requests
                        foreach (var receiverData in allRequests.Values)
                        {
                            foreach (var request in receiverData.Values)
                            {
                                var senderId = request.senderId?.ToString();
                                if (!string.IsNullOrEmpty(senderId))
                                {
                                    userIds.Add(senderId);
                                    _userIdCache.Add(senderId); // Cache ngay
                                }
                            }
                        }

                        var extractedIds = userIds.Take(limit).ToList();
                        System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] SUCCESS: Extracted {extractedIds.Count} user IDs from friendRequests.json");
                        return extractedIds;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] friendRequests.json failed: {requestsResponse.StatusCode}");
                }

                // Fallback Strategy 2: Lấy từ publicUsers (có thể ít restrictive hơn)
                System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] Trying publicUsers fallback...");
                var publicUrl = GetAuthenticatedUrl("publicUsers.json");
                var publicResponse = await _httpClient.GetAsync(publicUrl);

                if (publicResponse.IsSuccessStatusCode)
                {
                    var publicContent = await publicResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(publicContent) && publicContent != "null")
                    {
                        var publicUsers = JsonConvert.DeserializeObject<Dictionary<string, object>>(publicContent);
                        var publicIds = publicUsers.Keys.Take(limit).ToList();

                        // Cache ngay
                        foreach (var id in publicIds)
                        {
                            _userIdCache.Add(id);
                        }

                        System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] Found {publicIds.Count} users from publicUsers");
                        return publicIds;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] publicUsers failed: {publicResponse.StatusCode}");
                }

                // Fallback Strategy 3: Sử dụng cached user IDs từ previous operations
                System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] Using cached user IDs fallback");
                if (_userIdCache.Count > 0)
                {
                    var cachedIds = _userIdCache.Take(limit).ToList();
                    System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] Found {cachedIds.Count} cached user IDs");
                    return cachedIds;
                }

                // Fallback Strategy 4: Return empty but log the issue
                System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] ⚠️ ALL FALLBACK STRATEGIES FAILED - No user IDs available for scanning");
                return new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetLimitedUserIds] Exception: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Thêm friendship bidirectional với proper error handling
        /// </summary>
        private async Task<bool> AddBidirectionalFriendshipAsync(string userId1, string userId2)
        {
            try
            {
                // Cache user IDs for future fallback
                _userIdCache.Add(userId1);
                _userIdCache.Add(userId2);

                var friendData = new Dictionary<string, object>
                {
                    ["status"] = "Friends",
                    ["since"] = DateTime.UtcNow.ToString("o"),
                    ["syncedAt"] = DateTime.UtcNow.ToString("o")
                };

                // Add to user1's friends
                var url1 = GetAuthenticatedUrl($"friends/{userId1}/{userId2}.json");
                var content1 = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                var res1 = await _httpClient.PutAsync(url1, content1);
                content1.Dispose();

                // Add to user2's friends (bidirectional)
                var url2 = GetAuthenticatedUrl($"friends/{userId2}/{userId1}.json");
                var content2 = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                var res2 = await _httpClient.PutAsync(url2, content2);
                content2.Dispose();

                var success = res1.IsSuccessStatusCode && res2.IsSuccessStatusCode;

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[AddBidirectionalFriendship] Success: {userId1} <-> {userId2}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AddBidirectionalFriendship] Failed: {userId1} <-> {userId2}, Status: {res1.StatusCode}, {res2.StatusCode}");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddBidirectionalFriendship] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Thêm friendship single-side (chỉ cho 1 user) với proper error handling
        /// </summary>
        private async Task<bool> AddSingleSideFriendshipAsync(string userId, string friendId)
        {
            try
            {
                // Cache user IDs for future fallback
                _userIdCache.Add(userId);
                _userIdCache.Add(friendId);

                var friendData = new Dictionary<string, object>
                {
                    ["status"] = "Friends",
                    ["since"] = DateTime.UtcNow.ToString("o"),
                    ["syncedAt"] = DateTime.UtcNow.ToString("o")
                };

                // Add ONLY to userId's friends (single-side)
                var url = GetAuthenticatedUrl($"friends/{userId}/{friendId}.json");
                var content = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, content);
                content.Dispose();

                var success = response.IsSuccessStatusCode;

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[AddSingleSideFriendship] Success: {userId} -> {friendId}");
                    if (NotificationVM != null)
                    {
                        NotificationVM.TriggerFriendsListReload();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AddSingleSideFriendship] Failed: {userId} -> {friendId}, Status: {response.StatusCode}");
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddSingleSideFriendship] Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Property để inject NotificationViewModel từ MainViewModel
        /// </summary>
        /// <summary>
        /// Debug method: Kiểm tra trạng thái friends list hiện tại
        /// </summary>
        public async Task<string> DebugFriendsListAsync(string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DebugFriendsList] Checking friends list for {userId}");

                var friends = await GetFriendsAsync(userId);
                var friendNames = new List<string>();

                foreach (var friend in friends)
                {
                    friendNames.Add($"{friend.Name} ({friend.Id})");
                }

                var result = $"User {userId} has {friends.Count} friends:\n" + string.Join("\n", friendNames);
                System.Diagnostics.Debug.WriteLine($"[DebugFriendsList] {result}");

                return result;
            }
            catch (Exception ex)
            {
                var error = $"Debug failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[DebugFriendsList] {error}");
                return error;
            }
        }

        /// <summary>
        /// Debug method: Kiểm tra trạng thái unfriend markers trong friendRequests
        /// </summary>
        public async Task<string> DebugUnfriendMarkersAsync(string userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DebugUnfriendMarkers] Checking unfriend markers for {userId}");

                var url = GetAuthenticatedUrl($"friendRequests/{userId}.json");
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return $"Failed to get friendRequests: {response.StatusCode}";
                }

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content) || content == "null")
                {
                    return $"No friendRequests found for {userId}";
                }

                var requests = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(content);
                var unfriendMarkers = new List<string>();

                foreach (var kv in requests)
                {
                    var requestId = kv.Key;
                    var request = kv.Value;
                    var status = request.status?.ToString();
                    var markerType = request.markerType?.ToString();
                    var senderId = request.senderId?.ToString();

                    if (status == "NotifyUnfriend" && markerType == "UnfriendNotification")
                    {
                        unfriendMarkers.Add($"Marker {requestId}: {senderId} removed {userId}");
                    }
                }

                var result = unfriendMarkers.Count > 0
                    ? $"Found {unfriendMarkers.Count} unfriend markers:\n" + string.Join("\n", unfriendMarkers)
                    : $"No unfriend markers found for {userId}";

                System.Diagnostics.Debug.WriteLine($"[DebugUnfriendMarkers] {result}");
                return result;
            }
            catch (Exception ex)
            {
                var error = $"Debug unfriend markers failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[DebugUnfriendMarkers] {error}");
                return error;
            }
        }

        public NotificationViewModel NotificationVM { get; set; }
    }
}

