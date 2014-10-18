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

    public static class PeriodExtensionMethods
    {
        public static PeriodBuilder Length(this DateTime dateTime, TimeSpan length)
        {
            return PeriodBuilder.New(dateTime).Length(length);
        }

        public static PeriodBuilder NewPeriodAfter(this Period period, TimeSpan gap)
        {
            return PeriodBuilder.NewAfter(period, gap);
        }
    }

    public class ClockStub : Clock
    {
        private DateTime currentTime;
        private Queue<TimeSpan> queue;
        private TimeSpan interval = 1.secs();

        public void Arrange(DateTime startDateTime, params TimeSpan[] dateTimes)
        {
            currentTime = startDateTime;
            queue = new Queue<TimeSpan>(dateTimes);
        }

        public void Arrange(DateTime startDateTime)
        {
            currentTime = startDateTime;
        }

        public DateTime CurrentTime()
        {
            return currentTime;
        }

        public void NextValue()
        {
            if (queue == null)
                currentTime += interval;
            else
                currentTime += queue.Dequeue();
        }
    }

    public class LastActivityProbeStub : LastActivityProbe
    {
        private Queue<int> queue;

        public void Arrange(params int[] dateTimes)
        {
            queue = new Queue<int>(dateTimes);
        }

        public int GetLastInputTick()
        {
            return queue.Peek();
        }

        public void NextValue()
        {
            queue.Dequeue();
        }
    }
}