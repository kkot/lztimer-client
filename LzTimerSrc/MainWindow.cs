using System;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Media;
using kkot.LzTimer;

namespace kkot.LzTimer
{
    public partial class MainWindow : Form
    {
        private bool workingMode = false;
        private int secondsAfterLastActivity = 0;
        private int secondsAfterLastBreak = 0;
        private int prevLastInputTick = 0;
        private DateTime lastTickTime;

        private int initialWidth;
        private int initialHeight;

        //private Font font = new Font(FontFamily.GenericMonospace, 8.0f);
        private readonly Font font = new Font(FontFamily.GenericMonospace, 11, GraphicsUnit.Pixel);
        private readonly Font fontSmall = new Font(FontFamily.GenericMonospace, 9, GraphicsUnit.Pixel);

        private Icon idleIcon;
        private Icon funIcon;
        private Icon workIcon;
        private Icon alldayIcon;

        private SoundPlayer soundPlayer;
        private FullscreenDetector fullscreenDetector;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateFunWorkIcons(0);
            setNotifyIcon(null);
            addSecondsToActiveTime(0);

            initialHeight = Height;
            initialWidth = Width;
            prevLastInputTick = Helpers.GetLastInputTicks();

            // loading configuration
            intervalTextBox.Text = Properties.Settings.Default.MaxIdleMinutes.ToString();
            timer1.Interval = 1000;
            timer1.Enabled = true;

            soundPlayer = new SoundPlayer();
            fullscreenDetector = new FullscreenDetector();

            // rejestruje klawiszs
            ShortcutsManager.RegisterHotKey(this, Keys.Z, ShortcutsManager.MOD_WIN);
            ShortcutsManager.RegisterHotKey(this, Keys.A, ShortcutsManager.MOD_WIN);

            MoveToBottomRight();
        }

        private bool IsLastPeriodActive()
        {
            int lastInputTicks = Helpers.GetLastInputTicks();
            bool result = (lastInputTicks != prevLastInputTick) ? true : false;
            prevLastInputTick = lastInputTicks;

            if (fullscreenDetector.IsForegroundWindowFullScreen()) // tested with VLC and Smplayer
            {
                result = true;
                prevLastInputTick = 0; // value other than last one
            }

            // debug
            //if (result == true) soundPlayer.Play();

            return result;
        }

        //##################################################################

        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            if (lastTickTime.Ticks == 0)
            {
                lastTickTime = now;
                return;
            }

            bool lastPeriodActive = IsLastPeriodActive();

            // when the computer is suspended, this function is not called
            long milisecondsPassed = (now.Ticks - lastTickTime.Ticks) / 10000;
            lastTickTime = now;
            int secondsPassed;
            if (milisecondsPassed > 900 && milisecondsPassed < 1100)
                secondsPassed = 1;
            else
            {
                secondsPassed = (int)(milisecondsPassed / 1000);
                lastPeriodActive = false; // suspended
            }

            secondsAfterLastActivity += secondsPassed;
            secondsAfterLastBreak += secondsPassed;

            if (secondsAfterLastBreak % 60 == 1)
            {
                int minutes = (secondsAfterLastBreak / 60);
                UpdateFunWorkIcons(minutes);
            }

            if (secondsAfterLastActivity == (60 * Properties.Settings.Default.MaxIdleMinutes - 30))
            {
                soundPlayer.Play();
            }

            bool breakOccured = (secondsAfterLastActivity > (60 * Properties.Settings.Default.MaxIdleMinutes)) ? true : false;
            if (breakOccured)
            {
                setNotifyIcon(null);
                secondsAfterLastBreak = 0;
            }

            if (lastPeriodActive)
            {
                setNotifyIcon(workingMode);       
                if (! breakOccured)
                {
                    addSecondsToActiveTime(secondsAfterLastActivity);
                }
                else
                {
                    String lastBreakLength = Helpers.SecondsToHMS(secondsAfterLastActivity);
                    notifyIcon1.ShowBalloonTip(10, "leave", "time "+lastBreakLength, ToolTipIcon.Info);
                    addSecondsToActiveTime(30);
                }
                secondsAfterLastActivity = 0;
            }
        }

        private void UpdateFunWorkIcons(int minutes)
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
            Bitmap workBmp = new Bitmap(16, 16);
            if (funIcon != null)
                PInvoke.DestroyIcon(funIcon.Handle);
            if (workIcon != null)
                PInvoke.DestroyIcon(workIcon.Handle);

            using (Graphics g = Graphics.FromImage(funBmp))
            {
                g.FillEllipse(Brushes.Green, 0, 0, 16, 16);
                g.DrawString(minutes.ToString(), font, Brushes.White, 0, 1);
            }
            funIcon = Icon.FromHandle(funBmp.GetHicon());

            using (Graphics g = Graphics.FromImage(workBmp))
            {
                g.FillEllipse(Brushes.Red, 0, 0, 16, 16);
                g.DrawString(minutes.ToString(), font, Brushes.White, 0, 1);
            }
            workIcon = Icon.FromHandle(workBmp.GetHicon());
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

        private void setNotifyIcon(bool? workingMode)
        {
            if (workingMode == null)
            {
                notifyIcon1.Icon = idleIcon;
            }
            else if (workingMode == true)
            {
                notifyIcon1.Icon = workIcon;
            }
            else if (workingMode == false)
            {
                notifyIcon1.Icon = funIcon;
            }
        }

        class HoursMinutes
        {
            public int Hours { get; private set; }
            public int Minutes { get; private set; }

            public HoursMinutes(int seconds)
            {
                Hours = (seconds) / (60 * 60);
                Minutes = (seconds - Hours * 60 * 60) / (60);   
            }
        }

        private void addSecondsToActiveTime(int secondsAfterLastActivity)
        {
            String shortDateStr = Helpers.NowShortDateString();

            int numOfWorkSeconds;
            int numOfFunSeconds;
            DataStoreManager.ReadAndUpdateNumOfSeconds(workingMode, secondsAfterLastActivity, shortDateStr, out numOfWorkSeconds, out numOfFunSeconds);

            updateTimeLabels(numOfWorkSeconds, numOfFunSeconds);

            int totalTime = numOfWorkSeconds + numOfFunSeconds;
            var hoursMinutes = new HoursMinutes(totalTime);
            
            UpdateAlldayIcon(hoursMinutes.Hours, hoursMinutes.Minutes);
        }

        private void updateTimeLabels(int numOfWorkSeconds, int numOfFunSeconds)
        {
            int numOfSumSeconds = numOfWorkSeconds + numOfFunSeconds;

            workTimeLabel.Text = Helpers.SecondsToHMS(numOfWorkSeconds);
            funTimeLabel.Text  = Helpers.SecondsToHMS(numOfFunSeconds);
            sumTimeLabel.Text  = Helpers.SecondsToHMS(numOfSumSeconds);
            lastBreakLabel.Text = " "+(secondsAfterLastBreak / 60) + " min";
            
            string notifyText = "t " + sumTimeLabel.Text
                + "\n"
             //   + "\nf " + funTimeLabel.Text
             //   + "\nw " + workTimeLabel.Text // not used now
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
            else if (e.Button == MouseButtons.Right)
            {
                workingMode = !workingMode;
                setNotifyIcon(workingMode);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == ShortcutsManager.WM_HOTKEY)
            {
                if ((int)m.WParam == (int)Keys.Z)
                {
                    workingMode = !workingMode;
                    setNotifyIcon(workingMode);
                }
                else if ((int)m.WParam == (int)Keys.A)
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
