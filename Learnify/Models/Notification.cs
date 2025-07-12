using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Models
{
    public class Notification
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }
}
