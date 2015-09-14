using System;
using System.Collections.Generic;
using kkot.LzTimer;

namespace LzTimerTests
{
    public class PeriodBuilder
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public static TimeSpan DEFAULT_LENGTH_MS = 1000.ms();

        private PeriodBuilder(ActivityPeriod activityPeriod)
        {
            this.Start = activityPeriod.Start;
            this.End = activityPeriod.End;
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

        private static PeriodBuilder After(ActivityPeriod activityPeriod, TimeSpan gap)
        {
            var periodBuilder = New(activityPeriod.End + gap);
            return periodBuilder;
        }

        public static PeriodBuilder NewAfter(ActivityPeriod activityPeriod)
        {
            return After(activityPeriod, TimeSpan.Zero);
        }

        public static PeriodBuilder NewAfter(ActivityPeriod activityPeriod, TimeSpan gap)
        {
            return After(activityPeriod, gap);
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

        public static PeriodBuilder New(this DateTime dateTime)
        {
            return PeriodBuilder.New(dateTime);
        }

        public static PeriodBuilder NewAfter(this ActivityPeriod activityPeriod, TimeSpan gap)
        {
            return PeriodBuilder.NewAfter(activityPeriod, gap);
        }
    }

    public class ArrangableClockStub : Clock
    {
        private DateTime currentTime;
        private Queue<TimeSpan> timeSpans;
        private TimeSpan interval = 1.s();

        public void Arrange(DateTime startDateTime, params TimeSpan[] timeSpans)
        {
            currentTime = startDateTime;

            if (timeSpans.Length > 0)
            {
                var q = AddZeroToBeginning(timeSpans);
                this.timeSpans = new Queue<TimeSpan>(q);                
            }
        }

        private static List<TimeSpan> AddZeroToBeginning(TimeSpan[] dateTimes)
        {
            var q = new List<TimeSpan>(dateTimes);
            q.Add(TimeSpan.Zero);
            return q;
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
            if (timeSpans == null)
                currentTime += interval;
            else
                currentTime += timeSpans.Dequeue();
        }
    }

    public class SimpleClockStub : Clock
    {
        private DateTime currentTime;

        public void Arrange(DateTime startDateTime)
        {
            currentTime = startDateTime;
        }

        public DateTime CurrentTime()
        {
            return currentTime;
        }

        public void SetValue(DateTime currentTime)
        {
            this.currentTime = currentTime;
        }
    }

    public class ArrangableLastActivityProbeStub : LastActivityProbe
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

    public class SimpleLastActivityProbeStub : LastActivityProbe
    {
        private int currentValue;

        public int GetLastInputTick()
        {
            return GetLastInputTick();
        }

        public void SetValue(int value)
        {
            this.currentValue = value;
        }
    }
}