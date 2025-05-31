using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using Learnify.Models;

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

        // Lưu thời gian học của người dùng
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
                    Debug.WriteLine($"No username found for user {userId}, setting username to UID");
                    await SaveUsernameAsync(userId, userId);
                }

                // Tạo timestamp cho phiên học mới
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var newSession = new
                {
                    duration = studyTime.TotalMinutes,
                    timestamp = DateTime.UtcNow.ToString("o")
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
                    lastUpdated = DateTime.UtcNow.ToString("o"),
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

        // Lấy bảng xếp hạng thời gian học
        public async Task<List<(string UserId, TimeSpan Time)>> GetStudyTimeRankingsAsync()
        {
            try
            {
                Debug.WriteLine("Getting study time rankings...");
                var url = GetAuthenticatedUrl("users.json");
                Debug.WriteLine($"Fetching data from: {url}");

                var response = await _httpClient.GetAsync(url);
                Debug.WriteLine($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Response content: {content}");

                    if (string.IsNullOrEmpty(content) || content == "null")
                    {
                        Debug.WriteLine("No data found in response");
                        return new List<(string UserId, TimeSpan Time)>();
                    }

                    var users = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(content);
                    if (users == null)
                    {
                        Debug.WriteLine("Failed to deserialize users data");
                        return new List<(string UserId, TimeSpan Time)>();
                    }

                    Debug.WriteLine($"Found {users.Count} users");
                    var rankings = new List<(string UserId, TimeSpan Time)>();

                    // Xác định quý hiện tại
                    var now = DateTime.UtcNow;
                    int currentQuarter = (now.Month - 1) / 3 + 1;
                    int quarterStartMonth = (currentQuarter - 1) * 3 + 1;
                    var quarterStart = new DateTime(now.Year, quarterStartMonth, 1);
                    var quarterEnd = quarterStart.AddMonths(3);

                    foreach (var user in users)
                    {
                        try
                        {
                            Debug.WriteLine($"Processing user {user.Key}...");
                            var studyTimeData = user.Value["studyTime"];
                            if (studyTimeData != null && studyTimeData.Type == JTokenType.Object)
                            {
                                // Tính tổng thời gian học trong quý hiện tại
                                double totalMinutes = 0;
                                var sessions = studyTimeData["sessions"] as JObject;
                                if (sessions != null)
                                {
                                    foreach (var session in sessions)
                                    {
                                        var timestampStr = session.Value["timestamp"]?.ToString();
                                        if (DateTime.TryParse(timestampStr, out var timestamp))
                                        {
                                            if (timestamp >= quarterStart && timestamp < quarterEnd)
                                            {
                                                totalMinutes += session.Value["duration"]?.Value<double>() ?? 0;
                                            }
                                        }
                                    }
                                }
                                Debug.WriteLine($"User {user.Key}: {totalMinutes} minutes in current quarter");
                                if (totalMinutes > 0)
                                {
                                    rankings.Add((user.Key, TimeSpan.FromMinutes(totalMinutes)));
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"No study time data for user {user.Key}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing user {user.Key}: {ex.Message}");
                        }
                    }

                    var sortedRankings = rankings.OrderByDescending(x => x.Time.TotalMinutes).ToList();
                    Debug.WriteLine($"Setting leaderboard with {sortedRankings.Count} entries");
                    return sortedRankings;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Error response: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting study time rankings: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return new List<(string UserId, TimeSpan Time)>();
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

        public async Task<List<StudyLog>> GetUserStudyLogsAsync(string userId)
        {
            var logs = new List<StudyLog>();
            var url = GetAuthenticatedUrl($"users/{userId}/studyTime.json");
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content) && content != "null")
                {
                    var data = JObject.Parse(content);
                    var sessions = data["sessions"] as JObject;
                    if (sessions != null)
                    {
                        foreach (var session in sessions)
                        {
                            var timestamp = DateTime.Parse(session.Value["timestamp"].ToString());
                            var duration = session.Value["duration"].Value<double>();
                            logs.Add(new StudyLog { Date = timestamp, Hours = duration / 60.0 });
                        }
                    }
                }
            }
            return logs;
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
                    foreach (var user in users)
                    {
                        var userId = user.Key;
                        var username = user.Value["username"]?.ToString();
                        if (string.IsNullOrEmpty(username) || username == "null")
                        {
                            Debug.WriteLine($"Fixing username for user {userId}");
                            await SaveUsernameAsync(userId, userId);
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
    }
} 