using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Learnify.Models
{
    public class StudyLog
    {
        public DateTime Date { get; set; }
        public double Hours { get; set; }
        public double Duration { get; set; } // Số phút học, map trực tiếp từ Firebase nếu có
        public double Minutes => Hours * 60;
    }

}
