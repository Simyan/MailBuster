using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailBusterEngine.Dto
{
    public class EmailDetails
    {
        public string from { get; set; }
        public string subject { get; set; }
        public DateTime sentOn { get; set; }
        public string unsubscribeLinkFromHeader { get; set; }
        public string unsubscribeLinkFromBody { get; set; }
    }
}
