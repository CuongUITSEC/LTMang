using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public RankingViewModel RankingVm { get; set; }
        public RewardViewModel RewardVm { get; set; }
        public PieChartViewModel HomePieVm { get; set; }

        public HomeViewModel()
        {
            RankingVm = new RankingViewModel();
            RewardVm = new RewardViewModel();
            HomePieVm = new PieChartViewModel();
            // Gọi cập nhật dữ liệu biểu đồ hôm nay
            _ = HomePieVm.UpdateTodayPieAsync();
        }
    }
}
