using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace kkot.LzTimer
{
    public partial class HistoryForm : Form
    {
        public HistoryForm()
        {
            InitializeComponent();
        }

        private void HistoryForm_Load(object sender, EventArgs e)
        {
            DirectoryInfo di = new DirectoryInfo(".");
            FileInfo[] rgFiles = di.GetFiles("*.txt");
            foreach (FileInfo fi in rgFiles)
            {
                int workSecs, funSecs;
                readFromFile(fi.Name, out workSecs, out funSecs);
                richTextBox1.AppendText(fi.Name
                    + "\t work " + Helpers.SecondsToHMS(workSecs)
                    + "  \t fun " + Helpers.SecondsToHMS(funSecs)
                    + "\n");
            }
        }

        private void readFromFile(string filename, out int numOfWorkSeconds, out int numOfFunSeconds)
        {
            try
            {
                FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);
                String readContent = sr.ReadToEnd();
                sr.Close();

                String[] splitedContent = readContent.Split("\n".ToCharArray());
                numOfWorkSeconds = int.Parse(splitedContent[0]);
                numOfFunSeconds = int.Parse(splitedContent[1]);
            }
            catch (Exception e)
            {
                numOfWorkSeconds = 0;
                numOfFunSeconds = 0;
            }
        }
    }
}
