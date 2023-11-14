using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailBusterClient
{
    public class SenderInfo
    {
        public string Sender { get; set; }
        public int Frequency { get; set; }
        public bool isDelete { get; set; }
    }
}
