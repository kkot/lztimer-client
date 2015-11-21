using System;
using System.Runtime.InteropServices;

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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly LastActivityProbe probe;
        private readonly Clock clock;

        private ActivityPeriodsListener activityPeriodsListener;
        private int? lastInputTick;
        private DateTime lastCheckTime;

        public ActivityChecker(LastActivityProbe probe, Clock clock)
        {
            this.clock = clock;
            this.probe = probe;
        }

        public void SetActivityListner(ActivityPeriodsListener listener)
        {
            activityPeriodsListener = listener;
        }

        public void Check()
        {
            var now = clock.CurrentTime();

            if (lastInputTick == null || IsAfterWakeUp())
            {
                SaveLastInputTick(now);
                return;
            }

            var wasActive = (lastInputTick != probe.GetLastInputTick());
            log.Debug("period was " + (wasActive ? "active" : "idle"));
            activityPeriodsListener.PeriodPassed(ActivityPeriod.Create(wasActive, now - TimeSpanSinceLastCheck(), now));
            
            SaveLastInputTick(now);
        }

        private bool IsAfterWakeUp()
        {
            return TimeSpanSinceLastCheck().Duration() > 2.s();
        }

        private TimeSpan TimeSpanSinceLastCheck()
        {
            return clock.CurrentTime() - lastCheckTime;
        }

        private void SaveLastInputTick(DateTime now)
        {
            lastCheckTime = now;
            lastInputTick = probe.GetLastInputTick();
        }
    }   

    public interface LastActivityProbe
    {
        int GetLastInputTick();
    }

    public class Win32LastActivityProbe : LastActivityProbe
    {
        public int GetLastInputTick()
        {
            PInvoke.LASTINPUTINFO lastInputInfo = new PInvoke.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            if (PInvoke.GetLastInputInfo(ref lastInputInfo))
            {
                return (int)lastInputInfo.dwTime;
            }
            throw new Exception();
        }
    }

    public static class PInvoke
    {
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(IntPtr handle);
    }
}