using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailBusterEngine.Dto
{
    public class Email
    {
        public EmailHeader EmailHeaderDto { get; set; }
        public string textBody { get; set; }
        public string htmlBody { get; set; }
        public string from { get; set; }

        public DateTime sentOn { get; set; }

    }
}
