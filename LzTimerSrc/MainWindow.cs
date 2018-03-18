using System;
using System.Windows.Forms;
using System.Drawing;
using System.Media;

namespace kkot.LzTimer
{
    public partial class MainWindow : Form, 
        UserActivityListner, IWindowActivator, IDataSenterSettingsProvider, IDataSentStatusReceiver
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Font font = new Font(FontFamily.GenericMonospace, 11, GraphicsUnit.Pixel);
        private readonly Font fontSmall = new Font(FontFamily.GenericMonospace, 9, GraphicsUnit.Pixel);

        private const int TIME_LIMIT_NORMAL = 50;
        private const int TIME_LIMIT_WARNING = 60;
        private const int PERIOD_LENGTH_MS = 1000;

        private Icon idleIcon;
        private Icon currentTimeIcon;
        private Icon alldayTimeIcon;

        private SoundPlayer soundPlayer;
        private StatsReporter statsReporter;
        private ActivityChecker activityChecker;
        private TimeTablePolicies policies;
        private PeriodStorage periodStorage;
        private ShortcutsManager shortcutsManager;
        private HistoryWindow historyWindow;
        private HookActivityProbe inputProbe;
        private TokenReceiver tokenReceiver;
        private DataSender dataSender;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Log.Info("Form load");
            idleIcon = CreateIdleIcon();
            UpdateNotifyIcon(false, 0);

            // loading configuration
            var maxIdleMinutes = int.Parse(Properties.Settings.Default.MaxIdleMinutes.ToString());
            intervalTextBox.Text = maxIdleMinutes.ToString();

            var serverAddress = Properties.Settings.Default.ServerAddress;
            serverTextBox.Text = serverAddress;

            var task = Properties.Settings.Default.Task;
            taskTextBox.Text = task;

            // register shotcuts keys
            shortcutsManager = new ShortcutsManager(this);
            shortcutsManager.Register();

            MoveToPosition();

            soundPlayer = new SoundPlayer();
            inputProbe = new HookActivityProbe();
            activityChecker = new ActivityChecker(inputProbe, new SystemClock());

            // for testing
            //this.policies = new TimeTablePolicies { IdleTimeout = 1.secs() };
            policies = new TimeTablePolicies { IdleTimeout = maxIdleMinutes.mins() };

            periodStorage = new SqlitePeriodStorage("periods.db");
            var timeTable = new TimeTable(policies, periodStorage);
            activityChecker.SetActivityListner(timeTable);
            statsReporter = new StatsReporterImpl(timeTable, policies, new SystemClock());
            timeTable.RegisterUserActivityListener(this);
            tokenReceiver = new TokenReceiver(this, this);
            dataSender = new DataSender(tokenReceiver, periodStorage, this, this);
            timeTable.RegisterTimeTableUpdateListener(dataSender);

