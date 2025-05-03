using Learnify.Models;
using System.Collections.ObjectModel;

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
            Leaderboard = new ObservableCollection<UserRanking>
            {
                new UserRanking { Rank = 1, Name = "Người bạn 1", Time = "13 giờ 29 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star1.svg" },
                new UserRanking { Rank = 2, Name = "Người bạn 2", Time = "8 giờ 40 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star2.svg" },
                new UserRanking { Rank = 3, Name = "Người bạn 3", Time = "6 giờ 25 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star3.svg" },
                new UserRanking { Rank = 4, Name = "Người bạn 4", Time = "5 giờ 10 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star4.svg" },
                new UserRanking { Rank = 5, Name = "Người bạn 5", Time = "2 giờ 20 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star5.svg" },
                new UserRanking { Rank = 6, Name = "Người bạn 6", Time = "13 giờ 29 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star6.svg" },
                new UserRanking { Rank = 7, Name = "Người bạn 5", Time = "2 giờ 20 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star5.svg" },
                new UserRanking { Rank = 8, Name = "Người bạn 5", Time = "2 giờ 20 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star5.svg" },
                new UserRanking { Rank = 9, Name = "Người bạn 5", Time = "2 giờ 20 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star5.svg" },
                new UserRanking { Rank = 10, Name = "Người bạn 5", Time = "2 giờ 20 phút", Avatar="/Images/avatar1.svg", StarIcon="/Images/star5.svg" },
            };
        }
    }
}
