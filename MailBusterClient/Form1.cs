using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MailBuster;

namespace MailBusterClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SetUp();
        }

        private void SetUp()
        {
            var senderList = GetSenderData();
            dataGridView1.DataSource = senderList;
            
            //var helper = new IMAPMailHelper();
            //helper.GetCountMailsPerSender();
        }

        List<TestData> MakeTestDataSet()
        {
            var senderList = new List<TestData>();

            var sender1 = new TestData { Sender = "deku@ua.com", Frequency = 38, isDelete = true };
            var sender2 = new TestData { Sender = "bakugo@ua.com", Frequency = 81, isDelete = false };
            var sender3 = new TestData { Sender = "eraserhead@ua.com", Frequency = 9, isDelete = true };
            var sender4 = new TestData { Sender = "ochaka@ua.com", Frequency = 34, isDelete = false };
            senderList.Add(sender1);
            senderList.Add(sender2);
            senderList.Add(sender3);
            senderList.Add(sender4);

            return senderList;

        }

        List<SenderInfo> GetSenderData()
        {
            IMAPMailHelper iMAPMailHelper = new IMAPMailHelper();
            List<SenderInfo> senderInfoList = new List<SenderInfo>();

            if (iMAPMailHelper.IsDataAvailable())
            {
                var linesList = File.ReadLines(@"C:\Workspace\DotNet\MailBuster\Filestore\SortedMailCountPerSender.txt").Take(30).ToList();
                
                foreach(var x in linesList)
                {
                    var splitLine = x.Split(":");
                    senderInfoList.Add(new SenderInfo { Sender = splitLine[0], Frequency = Int32.Parse(splitLine[1]), isDelete = false });
                }

            }

            return senderInfoList;

        }

    }

    class TestData
    {
        public string Sender { get; set; }
        public int Frequency { get; set; }
        public bool isDelete { get; set; }
    }
}
