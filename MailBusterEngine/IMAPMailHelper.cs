using System;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using MailKit.Net.Pop3;
using Microsoft.Extensions.Configuration;

namespace MailBuster{
    public class IMAPMailHelper 
    {
        private readonly IConfiguration _configuration;

        ImapClient client = new ImapClient(); 
        //Pop3Client client = new Pop3Client(); 
        IMailFolder inbox;

        static string basePath = "..\\..\\..\\Filestore\\";
        static string trackerPath = $"{basePath}tracker.txt";

        class ConnectionInfo
        {
            public string Host { get; set; }
            public int port { get; set; }
            public bool isSSL { get; set; }
            public string email { get; set; }
            public string password { get; set; }
        }

        public IMAPMailHelper()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<IMAPMailHelper>();

            _configuration = builder.Build();

            string email = _configuration["email"];
            string password = _configuration["password"];

            //ConnectionInfo gmail = new ConnectionInfo { email = "abc@gmail.com", Host = "imap.gmail.com", isSSL = true, password = "password", port = 993 };
            ConnectionInfo gmail = new ConnectionInfo { email = email, Host = "imap.gmail.com", isSSL = true, password = password, port = 993 };

            client.Connect (gmail.Host, gmail.port, gmail.isSSL);
            client.Authenticate (gmail.email, gmail.password);
			// The Inbox folder is always available on all IMAP servers...
			inbox = client.Inbox;
        }

        
        public void GetCount()
        {
            inbox.Open (FolderAccess.ReadOnly);
            var totalCount = inbox.Count;
			Console.WriteLine ("Total messages: {0}", totalCount);
            client.Dispose();
        }

        public class EmailHeaderDto
        {
            public string returnEmail { get; set; }
            public string unsubscribeLink { get; set; }

            public string Id { get; set; }
            public string emailId { get; set; }
            public string sentOn { get; set; }
            public string sender { get; set; }
            public string subject { get; set; }


        }

        public class EmailDto
        {
            public EmailHeaderDto EmailHeaderDto { get; set; }
            public string textBody { get; set; }
            public string htmlBody { get; set; }
            public string from { get; set; }
           
            public DateTime sentOn { get; set; }

        }

        
        public void parseHeaders(HeaderList headers, EmailDto emailDto)
        {
           
            foreach (Header header in headers)
            {
                switch (header.Field)
                {

                    case "Reply-To":
                        emailDto.EmailHeaderDto.returnEmail = header.Value;
                        break;
                    case "List-Unsubscribe":
                        emailDto.EmailHeaderDto.unsubscribeLink = header.Value;
                        break;
                    case "Message-ID":
                        emailDto.EmailHeaderDto.Id = header.Value;
                        break;
                    case "From":
                        emailDto.EmailHeaderDto.emailId = header.Value;
                        break;
                    case "Date":
                        emailDto.EmailHeaderDto.sentOn = header.Value;
                        break;
                    case "Subject":
                        emailDto.EmailHeaderDto.subject = header.Value;
                        break;
                    case "Sender":
                        emailDto.EmailHeaderDto.sender = header.Value;
                        break;
                    default:
                        break;
                }
            }
        }

      


        #region start
        public class EmailDetails
        {
            public string from { get; set; }
            public string subject { get; set; }
            public DateTime sentOn { get; set; }
            public string unsubscribeLinkFromHeader { get; set; }
            public string unsubscribeLinkFromBody { get; set; }
        }

        public class EmailStats
        {
            public string emailId {get; set;}
            public int count {get; set;}
            public double avgGap { get; set; }
            public int maxGap { get; set; }

            public int minGap { get; set; }

            public DateTime earliestSentOn { get; set; }
            public DateTime latestSentOn { get; set; }

        }
        public void control()
        {
            var emails = iterateInbox();
            analyzeUnsubscribeLink(emails);
        }


