using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using Learnify.Models;
using Learnify.Services;
using System.Windows.Media;

namespace Learnify.Services
{
    public class FirebaseService
    {
        private static readonly HttpClient _httpClient;

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
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sharing campaign: {ex.Message}");
                return false;
            }
        }

        private const string FirebaseUrl = "https://learnify-b5cf3-default-rtdb.asia-southeast1.firebasedatabase.app/";

        public FirebaseService()
        {
            // Không cần thiết lập timeout ở đây nữa vì đã được thiết lập trong static constructor
        }

        private string GetAuthenticatedUrl(string path)
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
                Debug.WriteLine($"Error getting user data: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateUserDataAsync(string userId, Dictionary<string, object> data)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}.json");
                var content = new StringContent(JsonConvert.SerializeObject(data));
                var response = await _httpClient.PutAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating user data: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveStudyTimeAsync(string userId, double duration)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("o");
                var data = new Dictionary<string, object>
                {
                    ["timestamp"] = timestamp,
                    ["duration"] = duration
                };

                var url = GetAuthenticatedUrl($"studyTime/{userId}/sessions/{timestamp}.json");
                var content = new StringContent(JsonConvert.SerializeObject(data));
                var response = await _httpClient.PutAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving study time: {ex.Message}");
                return false;
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
                Debug.WriteLine($"Error getting study time data: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetUsernameAsync(string userId)
        {
            try
            {
                var url = GetAuthenticatedUrl($"users/{userId}/username.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var username = content.Trim('"');
                    if (string.IsNullOrEmpty(username) || username == "null")
                    {
                        // Nếu không có username, trả về UID
                        return userId;
                    }
                    return username;
                }
                // Nếu không lấy được username, trả về UID
                return userId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting username: {ex.Message}");
                // Nếu có lỗi, trả về UID
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
                Debug.WriteLine($"Error saving username: {ex.Message}");
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
                    Debug.WriteLine("Error: User ID is null or empty");
                    return false;
                }

                // Kiểm tra token
                var token = AuthService.GetToken();
                if (string.IsNullOrEmpty(token))
                {
                    Debug.WriteLine("Error: Authentication token is missing");
                    return false;
                }

                Debug.WriteLine($"Attempting to save study time for user {userId}");
                Debug.WriteLine($"Current study time: {studyTime.TotalMinutes} minutes");

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
                Debug.WriteLine($"Fetching current data from: {url}");

                var getResponse = await _httpClient.GetAsync(url);
                Debug.WriteLine($"GET Response Status: {getResponse.StatusCode}");

                var totalMinutes = studyTime.TotalMinutes;
                var sessions = new Dictionary<string, object>();

                if (getResponse.IsSuccessStatusCode)
                {
                    var responseContent = await getResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Current data: {responseContent}");

                    if (!string.IsNullOrEmpty(responseContent) && responseContent != "null")
                    {
                        try
                        {
                            var existingData = JObject.Parse(responseContent);
                            var existingTotalMinutes = existingData["totalMinutes"]?.Value<double>() ?? 0;
                            Debug.WriteLine($"Existing total minutes: {existingTotalMinutes}");
                            totalMinutes += existingTotalMinutes;
                            Debug.WriteLine($"New total minutes: {totalMinutes}");

                            var existingSessions = existingData["sessions"] as JObject;
                            if (existingSessions != null)
                            {
                                sessions = existingSessions.ToObject<Dictionary<string, object>>();
                                Debug.WriteLine($"Existing sessions count: {sessions.Count}");
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
            var url = GetAuthenticatedUrl($"users/{uid}.json");
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content) && content != "null")
                {
                    var data = JObject.Parse(content);
                    var username = data["username"]?.ToString() ?? uid;
                    // Nếu bạn có trường avatar, lấy thêm ở đây
                    return new Friend
                    {
                        Name = username,
                        Avatar = "/Images/avatar1.svg", // hoặc lấy từ data["avatar"] nếu có
                        IsOnline = false // hoặc lấy từ data nếu có
                    };
                }
            }
            return null;
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
                Debug.WriteLine("[FIREBASE] Getting weekly study time rankings...");
                // Tính toán tuần hiện tại (thứ 2 đến chủ nhật) theo giờ Việt Nam (UTC+7)
                var nowVN = DateTime.UtcNow.AddHours(7);
                var today = nowVN.Date;
                int dayOfWeek = (int)today.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Chủ nhật = 7 thay vì 0
                var startOfWeek = today.AddDays(-(dayOfWeek - 1)); // Lùi về thứ 2
                var endOfWeek = startOfWeek.AddDays(6); // Chủ nhật của tuần
                Debug.WriteLine($"[FIREBASE] Current week (VN): {startOfWeek:yyyy-MM-dd} to {endOfWeek:yyyy-MM-dd}");
                var url = GetAuthenticatedUrl("users.json");
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[FIREBASE] Response content length: {content?.Length ?? 0}");
                    if (!string.IsNullOrEmpty(content) && content != "null")
                    {
                        var users = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(content);
                        var rankings = new List<(string UserId, TimeSpan Time)>();
                        Debug.WriteLine($"[FIREBASE] Found {users.Count} users");
                        foreach (var user in users)
                        {
                            try
                            {
                                var userId = user.Key;
                                var userData = user.Value;
                                var studyTimeData = userData["studyTime"] as JObject;
                                if (studyTimeData != null)
                                {
                                    var sessions = studyTimeData["sessions"] as JObject;
                                    double weeklyMinutes = 0;
                                    if (sessions != null)
                                    {
                                        Debug.WriteLine($"[FIREBASE] User {userId}: Processing {sessions.Count} sessions");
                                        foreach (var session in sessions)
                                        {
                                            try
                                            {
                                                var sessionData = session.Value as JObject;
                                                var timestampStr = sessionData?["timestamp"]?.Value<string>();
                                                var duration = sessionData?["duration"]?.Value<double>() ?? 0;
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
                                                    // Try legacy format if ISO fails
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
                                                }
                                                if (parsed)
                                                {
                                                    var sessionDate = sessionTimeVN.Date;
                                                    Debug.WriteLine($"[FIREBASE][DEBUG] User {userId}: Session raw timestamp = '{timestampStr}', parsed = {sessionTimeVN:yyyy-MM-dd HH:mm:ss} (Kind={sessionTimeVN.Kind}), sessionDate = {sessionDate:yyyy-MM-dd}, week = {startOfWeek:yyyy-MM-dd} to {endOfWeek:yyyy-MM-dd}");
                                                    if (sessionDate >= startOfWeek && sessionDate <= endOfWeek)
                                                    {
                                                        weeklyMinutes += duration;
                                                        Debug.WriteLine($"[FIREBASE] User {userId}: Session {sessionDate:yyyy-MM-dd} = {duration} minutes (weekly total: {weeklyMinutes})");
                                                    }
                                                    else
                                                    {
                                                        Debug.WriteLine($"[FIREBASE][DEBUG] User {userId}: Session {sessionDate:yyyy-MM-dd} is OUTSIDE week");
                                                    }
                                                }
                                                else
                                                {
                                                    Debug.WriteLine($"[FIREBASE][DEBUG] User {userId}: Could not parse timestamp '{timestampStr}'");
                                                }
                                            }
                                            catch (Exception sessionEx)
                                            {
                                                Debug.WriteLine($"[FIREBASE] Error processing session for user {userId}: {sessionEx.Message}");
                                            }
                                        }
                                    }
                                    Debug.WriteLine($"[FIREBASE] User {userId}: Weekly total = {weeklyMinutes} minutes");
                                    if (weeklyMinutes > 0)
                                    {
                                        rankings.Add((userId, TimeSpan.FromMinutes(weeklyMinutes)));
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"[FIREBASE] User {userId}: No study time data");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[FIREBASE] Error processing user {user.Key}: {ex.Message}");
                            }
                        }
                        Debug.WriteLine($"[FIREBASE] Final weekly rankings count: {rankings.Count}");
                        return rankings.OrderByDescending(r => r.Time.TotalMinutes).ToList();
                    }
                    else
                    {
                        Debug.WriteLine("[FIREBASE] Empty or null response content");
                    }
                }
                else
                {
                    Debug.WriteLine($"[FIREBASE] HTTP error: {response.StatusCode}");
                }
                return new List<(string UserId, TimeSpan Time)>();
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
    }
}

