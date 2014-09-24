using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

    public class Period
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public Period(DateTime @start, DateTime end)
        {
            this.Start = start;
            this.End = end;
        }

        public bool CloseTo(Period aPeriod, int seconds)
        {
            if ((End - aPeriod.Start).Seconds <= seconds ||
                (Start - aPeriod.End).Seconds <= seconds)
                return true;
            return false;
        }

        public Period Merge(Period aPeriod)
        {
            var start = Start < aPeriod.Start ? Start : aPeriod.Start;
            var end   = End > aPeriod.End ? End : aPeriod.End;
            return new Period(start, end);
        }

        public override string ToString()
        {
            return "["+Start+" "+End+"]";
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var period = (Period) obj;
            return Start.Equals(period.Start) && End.Equals(period.End);     
        }

// override object.GetHashCode
        public override int GetHashCode()
        {
            return Start.GetHashCode()+End.GetHashCode();
        }
    }

    public interface ActivityPeriodsListener
    {
        void ActivityPeriod(Period period);

        void IdlePeriod(Period period);

        void SuspendPeriod(Period period);
    }

    public class PeriodList
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
                if (period.CloseTo(aPeriod, 1))
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

        IList<Period> getPeriods()
        {
            return new List<Period>(periods);
        } 
    }

    public class ActivityPeriodsMerger : ActivityPeriodsListener
    {
        private ActivityPeriodsListener listener;
        private readonly PeriodList periodList = new PeriodList();

        public ActivityPeriodsMerger()
        {
        }

        public void addActivityPeriodListener(ActivityPeriodsListener aListener)
        {
            this.listener = aListener;
        }

        public void ActivityPeriod(Period aPeriod)
        {
            var period = periodList.Add(aPeriod);
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
