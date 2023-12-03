using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailBusterEngine.Dto
{
    public class ConnectionInfo
    {
        public string Host { get; set; }
        public int port { get; set; }
        public bool isSSL { get; set; }
        public string email { get; set; }
        public string password { get; set; }

    }
}
