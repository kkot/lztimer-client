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

        List<Period> GetSinceFirstActivePeriodBefore(DateTime dateTime);
    }

    public interface PeriodStorage
    {
        void Add(Period period);
        void Remove(Period period);
        SortedSet<Period> GetAll();
        SortedSet<Period> GetPeriodsFromTimePeriod(TimePeriod period);
        SortedSet<Period> GetPeriodsAfter(DateTime dateTime);
        List<Period> GetSinceFirstActivePeriodBefore(DateTime dateTime);
        void Close();
    }

    public class MemoryPeriodStorage : PeriodStorage
    {
        private SortedSet<Period> periods = new SortedSet<Period>();

        public void Remove(Period period)
        {
            periods.Remove(period);
        }

        public SortedSet<Period> GetAll()
        {
            return periods;
        }

        public void Add(Period period)
        {
            periods.Add(period);
        }

        public SortedSet<Period> GetPeriodsFromTimePeriod(TimePeriod period)
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

        public List<Period> GetSinceFirstActivePeriodBefore(DateTime dateTime)
        {
            DateTime fromDate = periods.Where((p) => p.Start < dateTime).ToList().Last().Start;
            return periods.Where((p) => p.Start >= fromDate).ToList();
        }

        public void Close()
        {
            periods = null;
        }
    }

    public class TimeTablePolicies
    {
        public TimeSpan IdleTimeout { get; set; }

        public TimeSpan IdleTimeoutPenalty { get; set; } 
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
            foreach (var period in periodStorage.GetAll())
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

        public SortedSet<Period> getAll()
        {
            return periodStorage.GetAll();
        }

        public SortedSet<Period> GetPeriodsFromPeriod(TimePeriod period)
        {
            return periodStorage.GetPeriodsFromTimePeriod(period);
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            return periodStorage.GetPeriodsAfter(dateTime);
        }

        public List<Period> GetSinceFirstActivePeriodBefore(DateTime dateTime)
        {
            throw new NotImplementedException();
        }
    }

    public class Stats
    {
        public TimeSpan LastInactiveTimespan;
        public Period CurrentLogicalPeriod;
        public TimeSpan TotalActive;
    }

    public interface StatsReporter
    {
        Stats GetStatsAfter(DateTime startDateTime);
    }

    public class StatsReporterImpl : StatsReporter
    {
        private readonly PeriodsReader periodReader;
        private List<Period> periodsAfter;
        private TimeTablePolicies policies;
        private DateTime startDateTime;

        public StatsReporterImpl(PeriodsReader periodsReader, TimeTablePolicies policies)
        {
            this.periodReader = periodsReader;
            this.policies = policies;
        }

        public Stats GetStatsAfter(DateTime startDateTime)
        {
            this.periodsAfter = GetPeriodsAfter(startDateTime);
            this.startDateTime = startDateTime;
            return new Stats()
            {
                CurrentLogicalPeriod = GetCurrentPeriod(), 
                LastInactiveTimespan = GetLastInactiveTimespan(), 
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

        public TimeSpan GetLastInactiveTimespan()
        {
            if (periodsAfter.Count == 0)
                return TimeSpan.Zero;

            if (Last() is IdlePeriod && Last().Length > policies.IdleTimeout)
                return Last().Length;

            if (ActivePeriods().Count >= 2)
            {
                return LastActive(1).Start - LastActive(2).End;
            }
            return LastActive(1).Start- startDateTime;
        }

        private TimeSpan GetTotalActive()
        {
            if (periodsAfter.Count == 0)
                return TimeSpan.Zero;

            var activePeriods = ActivePeriods();

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

        private List<ActivePeriod> ActivePeriods()
        {
            return periodsAfter.Where(e => e is ActivePeriod).Select(p => (ActivePeriod) p).ToList();
        }

        private Period BeforeLast()
        {
            return Last(2);
        }

        private Period Last(int i = 1)
        {
            if (periodsAfter.Count >= i)
              return periodsAfter[periodsAfter.Count-i];

            return null;
        }

        private Period LastActive(int position)
        {
            return ActivePeriods().Last(position);
        }
    }
}
