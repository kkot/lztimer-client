using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace LzTimerTests
{
    public class TimeTableTests
    {
        private static readonly DateTime MIDDAY = new DateTime(2014, 1, 1, 12, 0, 0, 0);
        private static readonly DateTime MIDNIGHT_BEFORE = new DateTime(2014, 1, 1, 0, 0, 0, 0);
        private static readonly TimePeriod WHOLE_DAY = new TimePeriod(MIDDAY.AddHours(-12), MIDDAY.AddHours(12));

        [TestClass]
        public class TimeTableTest
        {
            protected TimeTable timeTableSUT;
            protected const int IDLE_TIMEOUT_SECS = 5;
            protected const int SHORT_IDLE_SECS = 3;

            [TestInitializeAttribute]
            public virtual void setUp()
            {
                timeTableSUT = new TimeTable(IDLE_TIMEOUT_SECS, SHORT_IDLE_SECS);
            }

            [TestClass]
            public class MergingTests : TimeTableTest
            {
                [TestMethod]
                public void singlePeriodShouldBeStoredAndReterned()
                {
                    var period = PeriodBuilder.New(MIDDAY).Active();

                    var merged = timeTableSUT.Add(period);

                    Assert.AreEqual(period, merged);
                    Assert.AreEqual(1, timeTableSUT.List.Count);
                    CollectionAssert.Contains(timeTableSUT.List, merged);
                }

                [TestMethod]
                public void twoCloseActivePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, secs(IDLE_TIMEOUT_SECS - 1)).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var periodMerged = new ActivePeriod(period1.Start, period2.End);

                    Assert.AreEqual(periodMerged, timeTableSUT.List.First());
                    CollectionAssert.AreEquivalent(new[] { periodMerged }, timeTableSUT.List);
                }

                [TestMethod]
                public void twoCloseIdlePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, mili(10)).Idle();
                    var period3 = PeriodBuilder.NewAfter(period2, mili(10)).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);
                    timeTableSUT.Add(period3);

                    var periodMerged = new IdlePeriod(period2.Start, period3.End);

                    CollectionAssert.AreEquivalent(new Period[] { period1, periodMerged }, timeTableSUT.List);
                }

                [TestMethod]
                public void twoDistantActivePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, secs(IDLE_TIMEOUT_SECS + 1)).Active();

                    var returned1 = timeTableSUT.Add(period1);
                    var returned2 = timeTableSUT.Add(period2);

                    Assert.AreEqual(period1, returned1);
                    Assert.AreEqual(period2, returned2);
                    CollectionAssert.AreEquivalent(timeTableSUT.List, new[] { period2, period1 });
                }

                [TestMethod]
                public void twoCloseActiveAndIdlePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, secs(IDLE_TIMEOUT_SECS - 1)).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    CollectionAssert.AreEquivalent(timeTableSUT.List, new Period[] { period2, period1 });
                }

                [TestMethod]
                public void twoCloseActivePeriodEnclosingIdlePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1).Idle();
                    var period3 = PeriodBuilder.NewAfter(period2).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);
                    timeTableSUT.Add(period3);

                    var mergedPeriod = new ActivePeriod(period1.Start, period3.End);

                    CollectionAssert.DoesNotContain(timeTableSUT.List, period2);
                    CollectionAssert.AreEquivalent(timeTableSUT.List, new[] { mergedPeriod });
                }
            }

            [TestClass]
            public class LastActivePeriodTests : TimeTableTest
            {
                [TestMethod]
                public void whenLastPeriodActive_shouldBeEqualLengthOfLastPeriod()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Length(secs(10)).Active();
                    var period2 = PeriodBuilder.NewAfter(period1).Length(secs(10)).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var merged = new ActivePeriod(period1.Start, period2.End);
                    Assert.AreEqual(merged, timeTableSUT.GetCurrentPeriod());
                }

                [TestMethod]
                public void whenLastIdlePeriodIsShort_shouldBeEqualLengthOfLastActivePlusIdlePeriod()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Length(secs(10)).Active();
                    var period2 = PeriodBuilder.NewAfter(period1).Length(secs(SHORT_IDLE_SECS-1)).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var merged = new ActivePeriod(period1.Start, period2.End);
                    Assert.AreEqual(merged, timeTableSUT.GetCurrentPeriod());
                }

                [TestMethod]
                public void whenLastIdlePeriodIsLong_shouldBeEqualLengthOfLastIdle()
                {
                    var period1Active = PeriodBuilder.New(MIDDAY).Length(secs(10)).Active();
                    var period2Idle = PeriodBuilder.NewAfter(period1Active).Length(secs(SHORT_IDLE_SECS+1)).Idle();

                    timeTableSUT.Add(period1Active);
                    timeTableSUT.Add(period2Idle);

                    Assert.AreEqual(period2Idle, timeTableSUT.GetCurrentPeriod());
                }
            }

            [TestClass]
            public class UserActivityReporterTests : TimeTableTest
            {
                private ActivityStatsReporter activityStatsReporterSUT;
                private DateTime PREVIOS_MIDDAY = MIDDAY.AddDays(-1);
                private DateTime NEXT_MIDDAY = MIDDAY.AddDays(1);

                [TestInitializeAttribute]
                public override void setUp()
                {
                    base.setUp();
                    activityStatsReporterSUT = timeTableSUT;
                }

                [TestMethod]
                public void whenOneActivePeriodIsInRage_timeSpanShouldBePeriodLength()
                {
                    var period = PeriodBuilder.New(MIDDAY).Active();

                    timeTableSUT.Add(period);

                    Assert.AreEqual(period.Length, activityStatsReporterSUT.GetTotalActiveTimespan(WHOLE_DAY));         
                }

                [TestMethod]
                public void whenOneIdlePeriodIsInRage_timeSpanShouldBeZero()
                {
                    var period = PeriodBuilder.New(MIDDAY).Idle();

                    timeTableSUT.Add(period);

                    Assert.AreEqual(TimeSpan.Zero, activityStatsReporterSUT.GetTotalActiveTimespan(WHOLE_DAY));
                }

                [TestMethod]
                public void whenPeriodsNotInRage_timeSpanShouldBeZeroLength()
                {
                    var period1 = PeriodBuilder.New(NEXT_MIDDAY).Active();
                    var period2 = PeriodBuilder.New(PREVIOS_MIDDAY).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    Assert.AreEqual(TimeSpan.Zero, activityStatsReporterSUT.GetTotalActiveTimespan(WHOLE_DAY));
                }

                [TestMethod]
                public void onlyActivePeriodInRangeShouldBeCounted()
                {
                    var periodBeforeActive = PeriodBuilder.New(PREVIOS_MIDDAY).Length(8.secs()).Active();
                    var periodBeforeIdle = PeriodBuilder.NewAfter(periodBeforeActive).Length(9.secs()).Idle();

                    var periodAfterIdle = PeriodBuilder.New(NEXT_MIDDAY).Length(10.secs()).Idle();
                    var periodAfterActive = PeriodBuilder.NewAfter(periodBeforeActive).Length(11.secs()).Active();

                    var periodTodayIdle = PeriodBuilder.New(MIDDAY).Length(12.secs()).Idle();
                    var periodTodayActive = PeriodBuilder.NewAfter(periodTodayIdle).Length(13.secs()).Active();

                    timeTableSUT.Add(periodBeforeActive);
                    timeTableSUT.Add(periodBeforeIdle);
                    timeTableSUT.Add(periodAfterIdle);
                    timeTableSUT.Add(periodAfterActive);
                    timeTableSUT.Add(periodTodayIdle);
                    timeTableSUT.Add(periodTodayActive);

                    var expected = 13.secs();
                    Assert.AreEqual(expected, activityStatsReporterSUT.GetTotalActiveTimespan(WHOLE_DAY));
                }


            }
        }

        private static TimeSpan secs(int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        private static TimeSpan mili(int miliseconds)
        {
            return TimeSpan.FromMilliseconds(miliseconds);
        }
    }
}
