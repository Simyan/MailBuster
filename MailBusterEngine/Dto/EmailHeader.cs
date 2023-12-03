using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailBusterEngine.Dto
{
    public class EmailHeader
    {
        public string returnEmail { get; set; }
        public string unsubscribeLink { get; set; }

        public string Id { get; set; }
        public string emailId { get; set; }
        public string sentOn { get; set; }
        public string sender { get; set; }
        public string subject { get; set; }


    }
}
