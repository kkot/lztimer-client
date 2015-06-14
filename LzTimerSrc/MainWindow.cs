using System;
using System.Windows.Forms;
using System.Drawing;
using System.Media;

namespace kkot.LzTimer
{
    public partial class MainWindow : Form, UserActivityListner
    {
        private readonly Font font = new Font(FontFamily.GenericMonospace, 11, GraphicsUnit.Pixel);
        private readonly Font fontSmall = new Font(FontFamily.GenericMonospace, 9, GraphicsUnit.Pixel);

        private const int TIME_LIMIT_NORMAL = 50;
        private const int TIME_LIMIT_WARNING = 60;

        private Icon idleIcon;
        private Icon currentTimeIcon;
        private Icon alldayTimeIcon;

        private SoundPlayer soundPlayer;
        private StatsReporter statsReporter;
        private ActivityChecker activityChecker;
        private TimeTablePolicies policies;
        private PeriodStorage periodStorage;
        private ShortcutsManager shortcutsManager;

        public MainWindow()
        {
            shortcutsManager = new ShortcutsManager(this);
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RecreateCurrentPeriodIcon(0);
            UpdateNotifyIcon(false, 0);

            // loading configuration
            int maxIdleMinutes = int.Parse(Properties.Settings.Default.MaxIdleMinutes.ToString());
            intervalTextBox.Text = maxIdleMinutes.ToString();
            timer1.Enabled = true;

            soundPlayer = new SoundPlayer();

            // register shotcuts keys
            shortcutsManager.Register();

            MoveToPosition();

            this.activityChecker = new ActivityChecker(new Win32LastActivityProbe(), new SystemClock());
            this.policies = new TimeTablePolicies() {IdleTimeout = maxIdleMinutes.min()};
            periodStorage = new SqlitePeriodStorage("periods.db");            
            TimeTable timeTable = new TimeTable(policies, periodStorage);
            this.activityChecker.SetActivityListner(timeTable);
            this.statsReporter = new StatsReporterImpl(timeTable, policies, new SystemClock());
        }

        //##################################################################

        private void timer1_Tick(object sender, EventArgs e)
        {
            activityChecker.Check();
            UpdateStats(statsReporter);
        }

        private void UpdateStats(StatsReporter reporter)
        {
            UpdateLabels(
                (int) reporter.GetTotalActiveToday(DateTime.Now.Date).Round(100.ms()).TotalSeconds, 
                (int) reporter.GetLastInactiveTimespan().Round(100.ms()).TotalSeconds
                );

            Period currentPeriod = reporter.GetCurrentLogicalPeriod();
            UpdateNotifyIcon(currentPeriod is ActivePeriod, (int)currentPeriod.Length.TotalMinutes);
            UpdateAlldayIcon(reporter.GetTotalActiveToday(DateTime.Now.Date));
        }

        private static Brush getCurrentTimeIconColor(int minutes)
        {
            if (minutes < TIME_LIMIT_NORMAL)
            {
                return Brushes.Green;
            }
            else if (minutes < TIME_LIMIT_WARNING)
            {
                return Brushes.Orange;
            }
            return Brushes.Red;
        }

        private void RecreateCurrentPeriodIcon(int minutes)
        {
            if (minutes > 99)
                minutes = 99;

            if (idleIcon == null) // idleIcon doesn't change
            {
                Bitmap idleBmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(idleBmp))
                {
                    g.FillEllipse(Brushes.Gray, 0, 0, 16, 16);
                }
                idleIcon = Icon.FromHandle(idleBmp.GetHicon());
            }

            Bitmap funBmp = new Bitmap(16, 16);
            if (currentTimeIcon != null)
                PInvoke.DestroyIcon(currentTimeIcon.Handle);

            using (Graphics g = Graphics.FromImage(funBmp))
            {
                g.FillEllipse(getCurrentTimeIconColor(minutes), 0, 0, 16, 16);
                g.DrawString(minutes.ToString(), font, Brushes.White, 0, 1);
            }
            currentTimeIcon = Icon.FromHandle(funBmp.GetHicon());
        }

        private void UpdateAlldayIcon(TimeSpan totalToday)
        {
            int hours = totalToday.Hours;
            int minutes = totalToday.Minutes;

            Bitmap alldayBmp = new Bitmap(16, 16);
            if (alldayTimeIcon != null)
                PInvoke.DestroyIcon(alldayTimeIcon.Handle);

            using (Graphics g = Graphics.FromImage(alldayBmp))
            {
                g.FillRectangle(Brushes.Wheat, 0, 0, 16, 16);
                string hoursStr = (hours > 9) ? "X" : "" + hours;
                g.DrawString(hoursStr, font, Brushes.Black, -1, -3);
                g.DrawString(""+minutes, fontSmall, Brushes.Red, 3, 6);
            }
            alldayTimeIcon = Icon.FromHandle(alldayBmp.GetHicon());
            notifyIconAllday.Icon = alldayTimeIcon;
        }

        private void UpdateNotifyIcon(bool active, int totalMinutes)
        {
            RecreateCurrentPeriodIcon(totalMinutes);
            notifyIcon1.Icon = !active ? idleIcon : currentTimeIcon;
        }

        private void UpdateLabels(int secondsToday, int secondsAfterLastBreak)
        {
            todayTimeLabel.Text  = Utilities.SecondsToHMS(secondsToday);
            lastBreakLabel.Text = Utilities.SecondsToHMS(secondsAfterLastBreak);
            
            string notifyText = "today " + todayTimeLabel.Text
                + "\n"
                + "\nb " + lastBreakLabel.Text;

            notifyIcon1.Text = notifyText;
            notifyIconAllday.Text = notifyText;
        }

        private void TestFormStatic_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                Hide();
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        internal void toggleVisible()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
                Show();
                MoveToPosition();
            }
            else
            {
                Hide();
                WindowState = FormWindowState.Minimized;
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                toggleVisible();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            shortcutsManager.ProcessMessage(ref m);
        }

        private void MoveToPosition()
        {
            if (Screen.PrimaryScreen.Bounds.Height > Screen.PrimaryScreen.WorkingArea.Height) // taskbar on bottom
            {
                Left = Screen.PrimaryScreen.WorkingArea.Width - Width - 10;
                Top = Screen.PrimaryScreen.WorkingArea.Height - Height - 10;
            }
            else // taskbar on left
            {
                Left = Screen.PrimaryScreen.WorkingArea.Left + 10;
                Top = Screen.PrimaryScreen.WorkingArea.Height - Height - 10;
            }
        }

        private void intervalTextBox_TextChanged(object sender, EventArgs e)
        {
            String text = intervalTextBox.Text;
            try {
                Properties.Settings.Default.MaxIdleMinutes = int.Parse(text);
                Properties.Settings.Default.Save();
            }
            catch(FormatException exception) {
                // ignore
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            shortcutsManager.UnRegister();
            Properties.Settings.Default.Save();
            periodStorage.Dispose();
        }

        private void reset_Click(object sender, EventArgs e)
        {
            periodStorage.Reset();
        }

        public void notifyActiveAfterBreak(TimeSpan leaveTime)
        {
            const int balloonTimeoutMs = 10000;
            notifyIcon1.ShowBalloonTip(balloonTimeoutMs, "leave", "time " + (int) leaveTime.TotalSeconds, ToolTipIcon.Info);
        }
    }
}