        public List<EmailDto> iterateInbox()
        {
            List<EmailDto> emails = new List<EmailDto>();
            try
            {
                inbox.Open(FolderAccess.ReadOnly);
                string tracker = File.Exists(trackerPath) ?  File.ReadAllText(trackerPath) : null;
                int totalCount = inbox.Count - 1;
                int i = tracker == null ? 0 : int.Parse(tracker);
                int limit = i + 500;
                for (; i < limit; i++)
                {
                    if (i % 100 == 0 || i == 0)
                    {
                        Console.WriteLine("Processed {0} mails at time {1}", i, DateTime.Now.TimeOfDay.ToString());
                    }
                    var msg = inbox.GetMessage(i);
                    mapEmail(msg, emails);
                    File.WriteAllText($"{basePath}tracker.txt", i.ToString());
                }

               

            }
            catch (Exception e)
            {
                Console.WriteLine("Error while collecting data from inbox: " + e.Message);
            }
            return emails;
        }

        public void mapEmail(MimeMessage msg, List<EmailDto> emails)
        {
            EmailDto emailDto = new EmailDto();
            emailDto.EmailHeaderDto = new EmailHeaderDto();
            emailDto.textBody = msg.TextBody;
            emailDto.htmlBody = msg.HtmlBody;
            emailDto.from = msg.From.ToString();
            emailDto.sentOn = msg.Date.DateTime;
            emailDto.sentOn = msg.Date.DateTime;
            parseHeaders(msg.Headers, emailDto);
            emails.Add(emailDto);

        }

        public void analyzeUnsubscribeLink(List<EmailDto> emails)
        {
            //find emails that do not have unsubscribe link in header
            var countMissingFromHeader = emails.Where(x => x.EmailHeaderDto.unsubscribeLink == null).ToList();
            //find emails that do have unsubscribe link in header
            var countInHeader = emails.Where(x => x.EmailHeaderDto.unsubscribeLink != null).ToList();
            //find emails that do have unsubscribe link in body 
            var countOutside = emails.Where(x => x.textBody != null && x.textBody.Contains("unsubscribe") ).ToList();
            //find emails that do not have unsubscribe link in body 
            var countMissingOutside = emails.Where(x => x.textBody != null && !x.textBody.Contains("unsubscribe")).ToList();
            //No body 
            var countMissingBody = emails.Where(x => x.textBody == null).ToList();
            
        }
        #endregion

