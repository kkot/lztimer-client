using System;

namespace kkot.LzTimer
{
    public class Period : IComparable
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public TimeSpan Length { get; private set; }

        public Period(DateTime @start, DateTime end)
        {
            Start = start;
            End = end;
            Length = end - start;
        }

        public override string ToString()
        {
            return "[" + Start.TimeOfDay + " " + End.TimeOfDay + " length " + Length + "]";
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
            return Start.GetHashCode() + End.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            var period = (Period) obj;
            return Start.CompareTo(period.Start);
        }

        public bool IsDirectlyBefore(Period period)
        {
            return End == period.Start;
        }
    }

    public class ActivityPeriod : Period
    {
        protected ActivityPeriod(DateTime @start, DateTime end)
            : base(start, end)
        {
        }

        public static ActivityPeriod Create(bool active, DateTime @start, DateTime end)
        {
            if (active)
            {
                return new ActivePeriod(start, end);
            }
            return new IdlePeriod(start, end);
        }

        public bool CanBeMerged(ActivityPeriod aActivityPeriod, TimeSpan aTimeoutPeriod)
        {
            if (GetType() != aActivityPeriod.GetType())
            {
                return false;
            }

            // 500 ms is little hacky
            var mergeGap = (aActivityPeriod is ActivePeriod) ? aTimeoutPeriod : TimeSpan.FromMilliseconds(500);

            if (((End - aActivityPeriod.Start).Duration() <= mergeGap) ||
                ((Start - aActivityPeriod.End).Duration() <= mergeGap))
            {
                return true;
            }
            return false;
        }

        virtual public ActivityPeriod Merge(ActivityPeriod aActivityPeriod)
        {
            var start = Start < aActivityPeriod.Start ? Start : aActivityPeriod.Start;
            var end = End > aActivityPeriod.End ? End : aActivityPeriod.End;
            return new ActivityPeriod(start, end);
        }

        public bool Overlap(ActivityPeriod other)
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
            var period = (ActivityPeriod) obj;
            return Start.Equals(period.Start) && End.Equals(period.End);
        }

        public override string ToString()
        {
            return "[" + Start.TimeOfDay + " " + End.TimeOfDay + " length " + Length + " type " + GetType()+ "]";
        }
    }

    public class ActivePeriod : ActivityPeriod
    {
        public ActivePeriod(DateTime start, DateTime end) : base(start, end) { }

        public override ActivityPeriod Merge(ActivityPeriod activityPeriod)
        {
            ActivityPeriod merged = base.Merge(activityPeriod);
            return new ActivePeriod(merged.Start, merged.End);
        }
    }

    public class IdlePeriod : ActivityPeriod
    {
        public IdlePeriod(DateTime start, DateTime end)
            : base(start, end) { }

        public override ActivityPeriod Merge(ActivityPeriod activityPeriod)
        {
            ActivityPeriod merged = base.Merge(activityPeriod);
            return new IdlePeriod(merged.Start, merged.End);
        }
    }
}
