using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace kkot.LzTimer
{
    /*
    interface UserActivityEventListner
    {
        void ActivityOccured(DateTime time);
        void SuspesionOccured(DateTime time);
        void TimePassed(DateTime time);
    }
     */

    public class Period : IComparable
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public TimeSpan Length { get; private set; }

        protected Period(DateTime @start, DateTime end)
        {
            this.Start = start;
            this.End = end;
            this.Length = end - start;
        }

        public bool CanBeMerged(Period aPeriod, int seconds)
        {
            if (GetType() != aPeriod.GetType())
            {
                return false;
            }

            if (Math.Abs((End - aPeriod.Start).TotalSeconds) <= seconds ||
                Math.Abs((Start - aPeriod.End).TotalSeconds) <= seconds)
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
        void ActivityPeriod(Period period);

        void IdlePeriod(Period period);

        void SuspendPeriod(Period period);
    }

    public class MergingPeriodList
    {
        private readonly IList<Period> periods = new List<Period>();

        public Period Add(Period period)
        {
            return merge(period);
        }

        private Period merge(Period aPeriod)
        {
            foreach (var period in periods.ToArray())
            {
                if (period.CanBeMerged(aPeriod, 1))
                {
                    periods.Remove(period);
                    var merged = period.Merge(aPeriod);
                    periods.Add(merged);
                    return merged;
                }
            }
            periods.Add(aPeriod);
            return aPeriod;
        }

        public List<Period> List
        {
            get { return new List<Period>(periods); } 
        }
    }

    interface PeriodStorage
    {
        void Add(Period period);
        void Remove(Period period);

        SortedSet<Period> getAll();
        SortedSet<Period> getFromPeriod(Period period);
        Period getLast();
        Period getLastActive();
    }

    class MemoryPeriodStorage : PeriodStorage
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

        public SortedSet<Period> getFromPeriod(Period period)
        {
            return new SortedSet<Period>(periods.Where(p => 
                p.Start >= period.Start && 
                p.End <= period.End));
        }

        public Period getLast()
        {
            return periods.Last();
        }

        public Period getLastActive()
        {
            return periods.
                Where(period => period.GetType() == typeof (ActivePeriod)).
                Max();
        }
    }

    public class TimeTable
    {
        private readonly PeriodStorage periodStorage = new MemoryPeriodStorage();
        private readonly int idleTimeoutSecs;
        private readonly int shortIdle;

        public TimeTable(int idleTimeoutSecs, int shortIdle)
        {
            this.idleTimeoutSecs = idleTimeoutSecs;
            this.shortIdle = shortIdle;
        }

        public Period Add(Period period)
        {
            return merge(period);
        }

        private Period merge(Period aPeriod)
        {
            foreach (var period in periodStorage.getAll())
            {
                if (period.CanBeMerged(aPeriod, idleTimeoutSecs))
                {
                    var merged = period.Merge(aPeriod);
                    foreach(Period innerPeriod in periodStorage.getFromPeriod(merged))
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

        public SortedSet<Period> List
        {
            get { return periodStorage.getAll(); }
        }

        public Period CurrentPeriod
        {
            get
            {
                var last = periodStorage.getLast();
                var lastActive = periodStorage.getLastActive();

                if (last is IdlePeriod && last.Length.Seconds < shortIdle)
                    return lastActive.Merge(last);
                else
                    return last;
            }
        }
    }

    public class ActivityPeriodsMerger : ActivityPeriodsListener
    {
        private ActivityPeriodsListener listener;
        private readonly MergingPeriodList mergingPeriodList = new MergingPeriodList();

        public ActivityPeriodsMerger()
        {
        }

        public void addActivityPeriodListener(ActivityPeriodsListener aListener)
        {
            this.listener = aListener;
        }

        public void ActivityPeriod(Period aPeriod)
        {
            var period = mergingPeriodList.Add(aPeriod);
            listener.ActivityPeriod(period);
        }

        public void IdlePeriod(Period period)
        {
            throw new NotImplementedException();
        }

        public void SuspendPeriod(Period period)
        {
            throw new NotImplementedException();
        }
    }

    class CurrentActivityMeasurer
    {
        private int activeTodaySecs;

        private int lastBreakSecs;

        private int activeSecs;
    }
}
