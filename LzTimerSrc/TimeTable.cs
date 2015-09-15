using System;
using System.Collections.Generic;

namespace kkot.LzTimer
{
    public interface ActivityPeriodsListener
    {
        void PeriodPassed(ActivityPeriod activityPeriod);
    }

    public interface PeriodsInfoProvider
    {
        SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime);
        SortedSet<ActivityPeriod> GetPeriods(Period period);
    }

    public interface UserActivityListner
    {
        void notifyActiveAfterBreak(TimeSpan leaveTime);
    }

    public class TimeTablePolicies
    {
        public TimeSpan IdleTimeout { get; set; }
    }

    public class TimeTable : ActivityPeriodsListener, PeriodsInfoProvider
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly PeriodStorage periodStorage;
        private readonly TimeTablePolicies policies;
        private UserActivityListner userActivityListner;

        public TimeTable(TimeTablePolicies policies, UserActivityListner userActivityListner = null) : this(
            policies,
            new MemoryPeriodStorage())
        {
        }

        public TimeTable(TimeTablePolicies policies, PeriodStorage periodStorage)
        {
            this.policies = policies;
            this.periodStorage = periodStorage;
        }

        public ActivityPeriod AddPeriod(ActivityPeriod activityPeriod)
        {
            log.Debug("AddPeriod " + activityPeriod);

            var mergedPeriod = merge(activityPeriod);
            log.Debug("merged period " + mergedPeriod);

            NotifyUserActivityListener(activityPeriod, mergedPeriod);
            return mergedPeriod;
        }

        private void NotifyUserActivityListener(ActivityPeriod activityPeriod, ActivityPeriod mergedPeriod)
        {
            if (userActivityListner == null)
                return;

            if (activityPeriod is IdlePeriod)
                return;

            var periodNotMerged = mergedPeriod.Equals(activityPeriod);
            if (!periodNotMerged)
                return;

            userActivityListner.notifyActiveAfterBreak(TimeSpan.Zero);
        }

        private ActivityPeriod merge(ActivityPeriod aActivityPeriod)
        {
            foreach (var period in periodStorage.GetPeriodsAfter(aActivityPeriod.Start - policies.IdleTimeout))
            {
                if (period.CanBeMerged(aActivityPeriod, policies.IdleTimeout))
                {
                    var merged = period.Merge(aActivityPeriod);
                    foreach (ActivityPeriod innerPeriod in periodStorage.GetPeriodsFromTimePeriod(merged))
                    {
                        periodStorage.Remove(innerPeriod);
                    }
                    periodStorage.Add(merged);
                    return merged;
                }
            }
            periodStorage.Add(aActivityPeriod);
            return aActivityPeriod;
        }

        public void PeriodPassed(ActivityPeriod activityPeriod)
        {
            AddPeriod(activityPeriod);
        }

        public SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime)
        {
            return periodStorage.GetPeriodsAfter(dateTime);
        }

        public SortedSet<ActivityPeriod> GetPeriods(Period period)
        {
            return periodStorage.GetPeriodsFromTimePeriod(period);
        }

        public void registerUserActivityListener(UserActivityListner listner)
        {
            userActivityListner = listner;
        }
    }
}