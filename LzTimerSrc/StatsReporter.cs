using System;
using System.Collections.Generic;
using System.Linq;

namespace kkot.LzTimer
{
    public interface StatsReporter
    {
        TimeSpan GetTotalActiveToday(DateTime todayBegin);

        TimeSpan GetLastInactiveTimespan();

        Period GetCurrentLogicalPeriod();

        IList<Period> PeriodsFromDay(DateTime day);
    }

    public class StatsReporterImpl : StatsReporter
    {
        private readonly PeriodsInfoProvider periodReader;
        private TimeTablePolicies policies;
        private Clock clock;

        public StatsReporterImpl(PeriodsInfoProvider periodsReader, TimeTablePolicies policies, Clock clock)
        {
            this.periodReader = periodsReader;
            this.policies = policies;
            this.clock = clock;
        }

        private List<Period> ReadPeriodsAfter(DateTime date)
        {
            return periodReader.GetPeriodsAfter(date).ToList();
        }

        private List<Period> ReadPeriodsFromLast24h()
        {
            return periodReader.GetPeriodsAfter(clock.CurrentTime().AddDays(-1)).ToList();
        }

        public TimeSpan GetLastInactiveTimespan()
        {
            List<Period> periods = ReadPeriodsFromLast24h();

            if (periods.Count == 0)
                return TimeSpan.Zero;

            if (GetCurrentLogicalPeriod() is IdlePeriod)
                return GetCurrentLogicalPeriod().Length;

            if (ActivePeriods(periods).Count >= 2)
            {
                return LastActive(periods, 1).Start - LastActive(periods, 2).End;
            }
            else if (ActivePeriods(periods).Count == 1)
            {
                return LastActive(periods, 1).Start - periods[0].Start;
            }

            return TimeSpan.Zero;
        }

        private List<ActivePeriod> ActivePeriods(List<Period> periods)
        {
            return periods.Where(e => e is ActivePeriod).Select(p => (ActivePeriod)p).ToList();
        }

        private Period Last(List<Period> periods, int i = 1)
        {
            if (periods.Count >= i)
                return periods[periods.Count - i];

            return null;
        }

        private Period LastActive(List<Period> periods, int position)
        {
            return ActivePeriods(periods).Last(position);
        }

        public TimeSpan GetTotalActiveToday(DateTime todayBegin)
        {
            List<Period> periods = ReadPeriodsAfter(todayBegin);

            if (periods.Count == 0)
                return TimeSpan.Zero;

            var activePeriods = ActivePeriods(periods);

            var sum = new TimeSpan();
            foreach (ActivePeriod period in activePeriods)
            {
                sum += period.Length;
            }

            if (IsLastIdlePeriodTreatedAsActive(Last(periods)))
            {
                sum += Last(periods).Length;
            }

            return sum;
        }

        private bool IsLastIdlePeriodTreatedAsActive(Period last)
        {
            return last is IdlePeriod && last.Length <= policies.IdleTimeout;
        }

        public Period GetCurrentLogicalPeriod()
        {
            List<Period> periods = ReadPeriodsFromLast24h();

            if (periods.Count == 0)
                return new IdlePeriod(DateTime.Now, DateTime.Now);

            var last = Last(periods, 1);

            if (periods.Count == 1)
                return last;

            var beforeLast = Last(periods, 2);

            if (IsLastIdlePeriodTreatedAsActive(last))
                return beforeLast.Merge(last);
            else
                return last;
        }

        public IList<Period> PeriodsFromDay(DateTime day)
        {
            throw new NotImplementedException();
        }
    }
}