        #region - old code
        public void GetStatistics(List<EmailDto> emailDtos)
        {
            var emailFrequency = emailDtos
                                    .GroupBy(g => g.EmailHeaderDto.emailId)
                                    .Select(x => new { email = x.Key, count = x.Count() })
                                    .OrderByDescending(o => o.count)
                                    .ThenBy(o => o.email);

            //find start & end date of email range
            var earliestEmail = emailDtos.OrderBy(o => o.EmailHeaderDto.sentOn).FirstOrDefault();
            var oldestEmail = emailDtos.OrderBy(o => o.EmailHeaderDto.sentOn).LastOrDefault();
            var earliestEmail2 = emailDtos.OrderBy(o => o.sentOn).FirstOrDefault();
            var oldestEmail2 = emailDtos.OrderBy(o => o.sentOn).LastOrDefault();

            //How many are missing "reply-to" header 
            var missingReplyTo = emailDtos.Where(w => w.EmailHeaderDto.returnEmail == "" || w.EmailHeaderDto.returnEmail == null);
            //How many are missing "List-unsub" header 
            var missingListUnsub = emailDtos.Where(w => w.EmailHeaderDto.unsubscribeLink == "" || w.EmailHeaderDto.unsubscribeLink == null);
            //How many are missing "Message-Id" header 
            var missingMsgId = emailDtos.Where(w => w.EmailHeaderDto.Id == "" || w.EmailHeaderDto.Id == null);
            //How many are missing "From" header 
            var missingFrom = emailDtos.Where(w => w.EmailHeaderDto.emailId == "" || w.EmailHeaderDto.emailId == null && (w.from != "" || w.from == null));
            //How many are missing "Date" header 
            var missingDate = emailDtos.Where(w => w.EmailHeaderDto.sentOn == "" || w.EmailHeaderDto.sentOn == null && (w.sentOn < new DateTime(1996, 1, 1)));
            //How many are missing "Subject" header 
            var missingSubject = emailDtos.Where(w => w.EmailHeaderDto.subject == "" || w.EmailHeaderDto.subject == null);
            //How many are missing "Sender" header 
            var missingSender = emailDtos.Where(w => w.EmailHeaderDto.sender == "" || w.EmailHeaderDto.sender == null);
        }
        public void GetEmails()
        {
            List<EmailDto> emails = new List<EmailDto>();
            inbox.Open(FolderAccess.ReadOnly);
            int totalCount = inbox.Count - 1;
            int i = 80000;
            int end = i + 400;




            try
            {
                for (; i < end; i++)
                {
                    if (i % 50 == 0 || i == 0)
                    {
                        Console.WriteLine("Processed {0} mails at time {1}", i, DateTime.Now.TimeOfDay.ToString());
                    }
                    EmailDto emailDto = new EmailDto();
                    emailDto.EmailHeaderDto = new EmailHeaderDto();
                    var msg = inbox.GetMessage(i);
                    emailDto.textBody = emailDto.textBody;
                    emailDto.htmlBody = msg.HtmlBody;
                    var attachments = msg.Attachments;

                    emailDto.sentOn = msg.Date.DateTime;
                    parseHeaders(msg.Headers, emailDto);
                    emails.Add(emailDto);
                }

                File.WriteAllText($"{basePath}tracker.txt", i.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while collecting data from inbox: " + e.Message);
            }



            GetStatistics(emails);
        }


        public void GetCountMailsPerSender()
        {

            List<string> senders = new List<string>();
            int min = 0;
            int max = 100; //Set to -1 if not bounded
            int multiple = 20; //Set this to 5000 or more when reading all mails
            Dictionary<string, int> mailCountPerSender = new Dictionary<string, int>();
            try
            {
                File.WriteAllText($"{basePath}hello.txt", "Hellooo!");

                inbox.Open(FolderAccess.ReadOnly);
                int totalCount = inbox.Count - 1;
                int i = 80000;

                foreach (var summary in inbox.Fetch(min, max, MessageSummaryItems.Envelope))
                {
                    var from = summary.Envelope.From.ToString();

                    if (i % multiple == 0 || i == 0)
                    {
                        Console.WriteLine("Processed {0} mails by {1}", i, DateTime.Now);
                        Console.WriteLine("Current Mail is from {0}", from);
                    }

                    senders.Add(from);
                    mailCountPerSender[from] = mailCountPerSender.TryGetValue(from, out int fromCount) ? ++fromCount : 1;
                    i++;
                }

                WriteToFile(mailCountPerSender);
                var countList = SortMailCountPerSender(mailCountPerSender);
                WriteToFile(countList);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: ERROR: {1}", DateTime.Now, ex.ToString());
                senders.ForEach(x => File.AppendAllText($"{basePath}Senders.txt", x + '\n'));
            }
            finally
            {
                client.Dispose();
            }

        }

        List<KeyValuePair<string, int>> SortMailCountPerSender(Dictionary<string, int> mailCountPerSender)
        {
            var countList = mailCountPerSender.ToList();
            countList.Sort((x, y) => y.Value.CompareTo(x.Value));
            return countList;
        }


        #endregion



        public void WriteToFile(Dictionary<string, int> content)
        {
            string jsonText = JsonConvert.SerializeObject(content);
            File.WriteAllText($"{basePath}MailCountPerSender.txt", jsonText);
        }

        public void WriteToFile(List<KeyValuePair<string, int>> content)
        {

            foreach (var item in content) 
            {
                File.AppendAllText($"{basePath}SortedMailCountPerSender.txt", $"{item.Key}: {item.Value} \n" );
            }
        }

        public bool IsDataAvailable()
        {
            FileInfo file = new FileInfo(@"C:\Workspace\DotNet\MailBuster\Filestore\SortedMailCountPerSender.txt");

            if(file.Exists && file.Length > 0)
            {
                return true;
            }

            return false;
        }

       
    }
}