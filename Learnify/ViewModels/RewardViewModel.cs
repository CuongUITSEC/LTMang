using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Learnify.Models;
using Learnify.Services;
using System.Diagnostics;

namespace Learnify.ViewModels
{
    public class RewardViewModel : ViewModelBase
    {
        public ObservableCollection<TaskItemViewModel> Tasks { get; set; }
        
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }
        
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public RewardViewModel()
        {
            Tasks = new ObservableCollection<TaskItemViewModel>();
            try
            {
                LoadRewardsAsync();
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Error initializing RewardViewModel: {ex}");
                StatusMessage = "Có lỗi xảy ra khi khởi tạo phần thưởng.";
                IsLoading = false;
            }
        }

        private async void LoadRewardsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Đang tải dữ liệu phần thưởng...";
                
                var userId = AuthService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    StatusMessage = "Vui lòng đăng nhập để xem phần thưởng.";
                    IsLoading = false;
                    return;
                }

                var today = DateTime.Now.Date;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                double todayMinutes = 0;
                double weekMinutes = 0;

                // Debug.WriteLine($"[REWARD] === Bắt đầu tính thời gian học ===");
                // Debug.WriteLine($"[REWARD] Ngày hiện tại: {today:yyyy-MM-dd}");
                // Debug.WriteLine($"[REWARD] Thứ 2 tuần này: {startOfWeek:yyyy-MM-dd}");

                var studyTimeData = await new FirebaseService().GetStudyTimeDataAsync(userId);
                if (studyTimeData != null && studyTimeData.Sessions != null)
                {
                    // Debug.WriteLine($"[REWARD] Tổng số phiên học: {studyTimeData.Sessions.Count}");
                    
                    foreach (var session in studyTimeData.Sessions)
                    {
                        if (DateTime.TryParse(session.Timestamp, out var sessionTime))
                        {
                            var localSessionTime = sessionTime.ToLocalTime();
                            var sessionDate = localSessionTime.Date;
                            
                            if (session.Duration > 0)
                            {
                                // Debug.WriteLine($"[REWARD] Phiên học: {localSessionTime:yyyy-MM-dd HH:mm:ss} | Thời lượng: {session.Duration} phút");
                                
                                if (sessionDate == today)
                                {
                                    todayMinutes += session.Duration;
                                    // Debug.WriteLine($"[REWARD] => Cộng vào thời gian học hôm nay: {session.Duration} phút (Tổng: {todayMinutes} phút)");
                                }
                                if (sessionDate >= startOfWeek && sessionDate <= today)
                                {
                                    weekMinutes += session.Duration;
                                    // Debug.WriteLine($"[REWARD] => Cộng vào thời gian học tuần này: {session.Duration} phút (Tổng: {weekMinutes} phút)");
                                }
                            }
                        }
                    }
                }

                // Debug.WriteLine($"[REWARD] === Kết quả ===");
                // Debug.WriteLine($"[REWARD] Tổng thời gian học hôm nay: {FormatTimeSpan(TimeSpan.FromMinutes(todayMinutes))}");
                // Debug.WriteLine($"[REWARD] Tổng thời gian học tuần này: {FormatTimeSpan(TimeSpan.FromMinutes(weekMinutes))}");
                // Debug.WriteLine($"[REWARD] === Kết thúc tính thời gian học ===");
                
                var statusBuilder = new StringBuilder();
                statusBuilder.AppendLine($"Thời gian học hôm nay: {FormatTimeSpan(TimeSpan.FromMinutes(todayMinutes))}");
                statusBuilder.AppendLine($"Thời gian học trong tuần: {FormatTimeSpan(TimeSpan.FromMinutes(weekMinutes))}");
                StatusMessage = statusBuilder.ToString();
                
                Tasks.Clear();
                var taskVMs = new[] {
                    new TaskItemViewModel(new TaskItem {
                        Title = "Học đủ 30 phút hôm nay",
                        Description = $"Hoàn thành 30 phút học trong ngày hiện tại. ({FormatTimeSpan(TimeSpan.FromMinutes(todayMinutes))} / 30 phút)",
                        IsCompleted = todayMinutes >= 30,
                        IsClaimed = false,
                        Reward = "Nhận 10 điểm"
                    }),
                    new TaskItemViewModel(new TaskItem {
                        Title = "Học đủ 1 tiếng hôm nay",
                        Description = $"Hoàn thành 1 tiếng học trong ngày hiện tại. ({FormatTimeSpan(TimeSpan.FromMinutes(todayMinutes))} / 60 phút)",
                        IsCompleted = todayMinutes >= 60,
                        IsClaimed = false,
                        Reward = "Nhận 20 điểm"
                    }),
                    new TaskItemViewModel(new TaskItem {
                        Title = "Học đủ 3 tiếng hôm nay",
                        Description = $"Hoàn thành 3 tiếng học trong ngày hiện tại. ({FormatTimeSpan(TimeSpan.FromMinutes(todayMinutes))} / 180 phút)",
                        IsCompleted = todayMinutes >= 180,
                        IsClaimed = false,
                        Reward = "Nhận 50 điểm"
                    }),
                    new TaskItemViewModel(new TaskItem {
                        Title = "Học đủ 20 tiếng trong tuần",
                        Description = $"Tích lũy đủ 20 tiếng học trong tuần này (thứ 2 - CN). ({FormatTimeSpan(TimeSpan.FromMinutes(weekMinutes))} / 1200 phút)",
                        IsCompleted = weekMinutes >= 1200,
                        IsClaimed = false,
                        Reward = "Nhận 100 điểm"
                    })
                };                foreach (var task in taskVMs)
                {
                    // Debug.WriteLine($"[REWARD] Nhiệm vụ: {task.Title}");
                    // Debug.WriteLine($"[REWARD] - Hoàn thành: {task.IsCompleted}");
                    // Debug.WriteLine($"[REWARD] - Đã nhận thưởng: {task.IsClaimed}");
                    // Debug.WriteLine($"[REWARD] - Phần thưởng: {task.Reward}");
                    Tasks.Add(task);
                    task.Refresh(); // Trigger property change notifications
                    // Debug.WriteLine($"[REWARD] Đã thêm nhiệm vụ '{task.Title}' vào danh sách Tasks. Tổng số nhiệm vụ hiện tại: {Tasks.Count}");
                }

                // Debug.WriteLine($"[REWARD] === Kiểm tra Tasks collection ===");
                // Debug.WriteLine($"[REWARD] Tasks.Count = {Tasks.Count}");
                // Debug.WriteLine($"[REWARD] IsLoading = {IsLoading}");
                
                // Force UI refresh
                OnPropertyChanged(nameof(Tasks));                // Thông báo nếu có nhiệm vụ hoàn thành
                var completedTasks = taskVMs.Where(t => t.IsCompleted && !t.IsClaimed).ToList();
                if (completedTasks.Any())
                {
                    var message = new StringBuilder();
                    message.AppendLine("Bạn có thể nhận thưởng cho các nhiệm vụ sau:");
                    foreach (var task in completedTasks)
                    {
                        message.AppendLine($"- {task.Title}: {task.Reward}");
                    }
                    StatusMessage = message.ToString();
                    OnPropertyChanged(nameof(StatusMessage));
                }
                else
                {
                    StatusMessage = "";
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Error loading rewards: {ex}");
                StatusMessage = "Không thể tải dữ liệu phần thưởng. Vui lòng thử lại sau.";
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours} giờ {timeSpan.Minutes} phút";
            }
            return $"{timeSpan.Minutes} phút";
        }
    }
}
