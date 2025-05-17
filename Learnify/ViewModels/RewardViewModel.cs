using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Learnify.Models; // Thêm dòng này vào đầu file RewardViewModel.cs


namespace Learnify.ViewModels
{
    public class RewardViewModel : ViewModelBase
    {
        public ObservableCollection<TaskItemViewModel> Tasks { get; set; }

        public RewardViewModel()
        {
            Tasks = new ObservableCollection<TaskItemViewModel>
            {
                new TaskItemViewModel(new TaskItem { Title = "Đăng nhập", Description = "Đăng nhập mỗi ngày", IsCompleted = true, IsClaimed = false }),
                new TaskItemViewModel(new TaskItem { Title = "Học 1 bài", Description = "Hoàn thành bài học bất kỳ", IsCompleted = true, IsClaimed = false }),
                new TaskItemViewModel(new TaskItem { Title = "Mời bạn", Description = "Mời 1 người bạn", IsCompleted = false, IsClaimed = false })
            };
        }
    }
}
