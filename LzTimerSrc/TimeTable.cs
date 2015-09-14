using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace kkot.LzTimer
{
    public interface ActivityPeriodsListener
    {
        void PeriodPassed(Period period);
    }

    public interface PeriodsInfoProvider
    {
        SortedSet<Period> GetPeriodsAfter(DateTime dateTime);
        SortedSet<Period> GetPeriods(TimePeriod period);
    }

    public class TimeTablePolicies
    {
        public TimeSpan IdleTimeout { get; set; }
    }

    public class TimeTable : ActivityPeriodsListener, PeriodsInfoProvider
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

        public SortedSet<Period> GetPeriods(TimePeriod period)
        {
            return periodStorage.GetPeriodsFromTimePeriod(period);
        }
    }

    interface UserActivityListner
    {
        void notifyActiveAfterBreak(TimeSpan leaveTime);
    }
}
