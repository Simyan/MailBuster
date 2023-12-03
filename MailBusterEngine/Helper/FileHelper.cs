using MailBusterEngine.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailBusterEngine.Helper
{
    public class FileHelper
    {
        private readonly string BasePath;

        public FileHelper(string basePath) => BasePath = basePath; 

        public void WriteToFile(Dictionary<string, int> content)
        {
            string jsonText = JsonConvert.SerializeObject(content);
            File.WriteAllText($"{BasePath}MailCountPerSender.txt", jsonText);
        }

        public void WriteToFile(List<KeyValuePair<string, int>> content)
        {

            foreach (var item in content)
            {
                File.AppendAllText($"{BasePath}SortedMailCountPerSender.txt", $"{item.Key}: {item.Value} \n");
            }
        }

        public void WriteToFile(List<EmailAnalytics> content)
        {
            string json = JsonConvert.SerializeObject(content);
            File.AppendAllText($"{BasePath}EmailAnalytics.json", json);
            foreach (var item in content)
            {
                //string json = JsonConvert.SerializeObject(item);
                //File.AppendAllText($"{basePath}EmailAnalytics.json", json + "\n");
                File.AppendAllText($"{BasePath}EmailAnalytics.txt", $"Email: {item.EmailId}, AvgGap: {item.AverageGap}, Count: {item.Count}, Score: {item.Score} \n");
            }
        }

        public bool IsDataAvailable()
        {
            FileInfo file = new FileInfo(@"C:\Workspace\DotNet\MailBuster\Filestore\SortedMailCountPerSender.txt");

            if (file.Exists && file.Length > 0)
            {
                return true;
            }

            return false;
        }
    }
}
