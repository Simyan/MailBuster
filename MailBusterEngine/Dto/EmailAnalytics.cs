using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailBusterEngine.Dto
{
    public class EmailAnalytics
    {
        public string EmailId { get; set; }
        public double AverageGap { get; set; }
        public double StandardDeviation { get; set; }
        public int Count { get; set; }
        public double Score { get; set; }
        public double Score2 { get; set; }
        //string from;
    }
}