            timer1.Interval = PERIOD_LENGTH_MS;
            timer1.Enabled = true;
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
                (int)reporter.GetTotalActiveToday(DateTime.Now.Date).Round(100.ms()).TotalSeconds,
                (int)reporter.GetLastInactiveTimespan().Round(100.ms()).TotalSeconds
                );

            ActivityPeriod currentActivityPeriod = reporter.GetCurrentLogicalPeriod();
            UpdateNotifyIcon(currentActivityPeriod is ActivePeriod, (int)currentActivityPeriod.Length.TotalMinutes);
            UpdateAlldayIcon(reporter.GetTotalActiveToday(DateTime.Now.Date));
            UpdateHistoryWindow();
        }

        private void UpdateHistoryWindow()
        {
            if (historyWindow == null)
            {
                return;
            }
            if (historyWindow.IsDisposed)
            {
                historyWindow = null;
                return;
            }
            historyWindow.UpdateStats();
        }

        private static Brush GetCurrentTimeIconColor(int minutes)
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

        private Icon RecreateCurrentPeriodIcon(int minutes)
        {
            if (minutes > 99)
                minutes = 99;

            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.FillEllipse(GetCurrentTimeIconColor(minutes), 0, 0, 16, 16);
                g.DrawString(minutes.ToString(), font, Brushes.White, 0, 1);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private static Icon CreateIdleIcon()
        {
            var idleBmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(idleBmp))
            {
                g.FillEllipse(Brushes.Gray, 0, 0, 16, 16);
            }
            return Icon.FromHandle(idleBmp.GetHicon());
        }

        private void UpdateAlldayIcon(TimeSpan totalToday)
        {
            int hours = totalToday.Hours;
            int minutes = totalToday.Minutes;

            var prevIcon = alldayTimeIcon;
            alldayTimeIcon = CreateAlldayIcon(hours, minutes);
            notifyIconAllday.Icon = alldayTimeIcon;

            if (prevIcon != null)
                Win32.DestroyIcon(prevIcon.Handle);
        }

        private Icon CreateAlldayIcon(int hours, int minutes)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(Brushes.Wheat, 0, 0, 16, 16);
                var hoursStr = (hours > 9) ? "X" : "" + hours;
                g.DrawString(hoursStr, font, Brushes.Black, -1, -3);
                g.DrawString("" + minutes, fontSmall, Brushes.Red, 3, 6);
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private void UpdateNotifyIcon(bool active, int totalMinutes)
        {
            var prevIcon = currentTimeIcon;

            currentTimeIcon = RecreateCurrentPeriodIcon(totalMinutes);
            notifyIcon1.Icon = !active ? idleIcon : currentTimeIcon;

            if (prevIcon != null)
                Win32.DestroyIcon(prevIcon.Handle);
        }

        private void UpdateLabels(int secondsToday, int secondsAfterLastBreak)
        {
            todayTimeLabel.Text = Utilities.SecondsToHMS(secondsToday);
            lastBreakLabel.Text = Utilities.SecondsToHMS(secondsAfterLastBreak);

            var notifyText = "today " + todayTimeLabel.Text
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

        internal void ToggleVisible()
        {
            Log.Info("WindowState " + WindowState);
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
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
                ToggleVisible();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            shortcutsManager?.ProcessMessage(ref m);
        }

        private void MoveToPosition()
        {
            Log.Debug("Height " + Height);
            Log.Debug("ClientSize.Height " + ClientSize.Height);

            if (ClientSize.Height <= 0)
            {
                return;
            }

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
            try
            {
                Properties.Settings.Default.MaxIdleMinutes = int.Parse(intervalTextBox.Text);
                Properties.Settings.Default.Save();
            }
            catch (FormatException exception)
            {
                Log.Error("error while saving MaxIdleMintues property", exception);
            }
        }

        private void serverTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.ServerAddress = serverTextBox.Text.Trim();
                Properties.Settings.Default.Save();
            }
            catch (FormatException exception)
            {
                Log.Error("error while saving ServerAddress property", exception);
            }
        }

        private void taskTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Properties.Settings.Default.Task = taskTextBox.Text.Trim();
                Properties.Settings.Default.Save();
            }
            catch (FormatException exception)
            {
                Log.Error("error while saving ServerAddress property", exception);
            }

        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            shortcutsManager.UnRegister();
            Properties.Settings.Default.Save();
            periodStorage.Dispose();
            inputProbe.Dispose();
        }

        public void NotifyActiveAfterBreak(TimeSpan leaveTime)
        {
            Log.Debug("Notify after break " + leaveTime);
            const int balloonTimeoutMs = 10000;
            notifyIcon1.ShowBalloonTip(balloonTimeoutMs, "leave", string.Format("time: {0:%h} h {0:%m} m {0:%s} s", leaveTime), ToolTipIcon.Info);
        }

        private async void signInWithGoogle_Click(object sender, EventArgs e)
        {
            Log.Info("SignInWithGoogle clicked");
            tokenReceiver.LogInWithGoogle();
        }

        public void ActivateInUiThread()
        {
            Invoke(new Action(Activate));
        }

        public string ServerAddress => serverTextBox.Text;

        public string TaskName => taskTextBox.Text;
        public void Report(string status)
        {
            connectionStatus.Text = status;
        }
    }
}
