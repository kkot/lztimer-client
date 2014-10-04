using System;
using System.Collections.Generic;
using kkot.LzTimer;

namespace LzTimerTests
{
    public class PeriodBuilder
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public static TimeSpan DEFAULT_LENGTH_MS = 1000.milisec();

        private PeriodBuilder(Period period)
        {
            this.Start = period.Start;
            this.End = period.End;
        }

        private PeriodBuilder(DateTime start, DateTime end)
        {
            this.Start = start;
            this.End = end;
        }

        public static PeriodBuilder New(DateTime start)
        {
            var periodBuilder = new PeriodBuilder(
                start, 
                start + DEFAULT_LENGTH_MS);
            return periodBuilder;
        }

        public static PeriodBuilder New()
        {
            return new PeriodBuilder(DateTime.Now, DateTime.Now + DEFAULT_LENGTH_MS);
        }

        private static PeriodBuilder After(Period period, TimeSpan gap)
        {
            var periodBuilder = New(period.End + gap);
            return periodBuilder;
        }

        public static PeriodBuilder NewAfter(Period period)
        {
            return After(period, TimeSpan.Zero);
        }

        public static PeriodBuilder NewAfter(Period period, TimeSpan gap)
        {
            return After(period, gap);
        }
        public PeriodBuilder NewAfter(TimeSpan gap)
        {
            return After(Active(), gap);
        }

        public PeriodBuilder Length(TimeSpan length)
        {
            End = Start + length;
            return this;
        }

        public IdlePeriod Idle()
        {
            return new IdlePeriod(Start, End);
        }

        public ActivePeriod Active()
        {
            return new ActivePeriod(Start, End);
        }
    }

    public class ClockStub : Clock
    {
        private Queue<DateTime> queue;

        public void Arrange(params DateTime[] dateTimes)
        {
            queue = new Queue<DateTime>(dateTimes);
        }

        public DateTime CurrentTime()
        {
            return queue.Dequeue();
        }
    }

    public class SimpleClock : Clock
    {
        private DateTime currentTime;

        public void StartTime(DateTime dateTime)
        {
            this.currentTime = dateTime;
        }

        public DateTime CurrentTime()
        {
            return currentTime;
        }

        public void NextValue()
        {
            currentTime += 1.secs();
        }
    }

    public class LastActivityProbeStub : LastActivityProbe
    {
        private Queue<int> queue;

        public void Arrange(params int[] dateTimes)
        {
            queue = new Queue<int>(dateTimes);
        }

        public int getLastInputTick()
        {
            return queue.Peek();
        }

        public void NextValue()
        {
            queue.Dequeue();
        }
    }
}