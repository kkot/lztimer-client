using System;
using kkot.LzTimer;

namespace LzTimerTests
{
    public class PeriodBuilder
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        private const int DEFAULT_LENGTH_MS = 1000;

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
                start.AddMilliseconds(DEFAULT_LENGTH_MS));
            return periodBuilder;
        }

        private static PeriodBuilder AfterMs(Period period, int miliseconds)
        {
            var periodBuilder = New(period.End.AddMilliseconds(miliseconds));
            return periodBuilder;
        }

        public static PeriodBuilder NewAfterMs(Period period, int miliseconds)
        {
            return AfterMs(period, miliseconds);
        }

        public static PeriodBuilder NewAfterSec(Period period, int seconds)
        {
            return AfterMs(period, toMili(seconds));
        }

        public static PeriodBuilder NewAfterMs(Period period1)
        {
            return NewAfterMs(period1, 0);
        }

        public PeriodBuilder LengthSecs(int seconds)
        {
            return LengthMs(toMili(seconds));
        }
        public PeriodBuilder LengthMs(int miliseconds)
        {
            End = Start.AddMilliseconds(miliseconds);
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

        private static int toMili(int seconds)
        {
            return seconds*1000;
        }
    }
}