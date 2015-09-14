using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kkot.LzTimer
{
    public class TimePeriod : IComparable
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public TimeSpan Length { get; private set; }

        public TimePeriod(DateTime @start, DateTime end)
        {
            this.Start = start;
            this.End = end;
            this.Length = end - start;
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
            var period = (Period)obj;
            return Start.Equals(period.Start) && End.Equals(period.End);
        }

        public override int GetHashCode()
        {
            return Start.GetHashCode() + End.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            Period period = (Period)obj;
            return Start.CompareTo(period.Start);
        }
    }

    public class Period : TimePeriod
    {
        protected Period(DateTime @start, DateTime end)
            : base(start, end)
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
        public ActivePeriod(DateTime start, DateTime end) : base(start, end) { }

        public override Period Merge(Period period)
        {
            Period merged = base.Merge(period);
            return new ActivePeriod(merged.Start, merged.End);
        }
    }

    public class IdlePeriod : Period
    {
        public IdlePeriod(DateTime start, DateTime end)
            : base(start, end) { }

        public override Period Merge(Period period)
        {
            Period merged = base.Merge(period);
            return new IdlePeriod(merged.Start, merged.End);
        }
    }
}
