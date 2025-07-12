using Learnify.Models;
using Learnify.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Learnify.ViewModels
{    public class RankingViewModel : ViewModelBase
    {
        private ObservableCollection<UserRanking> _leaderboard;
        private ObservableCollection<UserRanking> _top3Leaderboard;
        private readonly FirebaseService _firebaseService;
        private bool _isLoading;

        public ObservableCollection<UserRanking> Leaderboard
        {
            get => _leaderboard;
            set
            {
                if (_leaderboard != value)
                {
                    _leaderboard = value;
                    OnPropertyChanged(nameof(Leaderboard));
                    // Cập nhật Top3 khi Leaderboard thay đổi
                    UpdateTop3Leaderboard();
                }
            }
        }

        public ObservableCollection<UserRanking> Top3Leaderboard
        {
            get => _top3Leaderboard;
            set
            {
                if (_top3Leaderboard != value)
                {
                    _top3Leaderboard = value;
                    OnPropertyChanged(nameof(Top3Leaderboard));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }        public RankingViewModel()
        {
            _firebaseService = new FirebaseService();
            Leaderboard = new ObservableCollection<UserRanking>();
            Top3Leaderboard = new ObservableCollection<UserRanking>();
        }

        private void UpdateTop3Leaderboard()
        {
            if (Leaderboard != null)
            {
                var top3 = Leaderboard.Take(3).ToList();
                Top3Leaderboard = new ObservableCollection<UserRanking>(top3);
            }
        }public override void OnViewActivated()
        {
            LoadRankingsAsync();
        }private async void LoadRankingsAsync()
        {
            try
            {
                IsLoading = true;
                // Debug.WriteLine("[RANKING] Starting to load rankings...");

                // Debug.WriteLine("[RANKING] Getting rankings from Firebase...");
                var rankings = await _firebaseService.GetStudyTimeRankingsAsync();
                // Debug.WriteLine($"[RANKING] Received {rankings.Count} rankings");

                if (rankings.Count == 0)
                {
                    // Debug.WriteLine("[RANKING] No rankings found");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Leaderboard = new ObservableCollection<UserRanking>();
                    });
                    return;
                }

                // Chỉ lấy những người dùng có thời gian học > 0 và sắp xếp theo thời gian giảm dần
                var validRankings = rankings
                    .Where(r => r.Time.TotalMinutes > 0)
                    .OrderByDescending(r => r.Time.TotalMinutes)
                    .ThenBy(r => r.UserId) // Sắp xếp phụ theo UserId để đảm bảo tính nhất quán
                    .ToList();

                // Debug.WriteLine($"[RANKING] Found {validRankings.Count} users with study time > 0");

                var userRankings = new ObservableCollection<UserRanking>();
                int currentRank = 1;
                double? previousTime = null;
                int actualPosition = 1;

                foreach (var ranking in validRankings)
                {
                    try
                    {
                        // Debug.WriteLine($"[RANKING] Processing user {ranking.UserId}...");
                        
                        // Lấy tên người dùng từ Firebase
                        string username = await _firebaseService.GetUsernameAsync(ranking.UserId);
                        if (string.IsNullOrEmpty(username) || username == "null")
                        {
                            username = "Người dùng";
                        }

                        // Xử lý trường hợp có cùng thời gian học (tie ranking)
                        double currentTime = Math.Round(ranking.Time.TotalMinutes, 2);
                        if (previousTime.HasValue && Math.Abs(currentTime - previousTime.Value) > 0.01)
                        {
                            currentRank = actualPosition;
                        }

                        // Tính toán thời gian học chi tiết với độ chính xác cao hơn
                        var totalMinutes = ranking.Time.TotalMinutes;
                        int hours = (int)(totalMinutes / 60);
                        int minutes = (int)(totalMinutes % 60);
                        int seconds = (int)((totalMinutes % 1) * 60);

                        // Tạo chuỗi hiển thị thời gian ngắn gọn và rõ ràng
                        string timeDisplay;
                        if (hours > 0)
                        {
                            timeDisplay = $"{hours}h {minutes}m";
                        }
                        else if (minutes > 0)
                        {
                            timeDisplay = $"{minutes}m {seconds}s";
                        }
                        else
                        {
                            timeDisplay = $"{Math.Round(totalMinutes, 1)}m";
                        }

                        // Debug.WriteLine($"[RANKING] Username: {username}, Time: {timeDisplay}, Rank: {currentRank}");

                        // Xác định icon sao dựa trên thứ hạng
                        string starIcon;
                        switch (currentRank)
                        {
                            case 1:
                                starIcon = "/Images/star1.svg"; // Vàng
                                break;
                            case 2:
                                starIcon = "/Images/star2.svg"; // Bạc
                                break;
                            case 3:
                                starIcon = "/Images/star3.svg"; // Đồng
                                break;
                            default:
                                starIcon = "/Images/star4.svg"; // Thường
                                break;
                        }

                        // Tạo thông tin xếp hạng
                        var userRanking = new UserRanking
                        {
                            UserId = ranking.UserId,
                            Rank = currentRank,
                            Name = username,
                            Time = timeDisplay,
                            Avatar = "/Images/avatar1.svg",
                            StarIcon = starIcon
                        };

                        userRankings.Add(userRanking);
                        // Debug.WriteLine($"[RANKING] Added ranking for {username}: Rank {currentRank}, Time {timeDisplay}");
                        
                        previousTime = currentTime;
                        actualPosition++;
                    }
                    catch (Exception ex)
                    {
                        // Debug.WriteLine($"[RANKING] Error processing ranking for user {ranking.UserId}: {ex.Message}");
                    }
                }

                // Debug.WriteLine($"[RANKING] Setting leaderboard with {userRankings.Count} entries");
                
                // Cập nhật UI trên Main Thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Leaderboard = userRankings;
                });
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"Error loading rankings: {ex.Message}");
                // Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Không thể tải bảng xếp hạng: {ex.Message}", 
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

