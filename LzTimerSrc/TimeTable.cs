using System;
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
        void NotifyActiveAfterBreak(TimeSpan leaveTime);
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

        public TimeTable(TimeTablePolicies policies) : this(
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
            log.Debug("add period " + activityPeriod);
            var mergedPeriod = Merge(activityPeriod);

            NotifyUserActivityListener(activityPeriod, mergedPeriod);
            return mergedPeriod;
        }

        private void NotifyUserActivityListener(ActivityPeriod activityPeriod, ActivityPeriod mergedPeriod)
        {
            if (userActivityListner == null)
            {
                log.Debug("listener is null");
                return;
            }

            if (activityPeriod is IdlePeriod)
            {
                log.Debug("period is idle");
                return;
            }

            var periodMerged = !mergedPeriod.Equals(activityPeriod);
            if (periodMerged)
            {
                log.Debug("period merged");
                return;
            }

            var periodBefore = periodStorage.GetPeriodBefore(activityPeriod.Start);
            if (!(periodBefore is IdlePeriod))
            {
                log.Debug("period before was not idle");
                return;
            }

            if (periodBefore.Length < policies.IdleTimeout)
            {
                log.Debug("period before too short " + periodBefore.Length + " expected " + policies.IdleTimeout);
                return;
            }

            if (!periodBefore.IsDirectlyBefore(activityPeriod))
            {
                log.Debug("period before is not directly before, before " + periodBefore + ", added" + activityPeriod);
                return;
            }

            userActivityListner.NotifyActiveAfterBreak(periodBefore.Length);
        }

        private SortedSet<ActivityPeriod> GetMergeCandidates(ActivityPeriod currentPeriod)
        {
            var mergeTimeWindowStart = currentPeriod.Start - policies.IdleTimeout;
            var candidates = periodStorage.GetPeriodsAfter(mergeTimeWindowStart)
                .Where(period => !period.Equals(currentPeriod));
            return new SortedSet<ActivityPeriod>(candidates);
        }

        private ActivityPeriod FindMergablePeriod(ActivityPeriod period, SortedSet<ActivityPeriod> candidates)
        {
            if (candidates.Count == 0)
            {
                return null;
            }
            if (period is ActivePeriod)
            {
                foreach (var candidatePeriod in candidates)
                {
                    if (candidatePeriod.IsCloseAndSameType(period, policies.IdleTimeout))
                    {
                        return candidatePeriod;
                    }
                }
            }
            if (period is IdlePeriod)
            {
                var candidatePeriod = candidates.Last();
                if (candidatePeriod.IsCloseAndSameType(period, policies.IdleTimeout))
                {
                    return candidatePeriod;
                }
            }

            return null;
        }

        private ActivityPeriod Merge(ActivityPeriod activityPeriod)
        {
            ActivityPeriod mergablePeriod = FindMergablePeriod(activityPeriod, GetMergeCandidates(activityPeriod));
            if (mergablePeriod != null)
            {
                var merged = activityPeriod.Merge(mergablePeriod);
                log.Debug("merged " + merged);
                periodStorage.ExecuteInTransaction(() =>
                {
                    periodStorage.RemoveFromTimePeriod(merged);
                    periodStorage.Add(merged);
                });
                return merged;
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

        public void RegisterUserActivityListener(UserActivityListner listner)
        {
            userActivityListner = listner;
        }
    }
}