using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Learnify.Models;
using Learnify.Services;

namespace Learnify.Services
{
    public static class StudyTimeService
    {
        private static readonly string SavePath = "study_data.json";
        private static Dictionary<string, TimeSpan> _userTimes = new Dictionary<string, TimeSpan>();

        // Thêm thời gian học cho người dùng
        public static void AddStudyTime(string userId, TimeSpan sessionTime)
        {
            if (_userTimes.ContainsKey(userId))
                _userTimes[userId] += sessionTime;
            else
                _userTimes[userId] = sessionTime;

            SaveToFile();
        }

        // Lấy thời gian học của người dùng
        public static TimeSpan GetStudyTime(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return TimeSpan.Zero;
            return _userTimes.TryGetValue(userId, out var time) ? time : TimeSpan.Zero;
        }

        // Lấy bảng xếp hạng
        public static List<(string UserId, TimeSpan Time)> GetRankings()
        {
            return _userTimes
                .OrderByDescending(x => x.Value)
                .Select(x => (x.Key, x.Value))
                .ToList();
        }

        // Tải dữ liệu từ tệp JSON
        public static void LoadFromFile()
        {
            if (!File.Exists(SavePath)) return;

            var json = File.ReadAllText(SavePath);
            var loaded = JsonConvert.DeserializeObject<Dictionary<string, double>>(json);
            if (loaded != null)
            {
                _userTimes = loaded
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Key))
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => TimeSpan.FromMinutes(kvp.Value));
            }
        }

        // Lưu dữ liệu vào tệp JSON
        private static void SaveToFile()
        {
            var toSave = _userTimes.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.TotalMinutes);

            var json = JsonConvert.SerializeObject(toSave, Formatting.Indented);
            File.WriteAllText(SavePath, json);
        }

        // Lấy tên người dùng từ tệp JSON
        public static string GetCurrentUser()
        {
            if (!File.Exists(SavePath)) return string.Empty;

            var json = File.ReadAllText(SavePath);
            var loaded = JsonConvert.DeserializeObject<Dictionary<string, double>>(json);
            if (loaded != null && loaded.Count > 0)
            {
                // Trả về tên người dùng đầu tiên trong tệp JSON
                return loaded.Keys.First();
            }

            return string.Empty;
        }

        // === THỐNG NHẤT LOGIC TÍNH TOÁN ===
        
        /// <summary>
        /// Lấy ngày hiện tại theo giờ Việt Nam (UTC+7)
        /// </summary>
        public static DateTime GetCurrentDateVN()
        {
            return DateTime.UtcNow.AddHours(7).Date;
        }

        /// <summary>
        /// Lấy đầu tuần (Thứ 2) theo giờ Việt Nam
        /// </summary>
        public static DateTime GetStartOfWeekVN()
        {
            var today = GetCurrentDateVN();
            int dayOfWeek = (int)today.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Chủ nhật = 7 thay vì 0
            return today.AddDays(-(dayOfWeek - 1)); // Lùi về thứ 2
        }

        /// <summary>
        /// Lấy cuối tuần (Chủ nhật) theo giờ Việt Nam
        /// </summary>
        public static DateTime GetEndOfWeekVN()
        {
            return GetStartOfWeekVN().AddDays(6);
        }

        /// <summary>
        /// Parse timestamp theo tất cả format có thể
        /// </summary>
        public static DateTime? ParseSessionTimestamp(string timestampStr)
        {
            if (string.IsNullOrEmpty(timestampStr))
                return null;

            DateTime sessionTime;
            
            // Try ISO format first
            if (DateTime.TryParseExact(timestampStr, "yyyy-MM-ddTHH:mm:ss.fffffff", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out sessionTime))
                return sessionTime;

            // Try AM/PM format
            if (DateTime.TryParseExact(timestampStr, "MM/dd/yyyy hh:mm:ss tt", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out sessionTime))
                return sessionTime;

            // Try 24-hour format
            if (DateTime.TryParseExact(timestampStr, "MM/dd/yyyy HH:mm:ss", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out sessionTime))
                return sessionTime;

            // Try legacy format
            if (DateTime.TryParseExact(timestampStr, "MM/dd/yyyy HH:mm:ss", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out sessionTime))
                return sessionTime;

            // Final fallback
            if (DateTime.TryParse(timestampStr, out sessionTime))
                return sessionTime;

            return null;
        }

        /// <summary>
        /// Tính thời gian học hôm nay (theo giờ VN)
        /// </summary>
        public static double CalculateTodayStudyTime(List<FirebaseService.StudySession> sessions)
        {
            var today = GetCurrentDateVN();
            double totalMinutes = 0;

            if (sessions != null)
            {
                foreach (var session in sessions)
                {
                    var sessionTime = ParseSessionTimestamp(session.Timestamp);
                    if (sessionTime.HasValue)
                    {
                        var sessionDate = sessionTime.Value.Date;
                        if (sessionDate == today && session.Duration > 0)
                        {
                            totalMinutes += session.Duration;
                        }
                    }
                }
            }

            return totalMinutes;
        }

        /// <summary>
        /// Tính thời gian học tuần này (theo giờ VN)
        /// </summary>
        public static double CalculateWeekStudyTime(List<FirebaseService.StudySession> sessions)
        {
            var startOfWeek = GetStartOfWeekVN();
            var endOfWeek = GetEndOfWeekVN();
            double totalMinutes = 0;

            if (sessions != null)
            {
                foreach (var session in sessions)
                {
                    var sessionTime = ParseSessionTimestamp(session.Timestamp);
                    if (sessionTime.HasValue)
                    {
                        var sessionDate = sessionTime.Value.Date;
                        if (sessionDate >= startOfWeek && sessionDate <= endOfWeek && session.Duration > 0)
                        {
                            totalMinutes += session.Duration;
                        }
                    }
                }
            }

            return totalMinutes;
        }

        /// <summary>
        /// Tính thời gian học theo ngày cụ thể
        /// </summary>
        public static double CalculateStudyTimeForDate(List<FirebaseService.StudySession> sessions, DateTime targetDate)
        {
            double totalMinutes = 0;

            if (sessions != null)
            {
                foreach (var session in sessions)
                {
                    var sessionTime = ParseSessionTimestamp(session.Timestamp);
                    if (sessionTime.HasValue)
                    {
                        var sessionDate = sessionTime.Value.Date;
                        if (sessionDate == targetDate.Date && session.Duration > 0)
                        {
                            totalMinutes += session.Duration;
                        }
                    }
                }
            }

            return totalMinutes;
        }
    }
}
