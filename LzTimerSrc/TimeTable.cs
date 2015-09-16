﻿using System;
using System.Collections.Generic;
using System.Linq;

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

            var mergedPeriod = Merge(activityPeriod);
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

            var periodMerged = !mergedPeriod.Equals(activityPeriod);
            if (periodMerged)
                return;

            // TODO: refactor
            var mergeCandidates = GetMergeCandidates(activityPeriod);
            if (mergeCandidates.Count == 0
                || mergeCandidates.First().Start > (activityPeriod.Start - policies.IdleTimeout))
                return;

            userActivityListner.notifyActiveAfterBreak(TimeSpan.Zero);
        }

        private SortedSet<ActivityPeriod> GetMergeCandidates(ActivityPeriod currentPeriod)
        {
            var mergeTimeWindowStart = currentPeriod.Start - policies.IdleTimeout;
            var candidates = periodStorage.GetPeriodsAfter(mergeTimeWindowStart)
                .Where(period => !period.Equals(currentPeriod));
            return new SortedSet<ActivityPeriod>(candidates);
        }

        private ActivityPeriod Merge(ActivityPeriod activityPeriod)
        {
            foreach (var period in GetMergeCandidates(activityPeriod))
            {
                if (period.CanBeMerged(activityPeriod, policies.IdleTimeout))
                {
                    var merged = period.Merge(activityPeriod);
                    periodStorage.RemoveFromTimePeriod(merged);
                    periodStorage.Add(merged);
                    return merged;
                }
            }
            periodStorage.Add(activityPeriod);
            return activityPeriod;
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