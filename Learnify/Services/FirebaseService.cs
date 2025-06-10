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

namespace Learnify.Services
{
    public class FirebaseService
    {
        private static readonly HttpClient _httpClient;
        public static HttpClient SharedHttpClient => _httpClient;

        static FirebaseService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
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

        // Chấp nhận lời mời kết bạn (đồng bộ bạn bè 2 phía)
        public async Task<bool> AcceptFriendRequestAsync(string senderId, string receiverId, string requestId)
        {
            try
            {
                Debug.WriteLine($"[AcceptFriendRequest] Starting accept process: {senderId} -> {receiverId}, requestId: {requestId}");
                // 1. Cập nhật trạng thái FriendRequest thành Accepted
                var requestUpdate = new Dictionary<string, object> { ["status"] = "Accepted" };
                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}/{requestId}.json");
                var content = new StringContent(JsonConvert.SerializeObject(requestUpdate), Encoding.UTF8, "application/json");
                var res1 = await _httpClient.PatchAsync(url, content);
                content.Dispose();
                Debug.WriteLine($"[AcceptFriendRequest] Step 1 - Update status: {res1.StatusCode}");
                if (!res1.IsSuccessStatusCode)
                {
                    var error1 = await res1.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[AcceptFriendRequest] Step 1 failed: {error1}");
                }
                // 2. Thêm vào danh sách bạn bè của cả receiver và sender
                var friendData = new Dictionary<string, object> { ["status"] = "Friends", ["since"] = DateTime.UtcNow.ToString("o") };
                var urlFriendReceiver = GetAuthenticatedUrl($"friends/{receiverId}/{senderId}.json");
                var urlFriendSender = GetAuthenticatedUrl($"friends/{senderId}/{receiverId}.json");
                Debug.WriteLine($"[AcceptFriendRequest] Step 2 - Creating friend URLs: {urlFriendReceiver} | {urlFriendSender}");
                var contentReceiver = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                var contentSender = new StringContent(JsonConvert.SerializeObject(friendData), Encoding.UTF8, "application/json");
                var res2 = await _httpClient.PutAsync(urlFriendReceiver, contentReceiver);
                var res3 = await _httpClient.PutAsync(urlFriendSender, contentSender);
                contentReceiver.Dispose();
                contentSender.Dispose();
                Debug.WriteLine($"[AcceptFriendRequest] Step 2 - Add to friends: {res2.StatusCode}, {res3.StatusCode}");
                if (!res2.IsSuccessStatusCode)
                {
                    var error2 = await res2.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[AcceptFriendRequest] Step 2a failed: {error2}");
                }
                if (!res3.IsSuccessStatusCode)
                {
                    var error3 = await res3.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[AcceptFriendRequest] Step 2b failed: {error3}");
                }
                var success = res1.IsSuccessStatusCode && res2.IsSuccessStatusCode && res3.IsSuccessStatusCode;
                Debug.WriteLine($"[AcceptFriendRequest] Overall result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AcceptFriendRequest] Exception: {ex.Message}");
                Debug.WriteLine($"[AcceptFriendRequest] StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Từ chối lời mời kết bạn (cấu trúc mới)
        public async Task<bool> DeclineFriendRequestAsync(string receiverId, string requestId)
        {
            // Cập nhật trạng thái FriendRequest thành Declined
            var requestUpdate = new Dictionary<string, object> { ["status"] = "Declined" };
            var url = GetAuthenticatedUrl($"friendRequests/{receiverId}/{requestId}.json");
            var content = new StringContent(JsonConvert.SerializeObject(requestUpdate), Encoding.UTF8, "application/json");
            var res = await _httpClient.PatchAsync(url, content);
            content.Dispose();
            return res.IsSuccessStatusCode;
        }

        // Gửi lời mời kết bạn (theo cấu trúc mới)
        public async Task<SendFriendRequestResult> SendFriendRequestAsync(string senderId, string senderName, string receiverId, string receiverName)
        {
            try
            {
                // 1. Kiểm tra đã là bạn bè chưa
                if (await AreAlreadyFriendsAsync(senderId, receiverId))
                {
                    Debug.WriteLine($"[SendFriendRequest] Users {senderId} and {receiverId} are already friends");
                    return SendFriendRequestResult.AlreadyFriends;
                }

                // 2. Kiểm tra đã có lời mời Pending chưa
                if (await HasPendingRequestAsync(senderId, receiverId))
                {
                    Debug.WriteLine($"[SendFriendRequest] Already has pending request from {senderId} to {receiverId}");
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
                var request = new {
                    senderId = senderId,
                    senderName = senderName,
                    receiverId = receiverId,
                    receiverName = receiverName,
                    status = "Pending",
                    sentAt = DateTime.UtcNow.ToString("o")
                };
                var url = GetAuthenticatedUrl($"friendRequests/{receiverId}/{requestId}.json");
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync(url, content);
                content.Dispose();
                
                if (res.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[SendFriendRequest] Request sent successfully from {senderId} to {receiverId}");
                    return SendFriendRequestResult.Success;
                }
                else
                {
                    Debug.WriteLine($"[SendFriendRequest] Failed to send request: {res.StatusCode}");
                    return SendFriendRequestResult.Error;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendFriendRequest] Error: {ex.Message}");
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

        // Kiểm tra vượt quá giới hạn 5 lời mời/30 phút
        private async Task<bool> ExceedsRequestLimitAsync(string senderId, string receiverId)
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
                        var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
                        var recentRequestsCount = 0;

                        foreach (var kv in requests)
                        {
                            var request = kv.Value;
                            if (request.senderId?.ToString() == senderId)
                            {
                                var sentAtStr = request.sentAt?.ToString();
                                if (DateTime.TryParse(sentAtStr, out DateTime sentAt))
                                {
                                    if (sentAt >= thirtyMinutesAgo)
                                    {
                                        recentRequestsCount++;
                                    }
                                }
                            }
                        }

                        Debug.WriteLine($"[ExceedsRequestLimit] User {senderId} sent {recentRequestsCount} requests to {receiverId} in last 30 minutes");
                        return recentRequestsCount >= 5;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExceedsRequestLimit] Error: {ex.Message}");
                return false;
            }
        }

        // Kiểm tra đã có lời mời Pending chưa
        public async Task<bool> HasPendingRequestAsync(string senderId, string receiverId)
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
                            if (request.senderId?.ToString() == senderId && 
                                request.status?.ToString() == "Pending")
                            {
                                Debug.WriteLine($"[HasPendingRequest] Found pending request from {senderId} to {receiverId}");
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HasPendingRequest] Error: {ex.Message}");
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
                            var sentAt = request.sentAt?.ToString();
                            
                            Debug.WriteLine($"[FixMissingFriendsData] Found accepted request: {senderId} -> {receiverId}");
                            
                            // 3. Kiểm tra xem đã có trong friends chưa
                            if (!await AreAlreadyFriendsAsync(senderId, receiverId))
                            {
                                Debug.WriteLine($"[FixMissingFriendsData] Missing friends data, fixing...");
                                
                                // 4. Tạo dữ liệu bạn bè
                                var friendData = new Dictionary<string, object> 
                                { 
                                    ["status"] = "Friends", 
                                    ["since"] = sentAt ?? DateTime.UtcNow.ToString("o") 
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
    }
}

