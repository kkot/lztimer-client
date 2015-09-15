using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            foreach (var activityPeriod in periodsFromDay)
            {
                var line = activityPeriod.Start + " " + activityPeriod.End + " " + activityPeriod.Length;
                richTextBox.AppendText(line + "\n");
            }
        }
    }
}
