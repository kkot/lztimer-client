using System;
using System.Windows.Forms;
using System.Drawing;
using System.Media;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace kkot.LzTimer
{
    public partial class MainWindow : Form, UserActivityListner
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        private HttpListener http;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            log.Info("Form load");
            idleIcon = CreateIdleIcon();
            UpdateNotifyIcon(false, 0);

            // loading configuration
            int maxIdleMinutes = int.Parse(Properties.Settings.Default.MaxIdleMinutes.ToString());
            intervalTextBox.Text = maxIdleMinutes.ToString();

            soundPlayer = new SoundPlayer();

            // register shotcuts keys
            shortcutsManager = new ShortcutsManager(this);
            shortcutsManager.Register();

            MoveToPosition();

            this.inputProbe = new HookActivityProbe();
            this.activityChecker = new ActivityChecker(inputProbe, new SystemClock());
            this.policies = new TimeTablePolicies { IdleTimeout = maxIdleMinutes.mins() };

            // for testing
            //this.policies = new TimeTablePolicies { IdleTimeout = 1.secs() };

            periodStorage = new SqlitePeriodStorage("periods.db");
            var timeTable = new TimeTable(policies, periodStorage);
            this.activityChecker.SetActivityListner(timeTable);
            this.statsReporter = new StatsReporterImpl(timeTable, policies, new SystemClock());
            timeTable.RegisterUserActivityListener(this);

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

        private Icon RecreateCurrentPeriodIcon(int minutes)
        {
            if (minutes > 99)
                minutes = 99;

            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.FillEllipse(getCurrentTimeIconColor(minutes), 0, 0, 16, 16);
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

        internal void toggleVisible()
        {
            log.Info("WindowState " + WindowState);
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
                toggleVisible();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (shortcutsManager != null)
            {
                shortcutsManager.ProcessMessage(ref m);
            }
        }

        private void MoveToPosition()
        {
            log.Debug("Height " + Height);
            log.Debug("ClientSize.Height " + ClientSize.Height);

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
            string text = intervalTextBox.Text;
            try
            {
                Properties.Settings.Default.MaxIdleMinutes = int.Parse(text);
                Properties.Settings.Default.Save();
            }
            catch (FormatException)
            {
                // ignore
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
            log.Debug("Notify after break " + leaveTime);
            const int balloonTimeoutMs = 10000;
            notifyIcon1.ShowBalloonTip(balloonTimeoutMs, "leave", string.Format("time: {0:%h} h {0:%m} m {0:%s} s", leaveTime), ToolTipIcon.Info);
        }

        private int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void output(string message)
        {
            Console.WriteLine(message);
        }

        private async void signInWithGoogle_Click(object sender, EventArgs e)
        {
            this.http = new HttpListener();
            http.Start();

            var port = GetRandomUnusedPort();
            var redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, port);
            http.Prefixes.Add(redirectURI);

            var authorizationEndpoint = "http://localhost:8080/signin/desktop/google";

            var authorizationRequest = string.Format("{0}?redirect_uri={1}&port={2}",
                authorizationEndpoint, System.Uri.EscapeDataString(redirectURI), port);

            System.Diagnostics.Process.Start(authorizationRequest);

            while (http.IsListening)
            {
                var context = http.GetContext();
                ProcessContext(context);
            }

            // Brings this app back to the foreground.
            this.Activate();
        }

        private void ProcessContext(HttpListenerContext context)
        {
            // Sends an HTTP response to the browser.  
            var response = context.Response;
            var request = context.Request;
            response.AppendHeader("Access-Control-Allow-Origin", "*");

            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                response.AddHeader("Access-Control-Max-Age", "1728000");
                context.Response.StatusCode = 200;
                context.Response.OutputStream.Close();
            }
            else
            {
                // Get the data from the HTTP stream
                var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                output("Token: " + body);

                string responseString = string.Format("ok");
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var responseOutput = response.OutputStream;
                Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
                {
                    responseOutput.Close();
                    http.Stop();
                    Console.WriteLine("HTTP server stopped.");
                });
            }
        }
    }
}
