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
using MailBusterEngine.Dto;
using MailBusterEngine.Helper;

namespace MailBuster{
    public class IMAPMailHelper 
    {
        private readonly IConfiguration _configuration;
        static readonly string basePath = "..\\..\\..\\Filestore\\";
        static readonly string trackerPath = $"{basePath}tracker.txt";
        private readonly ConnectionInfo gmail;
       

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

        public ImapClient Connect()
        {
            ImapClient client = new ImapClient();
            //Pop3Client client = new Pop3Client(); 
            client.Connect(gmail.Host, gmail.port, gmail.isSSL);
            client.Authenticate(gmail.email, gmail.password);
            return client;
        }


        #region mappers
        public void MapEmail(MimeMessage msg, List<Email> emails)
        {
            Email emailDto = new Email();
            emailDto.EmailHeaderDto = new EmailHeader();
            emailDto.textBody = msg.TextBody;
            emailDto.htmlBody = msg.HtmlBody;
            emailDto.from = msg.From.ToString();
            emailDto.sentOn = msg.Date.DateTime;
            emailDto.sentOn = msg.Date.DateTime;
            MapHeaders(msg.Headers, emailDto);
            emails.Add(emailDto);

        }
        public void MapHeaders(HeaderList headers, Email emailDto)
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
        #endregion



        #region start

        public async void Control()
        {
            var emailsArray = await KageBunshinNoJutsuAsync();
            var emails = emailsArray
                            .SelectMany(s => s)
                            .OrderBy(o => o.sentOn)
                            .ToList();
            //analyzeUnsubscribeLink(emails);
            var result = Analyze(emails).ToList();
            var fileHelper = new FileHelper(basePath);
            fileHelper.WriteToFile(result);

        }

        public async Task<List<Email>> GetEmailsTask(int segment, int start, int limit)
        {
            List<Email> emails = new List<Email>();
            var client = Connect();
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
                    MapEmail(msg, emails);
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

        public async Task<List<Email>[]> KageBunshinNoJutsuAsync()
        {
            int size = 4;
            List<Email>[] emailsArray = new List<Email>[size]; 
            Task<List<Email>>[] tasks = new Task<List<Email>>[size];
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

        public double CalculateStandardDeviation(List<double> numbers, double average)
        {
            double sumOfSquaresOfDifferences = numbers.Select(val => Math.Pow(val - average, 2)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / numbers.Count);
        }

        public IEnumerable<EmailAnalytics> Analyze(List<Email> emails)
        {

            Dictionary<string, DatesByEmail> ls = new Dictionary<string, DatesByEmail>();
           

            foreach (var email in emails)
            {
                var tmp = new DatesByEmail() { sentOnList = new List<DateTime> { email.sentOn } };
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
                var stddev = gaps.Count != 0 ? CalculateStandardDeviation(gaps, avg): -1;
                emailAnalyticsLs.Add(new EmailAnalytics() 
                                        { 
                                            EmailId = item.Key, 
                                            AverageGap = avg, 
                                            StandardDeviation = stddev,
                                            Count = dateList.Count(), 
                                            Score = dateList.Count() / avg
                                        });

            }

            var x = emailAnalyticsLs
                        .Where(w => w.Count > 5 || w.AverageGap > 1 || w.AverageGap == -1)
                        .OrderByDescending(o => o.Score);
            var y = emailAnalyticsLs.OrderByDescending(o => o.Count).ThenByDescending(o => o.Score);

            var z = emailAnalyticsLs
                        .GroupBy(g => g.Count)
                        .Select(s => new { Frequency = s.Key, Count = s.Count(), Total = s.Key * s.Count() })
                        .OrderBy(o => o.Frequency);

            var sum = z.Sum(s => s.Total);

            return y;
        }

        public void AnalyzeUnsubscribeLink(List<Email> emails)
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
       
    }
}