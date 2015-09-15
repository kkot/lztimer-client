using System;
using System.Drawing;
using System.Windows.Forms;

namespace kkot.LzTimer
{
    public partial class HistoryWindow : Form
    {
        private readonly StatsReporter statsReporter;

        public HistoryWindow(StatsReporter statsReporter)
        {
            this.statsReporter = statsReporter;
            InitializeComponent();
        }

        private void HistoryWindow_Load(object sender, EventArgs e)
        {
            var periodsFromDay = statsReporter.PeriodsFromDay(DateTime.Now.Date);
            foreach (var activityPeriod in periodsFromDay.Reverse())
            {
                var line = activityPeriod.Start.ToString("t") 
                    + " - " + activityPeriod.End.ToString("t") 
                    + " length " + activityPeriod.Length.ToString(@"hh\:mm\:ss");

                var color = Color.Green;
                if (activityPeriod is ActivePeriod)
                {
                    color = Color.Red;
                }

                AppendText(richTextBox, line, color, true);
            }
        }

        private static void AppendText(RichTextBox box, string text, Color color, bool AddNewLine = false)
        {
            if (AddNewLine)
            {
                text += Environment.NewLine;
            }

            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}
