using System;
using System.Runtime.InteropServices;

namespace kkot.LzTimer
{
    public interface Clock
    {
        DateTime currentTime();
    }

    public class UserActivityChecker
    {
        private readonly LastActivityProbe probe;
        private readonly Clock clock;

        private ActivityPeriodsListener listener;
        private int? lastInputTime;

        public UserActivityChecker(LastActivityProbe probe, Clock clock)
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
            int probedLastInputTime = probe.getLastInputTime();

            if (lastInputTime == null)
            {
                lastInputTime = probedLastInputTime;
                return;
            }

            if (lastInputTime != probedLastInputTime)
            {
                listener.PeriodPassed(new ActivePeriod(new DateTime(), new DateTime()));
            }
            else
            {
                listener.PeriodPassed(new IdlePeriod(new DateTime(), new DateTime()));
            }
            lastInputTime = probedLastInputTime;
        }
    }

    public interface LastActivityProbe
    {
        int getLastInputTime();
    }

    public class Win32LastActivityProbe : LastActivityProbe
    {
        public int getLastInputTime()
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