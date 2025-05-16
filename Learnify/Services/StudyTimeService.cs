using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                _userTimes = loaded.ToDictionary(
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
    }
}
