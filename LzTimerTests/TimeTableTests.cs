using System;
using System.Linq;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            protected readonly TimeSpan IDLE_TIMEOUT_SECS = 5.secs();
            protected readonly TimeSpan SHORT_IDLE_SECS = 3.secs();

            [TestInitializeAttribute]
            public virtual void setUp()
            {
                TimeTablePolicies policies = new TimeTablePolicies()
                {
                    IdleTimeout = IDLE_TIMEOUT_SECS,
                    IdleTimeoutPenalty = SHORT_IDLE_SECS
                };
                timeTableSUT = new TimeTable(policies);
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
                    Assert.AreEqual(1, timeTableSUT.getAll().Count);
                    CollectionAssert.Contains(timeTableSUT.getAll(), merged);
                }

                [TestMethod]
                public void twoCloseActivePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.secs()).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var periodMerged = new ActivePeriod(period1.Start, period2.End);

                    Assert.AreEqual(periodMerged, timeTableSUT.getAll().First());
                    CollectionAssert.AreEquivalent(new[] {periodMerged}, timeTableSUT.getAll());
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

                    CollectionAssert.AreEquivalent(new Period[] {period1, periodMerged}, timeTableSUT.getAll());
                }

                [TestMethod]
                public void twoDistantActivePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS + 1.secs()).Active();

                    var returned1 = timeTableSUT.Add(period1);
                    var returned2 = timeTableSUT.Add(period2);

                    Assert.AreEqual(period1, returned1);
                    Assert.AreEqual(period2, returned2);
                    CollectionAssert.AreEquivalent(timeTableSUT.getAll(), new[] {period2, period1});
                }

                [TestMethod]
                public void twoCloseActiveAndIdlePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.secs()).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    CollectionAssert.AreEquivalent(timeTableSUT.getAll(), new Period[] {period2, period1});
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

                    CollectionAssert.DoesNotContain(timeTableSUT.getAll(), period2);
                    CollectionAssert.AreEquivalent(timeTableSUT.getAll(), new[] {mergedPeriod});
                }

                [TestMethod]
                public void twoCloseIdlePeriodEnclosingActivePeriodShouldNotBeMerged()
                {
                    var period1Active = PeriodBuilder.New(MIDDAY).Idle();
                    var period2Idle = PeriodBuilder.NewAfter(period1Active).Active();
                    var period3Active = PeriodBuilder.NewAfter(period2Idle).Idle();

                    timeTableSUT.Add(period1Active);
                    timeTableSUT.Add(period2Idle);
                    timeTableSUT.Add(period3Active);

                    var mergedPeriod = new ActivePeriod(period1Active.Start, period3Active.End);

                    CollectionAssert.AreEquivalent(timeTableSUT.getAll(),
                        new Period[] {period1Active, period2Idle, period3Active});
                }

                [TestClass]
                public class LastActivePeriodTests : TimeTableTest
                {
                    //[TestMethod]
                    public void whenLastPeriodActive_shouldBeEqualLengthOfLastPeriod()
                    {
                        var period1 = PeriodBuilder.New(MIDDAY).Length(secs(10)).Active();
                        var period2 = PeriodBuilder.NewAfter(period1).Length(secs(10)).Active();

                        timeTableSUT.Add(period1);
                        timeTableSUT.Add(period2);

                        var merged = new ActivePeriod(period1.Start, period2.End);
                        //Assert.AreEqual(merged, timeTableSUT.GetCurrentPeriod());
                    }

                    //[TestMethod]
                    public void whenLastIdlePeriodIsShort_shouldBeEqualLengthOfLastActivePlusIdlePeriod()
                    {
                        var period1 = PeriodBuilder.New(MIDDAY).Length(secs(10)).Active();
                        var period2 = PeriodBuilder.NewAfter(period1).Length(SHORT_IDLE_SECS - 1.secs()).Idle();

                        timeTableSUT.Add(period1);
                        timeTableSUT.Add(period2);

                        var merged = new ActivePeriod(period1.Start, period2.End);
                        //Assert.AreEqual(merged, timeTableSUT.GetCurrentPeriod());
                    }

                    //[TestMethod]
                    public void whenLastIdlePeriodIsLong_shouldBeEqualLengthOfLastIdle()
                    {
                        var period1Active = PeriodBuilder.New(MIDDAY).Length(secs(10)).Active();
                        var period2Idle =
                            PeriodBuilder.NewAfter(period1Active).Length(IDLE_TIMEOUT_SECS + 1.secs()).Idle();

                        timeTableSUT.Add(period1Active);
                        timeTableSUT.Add(period2Idle);

                        //Assert.AreEqual(period2Idle, timeTableSUT.GetCurrentPeriod());
                    }

                    //[TestMethod]
                    public void lastIdlePeriod_shouldBeNull()
                    {
                        //Assert.AreEqual(null, timeTableSUT.GetCurrentPeriod());
                        //Assert.AreEqual(null, timeTableSUT.GetLastIdlePeriod());
                    }

                    //[TestMethod]
                    public void whenOnlyActive_lastIdleShouldBeNull()
                    {
                        var period1Active = PeriodBuilder.New(MIDDAY).Length(secs(10)).Active();
                        timeTableSUT.Add(period1Active);

                        //Assert.AreEqual(period1Active, timeTableSUT.GetCurrentPeriod());
                        //Assert.AreEqual(null, timeTableSUT.GetLastIdlePeriod());
                    }
                }

                [TestClass]
                public class UserActivityReporterTests : TimeTableTest
                {
                    private StatsReporter statsReporterSut;
                    private DateTime PREVIOS_MIDDAY = MIDDAY.AddDays(-1);
                    private DateTime NEXT_MIDDAY = MIDDAY.AddDays(1);

                    [TestInitializeAttribute]
                    public override void setUp()
                    {
                        base.setUp();
                        //statsReporterSut = timeTableSUT;
                    }

                    [TestMethod]
                    public void whenOneActivePeriodIsInRage_timeSpanShouldBePeriodLength()
                    {
                        var period = PeriodBuilder.New(MIDDAY).Active();

                        timeTableSUT.Add(period);

                        //Assert.AreEqual(period.Length, statsReporterSut.GetTotalActiveTimespan(WHOLE_DAY));         
                    }

                    [TestMethod]
                    public void whenOneIdlePeriodIsInRage_timeSpanShouldBeZero()
                    {
                        var period = PeriodBuilder.New(MIDDAY).Idle();

                        timeTableSUT.Add(period);

                        //Assert.AreEqual(TimeSpan.Zero, statsReporterSut.GetTotalActiveTimespan(WHOLE_DAY));
                    }

                    [TestMethod]
                    public void whenPeriodsNotInRage_timeSpanShouldBeZeroLength()
                    {
                        var period1 = PeriodBuilder.New(NEXT_MIDDAY).Active();
                        var period2 = PeriodBuilder.New(PREVIOS_MIDDAY).Active();

                        timeTableSUT.Add(period1);
                        timeTableSUT.Add(period2);

                        //Assert.AreEqual(TimeSpan.Zero, statsReporterSut.GetTotalActiveTimespan(WHOLE_DAY));
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
                        //Assert.AreEqual(expected, statsReporterSut.GetTotalActiveTimespan(WHOLE_DAY));
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
}
