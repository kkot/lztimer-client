using System;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

namespace kkot.LzTimer
{
    public interface Clock
    {
        DateTime CurrentTime();
    }

    public class SystemClock : Clock
    {
        public DateTime CurrentTime()
        {
            return DateTime.Now;
        }
    }

    public class ActivityChecker
    {
        private readonly LastActivityProbe probe;
        private readonly Clock clock;

        private ActivityPeriodsListener listener;
        private int? lastInputTick;
        private DateTime lastRealCheckTime;
        private DateTime lastSentCheckTime;

        public ActivityChecker(LastActivityProbe probe, Clock clock)
        {
            this.clock = clock;
            this.probe = probe;
        }

        public void setActivityListner(ActivityPeriodsListener listener)
        {
            this.listener = listener;
        }

        public void check()
        {
            int currentInputTick = probe.getLastInputTick();

            if (lastInputTick == null)
            {
                lastInputTick = currentInputTick;
                lastSentCheckTime = clock.CurrentTime();
                return;
            }

            TimeSpan length = 1.secs();
            TimeSpan difference = (lastSentCheckTime + length) - clock.CurrentTime();
            Console.WriteLine(difference);
            if (difference.Duration() > 1.secs())
            {
                if (difference > 0.secs())
                {
                    length = 0.secs();
                }
                if (difference < 0.secs())
                {
                    length = 2.secs();
                }
            }

            bool active = (lastInputTick != currentInputTick);

            listener.PeriodPassed(Period.Create(active, lastSentCheckTime, lastSentCheckTime + length));
            lastSentCheckTime = lastSentCheckTime + length;
            
            lastInputTick = currentInputTick;
        }
    }   

    public interface LastActivityProbe
    {
        int getLastInputTick();
    }

    public class Win32LastActivityProbe : LastActivityProbe
    {
        public int getLastInputTick()
        {
            int lastInputTicks = 0;

            PInvoke.LASTINPUTINFO lastInputInfo = new PInvoke.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            if (PInvoke.GetLastInputInfo(ref lastInputInfo))
            {
                lastInputTicks = (int)lastInputInfo.dwTime;
            }
            else
            {
                throw new Exception();
            }
            return lastInputTicks;
        }
    }
}