using System;
using System.Runtime.InteropServices;

namespace kkot.LzTimer
{
    public interface Clock
    {
        DateTime CurrentTime();
    }

    public class UserActivityChecker
    {
        private readonly LastActivityProbe probe;
        private readonly Clock clock;

        private ActivityPeriodsListener listener;
        private int? lastInputTick;
        private DateTime lastCheck;

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
            int probedLastInputTick = probe.getLastInputTick();

            if (lastInputTick == null)
            {
                lastInputTick = probedLastInputTick;
                lastCheck = clock.CurrentTime();
                return;
            }

            DateTime currentDateTime = clock.CurrentTime();

            if (lastInputTick != probedLastInputTick)
            {
                listener.PeriodPassed(new ActivePeriod(lastCheck, currentDateTime));
            }
            else
            {
                listener.PeriodPassed(new IdlePeriod(lastCheck, currentDateTime));
            }
            lastInputTick = probedLastInputTick;
            lastCheck = currentDateTime;
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