using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Models
{
    public class Friend
    {
        public string Name { get; set; }
        public string Avatar { get; set; } // Đường dẫn tương đối đến ảnh
        public bool IsOnline { get; set; }
    }
}