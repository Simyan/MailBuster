using System;

namespace MailBuster
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var helper = new IMAPMailHelper();
            helper.Control();
            //helper.GetEmails();
            //helper.GetCountMailsPerSender();
        }
    }
}
