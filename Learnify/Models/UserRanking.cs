using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Models
{
    
        public class UserRanking
        {
            public int Rank { get; set; }
            public string Name { get; set; }
            public string Avatar { get; set; } // đường dẫn ảnh avatar
            public string Time { get; set; }   // ví dụ: "13 giờ 29 phút"
            public string StarIcon { get; set; } // đường dẫn ảnh ngôi sao
        }

    
}
