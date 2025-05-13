using Learnify.Models;
using Learnify.Services; // Nhớ thêm namespace chứa StudyTimeService
using System.Collections.ObjectModel;
using System.Linq;

namespace Learnify.ViewModels
{
    public class RankingViewModel : ViewModelBase
    {
        private ObservableCollection<UserRanking> _leaderboard;
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

        public RankingViewModel()
        {
            var rankingData = StudyTimeService.GetRankings();

            Leaderboard = new ObservableCollection<UserRanking>(
                rankingData.Select((entry, index) => new UserRanking
                {
                    Rank = index + 1,
                    Name = entry.UserId, // hoặc lấy tên từ hồ sơ người dùng
                    Time = $"{(int)entry.Time.TotalHours} giờ {entry.Time.Minutes} phút",
                    Avatar = "/Images/avatar1.svg",
                    StarIcon = $"/Images/star{(index + 1).ToString()}.svg"
                }));
        }
    }
}

