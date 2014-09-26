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
}