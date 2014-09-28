using System;
using System.Windows.Forms;
using System.Drawing;
using System.Media;

namespace kkot.LzTimer
{
    public partial class MainWindow : Form
    {
        private int initialWidth;
        private int initialHeight;

        //private Font font = new Font(FontFamily.GenericMonospace, 8.0f);
        private readonly Font font = new Font(FontFamily.GenericMonospace, 11, GraphicsUnit.Pixel);
        private readonly Font fontSmall = new Font(FontFamily.GenericMonospace, 9, GraphicsUnit.Pixel);

        private Icon idleIcon;
        private Icon currentPeriodIcon;
        private Icon alldayIcon;

        private SoundPlayer soundPlayer;
        private StatsReporter statsReporter;
        private ActivityChecker activityChecker;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RecreateCurrentPeriodIcon(0);
            UpdateNotifyIcon(false, 0);

            initialHeight = Height;
            initialWidth = Width;

            // loading configuration
            intervalTextBox.Text = Properties.Settings.Default.MaxIdleMinutes.ToString();
            timer1.Interval = 1000;
            timer1.Enabled = true;

            soundPlayer = new SoundPlayer();

            // rejestruje klawisze
            ShortcutsManager.RegisterHotKey(this, Keys.Z, ShortcutsManager.MOD_WIN);
            ShortcutsManager.RegisterHotKey(this, Keys.A, ShortcutsManager.MOD_WIN);

            MoveToBottomRight();

            this.activityChecker = new ActivityChecker(new Win32LastActivityProbe(), new SystemClock());

            var defaultPolicies = new TimeTablePolicies() {IdleTimeout = TimeSpan.FromMinutes(5), IdleTimeoutPenalty = TimeSpan.FromSeconds(30)};
            TimeTable timeTable = new TimeTable(defaultPolicies);
            this.activityChecker.setActivityListner(timeTable);
            this.statsReporter = new StatsReporterImpl(timeTable, defaultPolicies.IdleTimeout);
        }

        //##################################################################

        private void timer1_Tick(object sender, EventArgs e)
        {
            activityChecker.check();
            UpdateStats(statsReporter.GetStats(DateTime.Now.Date));
        }

        private void UpdateStats(Stats stats)
        {
            UpdateLabels(
                (int) stats.TotalToday.TotalSeconds, 
                (int) stats.LastBreak.TotalSeconds
                );

            Period currentPeriod = stats.CurrentPeriod;
            UpdateNotifyIcon(currentPeriod is ActivePeriod, (int)currentPeriod.Length.TotalMinutes);
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
            if (currentPeriodIcon != null)
                PInvoke.DestroyIcon(currentPeriodIcon.Handle);

            using (Graphics g = Graphics.FromImage(funBmp))
            {
                g.FillEllipse(Brushes.Green, 0, 0, 16, 16);
                g.DrawString(minutes.ToString(), font, Brushes.White, 0, 1);
            }
            currentPeriodIcon = Icon.FromHandle(funBmp.GetHicon());
        }

        private void UpdateAlldayIcon(int hours, int minutes)
        {
            Bitmap alldayBmp = new Bitmap(16, 16);
            if (alldayIcon != null)
                PInvoke.DestroyIcon(alldayIcon.Handle);

            using (Graphics g = Graphics.FromImage(alldayBmp))
            {
                g.FillRectangle(Brushes.White, 0, 0, 16, 16);
                string hoursStr = (hours > 9) ? "X" : "" + hours;
                g.DrawString(hoursStr, font, Brushes.Black, -1, -3);
                g.DrawString(""+minutes, fontSmall, Brushes.Red, 3, 6);
            }
            alldayIcon = Icon.FromHandle(alldayBmp.GetHicon());
            notifyIconAllday.Icon = alldayIcon;
        }

        private void UpdateNotifyIcon(bool active, int totalMinutes)
        {
            RecreateCurrentPeriodIcon(totalMinutes);
            notifyIcon1.Icon = !active ? idleIcon : currentPeriodIcon;
        }

        private void UpdateLabels(int secondsToday, int secondsAfterLastBreak)
        {
            todayTimeLabel.Text  = Helpers.SecondsToHMS(secondsToday);
            lastBreakLabel.Text = " "+(secondsAfterLastBreak / 60) + " min";
            
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

        private void toggleVisible()
        {
            if (WindowState == FormWindowState.Minimized)
            {

                WindowState = FormWindowState.Normal;
                Show();
                MoveToBottomRight();
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
            else if (e.Button == MouseButtons.Middle)
            {
                MoveToBottomRight();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == ShortcutsManager.WM_HOTKEY)
            {
                if ((int)m.WParam == (int)Keys.A)
                {
                    toggleVisible();
                }
            }
        }

        private void MoveToBottomRight()
        {
            Width = initialWidth;
            Height = initialHeight;
            Left = Screen.PrimaryScreen.WorkingArea.Width - Width - 10;
            Top = Screen.PrimaryScreen.WorkingArea.Height - Height - 10;
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

        private void historyButton_Click(object sender, EventArgs e)
        {
            new HistoryForm().Show();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            ShortcutsManager.UnregisterHotKey(this, (int)Keys.Z);
            ShortcutsManager.UnregisterHotKey(this, (int)Keys.A);
            Properties.Settings.Default.Save();
        }
    }
}
