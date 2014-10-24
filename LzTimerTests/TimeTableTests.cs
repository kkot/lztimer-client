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
            protected readonly TimeSpan IDLE_TIMEOUT_SECS = 5.s();

            [TestInitializeAttribute]
            public virtual void setUp()
            {
                TimeTablePolicies policies = new TimeTablePolicies()
                {
                    IdleTimeout = IDLE_TIMEOUT_SECS,
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
                    Assert.AreEqual(1, timeTableSUT.GetAll().Count);
                    CollectionAssert.Contains(timeTableSUT.GetAll(), merged);
                }

                [TestMethod]
                public void twoCloseActivePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.s()).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var periodMerged = new ActivePeriod(period1.Start, period2.End);

                    Assert.AreEqual(periodMerged, timeTableSUT.GetAll().First());
                    CollectionAssert.AreEquivalent(new[] {periodMerged}, timeTableSUT.GetAll());
                }

                [TestMethod]
                public void twoCloseIdlePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, 10.ms()).Idle();
                    var period3 = PeriodBuilder.NewAfter(period2, 10.ms()).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);
                    timeTableSUT.Add(period3);

                    var periodMerged = new IdlePeriod(period2.Start, period3.End);

                    CollectionAssert.AreEquivalent(new Period[] {period1, periodMerged}, timeTableSUT.GetAll());
                }

                [TestMethod]
                public void twoDistantActivePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS + 1.s()).Active();

                    var returned1 = timeTableSUT.Add(period1);
                    var returned2 = timeTableSUT.Add(period2);

                    Assert.AreEqual(period1, returned1);
                    Assert.AreEqual(period2, returned2);
                    CollectionAssert.AreEquivalent(timeTableSUT.GetAll(), new[] {period2, period1});
                }

                [TestMethod]
                public void twoCloseActiveAndIdlePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.s()).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    CollectionAssert.AreEquivalent(timeTableSUT.GetAll(), new Period[] {period2, period1});
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

                    CollectionAssert.DoesNotContain(timeTableSUT.GetAll(), period2);
                    CollectionAssert.AreEquivalent(timeTableSUT.GetAll(), new[] {mergedPeriod});
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

                    CollectionAssert.AreEquivalent(timeTableSUT.GetAll(),
                        new Period[] {period1Active, period2Idle, period3Active});
                }
            }
        }
    }
}
