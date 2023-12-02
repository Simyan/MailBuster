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
using System.Threading.Tasks;
using System.Threading;


namespace MailBuster{
    public class IMAPMailHelper 
    {
        private readonly IConfiguration _configuration;

        

        static string basePath = "..\\..\\..\\Filestore\\";
        static string trackerPath = $"{basePath}tracker.txt";
        ConnectionInfo gmail;

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
            gmail = new ConnectionInfo { email = email, Host = "imap.gmail.com", isSSL = true, password = password, port = 993 };

           
        }

        public ImapClient connect()
        {
            ImapClient client = new ImapClient();
            //Pop3Client client = new Pop3Client(); 
            client.Connect(gmail.Host, gmail.port, gmail.isSSL);
            client.Authenticate(gmail.email, gmail.password);
            return client;
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

        public async Task<List<EmailDto>> GetEmailsTask(int segment, int start, int limit)
        {
            List<EmailDto> emails = new List<EmailDto>();
            var client = connect();
            // The Inbox folder is always available on all IMAP servers...
            IMailFolder inbox = client.Inbox;
            try
            {

                inbox.Open(FolderAccess.ReadOnly);
                // string tracker = File.Exists(trackerPath) ? File.ReadAllText(trackerPath) : null;
                int totalCount = inbox.Count - 1;
                Console.WriteLine("thread #{0} total count is {1}", segment, totalCount);
                //if segment value is not based off zero index then it should be 1 less here 
                int i = start + (limit * segment);
                int end = i + limit;
                //int limit = i + 500;
                for (; i < end; i++)
                {
                    
                    if (i % 100 == 0 || i == 0)
                    {
                        Console.WriteLine("Thread #{0} processed: {1} mails at time {2} & the end is nigh {3}", segment, i, DateTime.Now.TimeOfDay.ToString(), end);
                    }
                    var msg = inbox.GetMessage(i);
                    mapEmail(msg, emails);
                    //File.WriteAllText($"{basePath}tracker.txt", i.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on thread #{0} while collecting data from inbox: {1}", segment, e.Message);
            }
            finally
            {
                client.Dispose();
            }
            return emails;
        }

        public async Task<List<EmailDto>[]> kageBunshinNoJutsuAsync()
        {
            int size = 4;
            List<EmailDto>[] emailsArray = new List<EmailDto>[size]; 
            Task<List<EmailDto>>[] tasks = new Task<List<EmailDto>>[size];
            for (int i = 0; i < size; i++)
            {
                int index = i;
                Console.WriteLine("Creating thread #{0}", index);
                tasks[i] = Task.Run(() => GetEmailsTask(index, 10000, 200));
            }
            Console.WriteLine("Start Threads & Wait");
            
            try
            {
                Task.WaitAll(tasks);
                Console.WriteLine("Done Waiting");

                
                for(int i = 0; i < size; i++)
                {
                    emailsArray[i] = await tasks[i];
                }

                return emailsArray;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        

        public async void control()
        {
            var emailsArray = await kageBunshinNoJutsuAsync();
            var emails = emailsArray
                            .SelectMany(s => s)
                            .OrderBy(o => o.sentOn)
                            .ToList();
            //analyzeUnsubscribeLink(emails);
            var result = analyze(emails).ToList();
            WriteToFile(result);

        }

        

        //public List<EmailDto> iterateInbox()
        //{
        //    List<EmailDto> emails = new List<EmailDto>();
        //    try
        //    {
        //        inbox.Open(FolderAccess.ReadOnly);
        //        string tracker = File.Exists(trackerPath) ?  File.ReadAllText(trackerPath) : null;
        //        int totalCount = inbox.Count - 1;
        //        int i = tracker == null ? 0 : int.Parse(tracker);
        //        int limit = i + 500;
        //        for (; i < limit; i++)
        //        {
        //            if (i % 100 == 0 || i == 0)
        //            {
        //                Console.WriteLine("Processed {0} mails at time {1}", i, DateTime.Now.TimeOfDay.ToString());
        //            }
        //            var msg = inbox.GetMessage(i);
        //            mapEmail(msg, emails);
        //            File.WriteAllText($"{basePath}tracker.txt", i.ToString());
        //        }

               

        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Error while collecting data from inbox: " + e.Message);
        //    }
        //    return emails;
        //}

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

        public class datesByEmail
        {
            public List<DateTime> sentOnList { get; set; }
            //string from;
        }


        public class EmailAnalytics
        {
            public string Email { get; set; }
            public double AverageGap { get; set; }
            public int Count { get; set; }
            public Double Score { get; set; }
            //string from;
        }



        public IEnumerable<EmailAnalytics> analyze(List<EmailDto> emails)
        {

            Dictionary<string, datesByEmail> ls = new Dictionary<string, datesByEmail>();
           

            foreach (var email in emails)
            {
                var tmp = new datesByEmail() { sentOnList = new List<DateTime> { email.sentOn } };
                if (!ls.ContainsKey(email.from))
                {
                    ls.Add(email.from, tmp);
                }
                else
                {
                    ls[email.from].sentOnList.Add(email.sentOn);
                }
            }


            Dictionary<string, Double> AvgList = new Dictionary<string, double>();
            List<EmailAnalytics> emailAnalyticsLs = new List<EmailAnalytics>();
            foreach (var item in ls)
            {
                var dateList = item.Value.sentOnList;
                var gaps = item.Value.sentOnList.Zip(dateList.Skip(1), (d1, d2) => (d2 - d1).TotalDays).ToList();
                var avg = gaps.Count != 0 ? gaps.Average() : -1;
                emailAnalyticsLs.Add(new EmailAnalytics() 
                                        { 
                                            Email = item.Key, 
                                            AverageGap = avg, 
                                            Count = dateList.Count(), 
                                            Score = dateList.Count() / avg
                                        });

            }

            var x = emailAnalyticsLs
                        .Where(w => w.Count > 5 || w.AverageGap > 1 || w.AverageGap == -1)
                        .OrderByDescending(o => o.Score);
            var y = emailAnalyticsLs.OrderByDescending(o => o.Count).ThenByDescending(o => o.Score);
            return y;
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

        public void WriteToFile(List<EmailAnalytics> content)
        {
            string json = JsonConvert.SerializeObject(content);
            File.AppendAllText($"{basePath}EmailAnalytics.json", json); 
            foreach (var item in content)
            {
                //string json = JsonConvert.SerializeObject(item);
                //File.AppendAllText($"{basePath}EmailAnalytics.json", json + "\n");
                File.AppendAllText($"{basePath}EmailAnalytics.txt", $"Email: {item.Email}, AvgGap: {item.AverageGap}, Count: {item.Count}, Score: {item.Score} \n");
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