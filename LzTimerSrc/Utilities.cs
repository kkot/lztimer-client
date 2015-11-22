using System;
using System.Collections.Generic;
using System.Linq;

namespace kkot.LzTimer
{
    public static class ListExensionMethods
    {
        public static T BeforeLast<T>(this List<T> list)
        {
            return list[list.Count-2];
        }

        public static T Last<T>(this List<T> list, int position)
        {
            return list[list.Count - position];
        }
    }

    public static class TimeSpanExensionMethods
    {
        public static TimeSpan Round(this TimeSpan toRound, TimeSpan round)
        {
            long wholeParts = toRound.Ticks / round.Ticks;
            long remain = toRound.Ticks % round.Ticks;
            if (remain < round.Ticks / 2)
                remain = 0;
            else
                remain = round.Ticks;

            TimeSpan rounded = new TimeSpan(wholeParts * round.Ticks + remain);
            return rounded;
        }
    }

    public static class IntegerTimespanExensionMethods
    {
        public static TimeSpan secs(this int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        public static TimeSpan ms(this int miliseconds)
        {
            return TimeSpan.FromMilliseconds(miliseconds);
        }

        public static TimeSpan mins(this int minutes)
        {
            return TimeSpan.FromMinutes(minutes);
        }

        public static TimeSpan[] s(this int[] seconds)
        {
            return seconds.Select(p => TimeSpan.FromSeconds(p)).ToArray();
        }

        public static TimeSpan hours(this int hours)
        {
            return TimeSpan.FromHours(hours);
        }
    }

    static class Utilities
    {
        public static string NowShortDateString()
        {
            DateTime nowDate = DateTime.Now;

            // przed 5 rano To jeszcze wczoraj
            if (nowDate.Hour < 5)
            {
                TimeSpan ds = new TimeSpan(24, 0, 0);
                nowDate = nowDate.Subtract(ds);
            }

            string date = nowDate.ToShortDateString();
            return date;
        }

        public static string SecondsToHMS(int allSeconds)
        {
            int hours = allSeconds / (60 * 60);
            int minutes = (allSeconds - (hours * 60 * 60)) / 60;
            int secunds = allSeconds - (hours * 60 * 60) - (minutes * 60);

            string result = string.Format("{0:00} h {1:00} m {2:00} s", new object[] { hours, minutes, secunds });
            return result;
        }
    }
}
