using System;
using System.Collections.Generic;
using System.Linq;

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
            return "["+Start.TimeOfDay+" "+End.TimeOfDay+"]";
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

        public bool CanBeMerged(Period aPeriod, TimeSpan aTimeoutPeriod)
        {
            if (GetType() != aPeriod.GetType())
            {
                return false;
            }

            if (((End - aPeriod.Start).Duration() <= aTimeoutPeriod) ||
                ((Start - aPeriod.End).Duration() <= aTimeoutPeriod))
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

    public interface PeriodStorage
    {
        void Add(Period period);
        void Remove(Period period);
        SortedSet<Period> getAll();
        SortedSet<Period> GetPeriodsFromPeriod(TimePeriod period);
        SortedSet<Period> GetPeriodsAfter(DateTime dateTime);
    }

    public class MemoryPeriodStorage : PeriodStorage
    {
        private readonly SortedSet<Period> periods = new SortedSet<Period>();

        public void Remove(Period period)
        {
            periods.Remove(period);
        }

        public SortedSet<Period> getAll()
        {
            return periods;
        }

        public void Add(Period period)
        {
            periods.Add(period);
        }

        public SortedSet<Period> GetPeriodsFromPeriod(TimePeriod period)
        {
            return new SortedSet<Period>(periods.Where(p => 
                p.Start >= period.Start && 
                p.End <= period.End));
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            return new SortedSet<Period>(periods.Where(p =>
                p.Start >= dateTime));
        }
    }

    public class TimeTablePolicies
    {
        public TimeSpan IdleTimeout { get; set; }

        public TimeSpan IdleTimeoutPenalty { get; set; } 
    }

    public class TimeTable : ActivityPeriodsListener, PeriodsReader
    {
        private readonly PeriodStorage periodStorage = new MemoryPeriodStorage();
        private readonly TimeTablePolicies policies;

        public TimeTable(TimeTablePolicies policies)
        {
            this.policies = policies;
        }

        public Period Add(Period period)
        {
            return merge(period);
        }

        private Period merge(Period aPeriod)
        {
            foreach (var period in periodStorage.getAll())
            {
                if (period.CanBeMerged(aPeriod, policies.IdleTimeout))
                {
                    var merged = period.Merge(aPeriod);
                    foreach(Period innerPeriod in periodStorage.GetPeriodsFromPeriod(merged))
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

        public SortedSet<Period> getAll()
        {
            return periodStorage.getAll();
        }

        public SortedSet<Period> GetPeriodsFromPeriod(TimePeriod period)
        {
            return periodStorage.GetPeriodsFromPeriod(period);
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            return periodStorage.GetPeriodsAfter(dateTime);
        }
    }

    public class Stats
    {
        public TimeSpan LastBreak;
        public Period CurrentPeriod;
        public TimeSpan TotalActive;
    }

    public interface StatsReporter
    {
        Stats GetStatsAfter(DateTime date);
    }

    public class StatsReporterImpl : StatsReporter
    {
        private readonly PeriodsReader periodReader;
        private List<Period> periodsAfter;
        private TimeTablePolicies policies;

        public StatsReporterImpl(PeriodsReader periodsReader, TimeTablePolicies policies)
        {
            this.periodReader = periodsReader;
            this.policies = policies;
        }

        public Stats GetStatsAfter(DateTime date)
        {
            periodsAfter = GetPeriodsAfter(date);
            return new Stats()
            {
                CurrentPeriod = GetCurrentPeriod(), 
                LastBreak = GetLastBreak(), 
                TotalActive = GetTotalActive()
            };
        }

        private List<Period> GetPeriodsAfter(DateTime date)
        {
            return periodReader.GetPeriodsAfter(date).ToList();
        }

        public Period GetCurrentPeriod()
        {
            if (periodsAfter.Count == 0)
                return new IdlePeriod(DateTime.Now, DateTime.Now);

            if (periodsAfter.Count == 1)
                return Last();

            var last = Last();
            var beforeLast = BeforeLast();

            if (last is IdlePeriod && last.Length < policies.IdleTimeout)
                return beforeLast.Merge(last);
            else
                return last;
        }

        public TimeSpan GetLastBreak()
        {
            if (periodsAfter.Count == 0 || periodsAfter.Count == 1)
                return TimeSpan.Zero;

            var last = Last();
            var beforeLast = BeforeLast();

            if (last is IdlePeriod)
                return last.Length;
            else
                return beforeLast.Length;
        }

        private TimeSpan GetTotalActive()
        {
            if (periodsAfter.Count == 0)
                return TimeSpan.Zero;

            var activePeriods =
                periodsAfter.
                Where(e => e is ActivePeriod);

            var sum = new TimeSpan();
            foreach (ActivePeriod period in activePeriods)
            {
                sum += period.Length;
            }

            if (Last() is IdlePeriod && Last().Length <= policies.IdleTimeout)
            {
                sum += Last().Length;                
            }

            return sum;
        }

        private Period BeforeLast()
        {
            return periodsAfter[periodsAfter.Count - 2];
        }

        private Period Last()
        {
            return periodsAfter[periodsAfter.Count-1];
        }
    }
}
