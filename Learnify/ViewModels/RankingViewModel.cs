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
{
    public class RankingViewModel : ViewModelBase
    {
        private ObservableCollection<UserRanking> _leaderboard;
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
        }

        public RankingViewModel()
        {
            _firebaseService = new FirebaseService();
            Leaderboard = new ObservableCollection<UserRanking>();
            LoadRankingsAsync();
        }

        private async void LoadRankingsAsync()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine("Starting to load rankings...");

                if (!AuthService.IsAuthenticated())
                {
                    Debug.WriteLine("User is not authenticated");
                    MessageBox.Show("Vui lòng đăng nhập để xem bảng xếp hạng.", 
                        "Chưa đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug.WriteLine("Getting rankings from Firebase...");
                var rankings = await _firebaseService.GetStudyTimeRankingsAsync();
                Debug.WriteLine($"Received {rankings.Count} rankings");

                if (rankings.Count == 0)
                {
                    Debug.WriteLine("No rankings found");
                    MessageBox.Show("Chưa có dữ liệu xếp hạng.", 
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var userRankings = new ObservableCollection<UserRanking>();
                int rank = 1;

                // Sắp xếp rankings theo thời gian học giảm dần
                var sortedRankings = rankings.OrderByDescending(r => r.Time.TotalMinutes).ToList();

                foreach (var ranking in sortedRankings)
                {
                    try
                    {
                        Debug.WriteLine($"Processing user {ranking.UserId}...");
                        // Lấy tên người dùng từ Firebase
                        string username = await _firebaseService.GetUsernameAsync(ranking.UserId);
                        if (string.IsNullOrEmpty(username))
                        {
                            username = "Người dùng";
                        }

                        // Tính toán thời gian học
                        var totalHours = (int)ranking.Time.TotalHours;
                        var totalMinutes = ranking.Time.Minutes;

                        // Tạo chuỗi hiển thị thời gian
                        string timeDisplay;
                        if (totalHours > 0)
                        {
                            timeDisplay = $"{totalHours} giờ {totalMinutes} phút";
                        }
                        else
                        {
                            timeDisplay = $"{totalMinutes} phút";
                        }

                        Debug.WriteLine($"Username: {username}, Time: {timeDisplay}");

                        // Xác định icon sao dựa trên thứ hạng
                        string starIcon;
                        if (rank <= 3)
                        {
                            starIcon = $"/Images/star{rank}.svg";
                        }
                        else
                        {
                            starIcon = "/Images/star4.svg";
                        }

                        // Tạo thông tin xếp hạng
                        var userRanking = new UserRanking
                        {
                            Rank = rank,
                            Name = username,
                            Time = timeDisplay,
                            Avatar = "/Images/avatar1.svg",
                            StarIcon = starIcon
                        };

                        userRankings.Add(userRanking);
                        Debug.WriteLine($"Added ranking for {username}: Rank {rank}, Time {timeDisplay}");
                        rank++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing ranking for user {ranking.UserId}: {ex.Message}");
                    }
                }

                Debug.WriteLine($"Setting leaderboard with {userRankings.Count} entries");
                Leaderboard = userRankings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading rankings: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

