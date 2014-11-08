using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace kkot.LzTimer
{
    public class TimePeriod : IComparable
    {
        public TimePeriod(DateTime @start, DateTime end)
        {
            this.Start = start;
            this.End = end;
            this.Length = end - start;
        }

        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public TimeSpan Length { get; private set; }

        public override string ToString()
        {
            return "["+Start.TimeOfDay+" "+End.TimeOfDay+" length "+Length+"]";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var period = (Period) obj;
            return Start.Equals(period.Start) && End.Equals(period.End);     
        }

        public override int GetHashCode()
        {
            return Start.GetHashCode()+End.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            Period period = (Period) obj;
            return Start.CompareTo(period.Start);
        }
    }

    public class Period : TimePeriod
    {
        protected Period(DateTime @start, DateTime end) : base(start, end)
        {
        }

        public static Period Create(bool active, DateTime @start, DateTime end)
        {
            if (active)
                return new ActivePeriod(start, end);
            else
                return new IdlePeriod(start, end);
        }

        public bool CanBeMerged(Period aPeriod, TimeSpan aTimeoutPeriod)
        {
            if (GetType() != aPeriod.GetType())
            {
                return false;
            }

            // 500 ms is little hacky
            var mergeGap = (aPeriod is ActivePeriod) ? aTimeoutPeriod : TimeSpan.FromMilliseconds(500);

            if (((End - aPeriod.Start).Duration() <= mergeGap) ||
                ((Start - aPeriod.End).Duration() <= mergeGap))
            {
                return true;
            }
            return false;
        }

        virtual public Period Merge(Period aPeriod)
        {
            var start = Start < aPeriod.Start ? Start : aPeriod.Start;
            var end = End > aPeriod.End ? End : aPeriod.End;
            return new Period(start, end);
        }

        public bool Overlap(Period other)
        {
            if (this.Start >= other.End ||
                this.End <= other.Start)
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var period = (Period)obj;
            return Start.Equals(period.Start) && End.Equals(period.End);
        }
    }

    public class ActivePeriod : Period
    {
        public ActivePeriod(DateTime start, DateTime end) : base(start, end) {}

        public override Period Merge(Period period)
        {
            Period merged = base.Merge(period);
            return new ActivePeriod(merged.Start, merged.End);
        }
    }

    public class IdlePeriod : Period
    {
        public IdlePeriod(DateTime start, DateTime end)
            : base(start, end) {}

        public override Period Merge(Period period)
        {
            Period merged = base.Merge(period);
            return new IdlePeriod(merged.Start, merged.End);
        }
    }

    public interface ActivityPeriodsListener
    {
        void PeriodPassed(Period period);
    }

    public interface PeriodsReader
    {
        SortedSet<Period> GetPeriodsAfter(DateTime dateTime);
    }

    public interface PeriodStorage : IDisposable
    {
        void Add(Period period);
        void Remove(Period period);
        SortedSet<Period> GetPeriodsFromTimePeriod(TimePeriod searchedTimePeriod);
        SortedSet<Period> GetPeriodsAfter(DateTime dateTime);
        void Reset();
    }

    public interface TestablePeriodStorage : PeriodStorage
    {
        SortedSet<Period> GetAll();
    }

    public class TimeTablePolicies
    {
        public TimeSpan IdleTimeout { get; set; }
    }

    public class TimeTable : ActivityPeriodsListener, PeriodsReader
    {
        private readonly PeriodStorage periodStorage;
        private readonly TimeTablePolicies policies;

        public TimeTable(TimeTablePolicies policies) : this(policies, new MemoryPeriodStorage())
        {
            
        }

        public TimeTable(TimeTablePolicies policies, PeriodStorage storage)
        {
            this.policies = policies;
            this.periodStorage = storage;
        }

        public Period Add(Period period)
        {
            return merge(period);   
        }

        private Period merge(Period aPeriod)
        {
            foreach (var period in periodStorage.GetPeriodsAfter(aPeriod.Start - policies.IdleTimeout))
            {
                if (period.CanBeMerged(aPeriod, policies.IdleTimeout))
                {
                    var merged = period.Merge(aPeriod);
                    foreach(Period innerPeriod in periodStorage.GetPeriodsFromTimePeriod(merged))
                    {
                        periodStorage.Remove(innerPeriod);
                    }
                    periodStorage.Add(merged);
                    return merged;
                }
            }
            periodStorage.Add(aPeriod);
            return aPeriod;
        }

        public void PeriodPassed(Period period)
        {
            this.Add(period);
        }

        public SortedSet<Period> GetPeriodsFromPeriod(TimePeriod period)
        {
            return periodStorage.GetPeriodsFromTimePeriod(period);
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            return periodStorage.GetPeriodsAfter(dateTime);
        }
    }

    public interface StatsReporter
    {
        TimeSpan GetTotalActiveToday(DateTime todayBegin);

        TimeSpan GetLastInactiveTimespan();

        Period GetCurrentLogicalPeriod();
    }

    public class StatsReporterImpl : StatsReporter
    {
        private readonly PeriodsReader periodReader;
        private TimeTablePolicies policies;
        private Clock clock;

        public StatsReporterImpl(PeriodsReader periodsReader, TimeTablePolicies policies, Clock clock)
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
            return periods.Where(e => e is ActivePeriod).Select(p => (ActivePeriod) p).ToList();
        }

        private Period Last(List<Period> periods, int i = 1)
        {
            if (periods.Count >= i)
              return periods[periods.Count-i];

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
    }
}
