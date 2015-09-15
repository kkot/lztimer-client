using System;
using System.Collections.Generic;
using System.Linq;

namespace kkot.LzTimer
{
    public interface StatsReporter
    {
        TimeSpan GetTotalActiveToday(DateTime todayBegin);

        TimeSpan GetLastInactiveTimespan();

        ActivityPeriod GetCurrentLogicalPeriod();

        SortedSet<ActivityPeriod> PeriodsFromDay(DateTime day);
    }

    public class StatsReporterImpl : StatsReporter
    {
        private readonly PeriodsInfoProvider periodReader;
        private readonly TimeTablePolicies policies;
        private readonly Clock clock;

        public StatsReporterImpl(PeriodsInfoProvider periodsReader, TimeTablePolicies policies, Clock clock)
        {
            this.periodReader = periodsReader;
            this.policies = policies;
            this.clock = clock;
        }

        private List<ActivityPeriod> ReadPeriodsAfter(DateTime date)
        {
            return periodReader.GetPeriodsAfter(date).ToList();
        }

        private List<ActivityPeriod> ReadPeriodsFromLast24h()
        {
            return periodReader.GetPeriodsAfter(clock.CurrentTime().AddDays(-1)).ToList();
        }

        public TimeSpan GetLastInactiveTimespan()
        {
            List<ActivityPeriod> periods = ReadPeriodsFromLast24h();

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

        private List<ActivePeriod> ActivePeriods(List<ActivityPeriod> periods)
        {
            return periods.OfType<ActivePeriod>().ToList();
        }

        private ActivityPeriod Last(List<ActivityPeriod> periods, int i = 1)
        {
            if (periods.Count >= i)
                return periods[periods.Count - i];

            return null;
        }

        private ActivityPeriod LastActive(List<ActivityPeriod> periods, int position)
        {
            return ActivePeriods(periods).Last(position);
        }

        public TimeSpan GetTotalActiveToday(DateTime todayBegin)
        {
            List<ActivityPeriod> periods = ReadPeriodsAfter(todayBegin);

            if (periods.Count == 0)
                return TimeSpan.Zero;

            var activePeriods = ActivePeriods(periods);
            var sum = activePeriods.Aggregate(new TimeSpan(), (current, period) => current + period.Length);

            if (IsLastIdlePeriodTreatedAsActive(Last(periods)))
            {
                sum += Last(periods).Length;
            }

            return sum;
        }

        private bool IsLastIdlePeriodTreatedAsActive(ActivityPeriod last)
        {
            return last is IdlePeriod && last.Length <= policies.IdleTimeout;
        }

        public ActivityPeriod GetCurrentLogicalPeriod()
        {
            List<ActivityPeriod> periods = ReadPeriodsFromLast24h();

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

        public SortedSet<ActivityPeriod> PeriodsFromDay(DateTime day)
        {
            if (day.Date != day)
            {
                throw new ArgumentException("Only date without time should be passed");
            }
            var startDateTime = day;
            var endDateTime = day.AddDays(1);
            return periodReader.GetPeriods(new Period(startDateTime, endDateTime));
        }
    }
}
