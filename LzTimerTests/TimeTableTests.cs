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
        private static readonly Period WHOLE_DAY = new Period(MIDDAY.AddHours(-12), MIDDAY.AddHours(12));

        [TestClass]
        public class TimeTableTest
        {
            protected TimeTable timeTableSUT;
            protected TestablePeriodStorage periodStorage;
            protected readonly TimeSpan IDLE_TIMEOUT_SECS = 5.s();

            [TestInitializeAttribute]
            public virtual void setUp()
            {
                TimeTablePolicies policies = new TimeTablePolicies()
                {
                    IdleTimeout = IDLE_TIMEOUT_SECS,
                };
                periodStorage = new MemoryPeriodStorage();
                timeTableSUT = new TimeTable(policies, periodStorage);
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
                    Assert.AreEqual(1, periodStorage.GetAll().Count);
                    CollectionAssert.Contains(periodStorage.GetAll(), merged);
                }

                [TestMethod]
                public void twoCloseActivePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.s()).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var periodMerged = new ActivePeriod(period1.Start, period2.End);

                    Assert.AreEqual(periodMerged, periodStorage.GetAll().First());
                    CollectionAssert.AreEquivalent(new[] { periodMerged }, periodStorage.GetAll());
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

                    CollectionAssert.AreEquivalent(new ActivityPeriod[] { period1, periodMerged }, periodStorage.GetAll());
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
                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new[] { period2, period1 });
                }

                [TestMethod]
                public void closeActiveAndIdlePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.s()).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new ActivityPeriod[] { period2, period1 });
                }

                [TestMethod]
                public void twoCloseActivePeriodEnclosingIdlePeriodShouldBeMerged()
                {
                    var period1a = PeriodBuilder.New(MIDDAY).Active();
                    var period2i = PeriodBuilder.NewAfter(period1a).Idle();
                    var period3a = PeriodBuilder.NewAfter(period2i).Active();

                    timeTableSUT.Add(period1a);
                    timeTableSUT.Add(period2i);
                    timeTableSUT.Add(period3a);

                    var mergedPeriod = new ActivePeriod(period1a.Start, period3a.End);

                    CollectionAssert.DoesNotContain(periodStorage.GetAll(), period2i);
                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new[] { mergedPeriod });
                }

                [TestMethod]
                public void twoCloseIdlePeriodEnclosingActivePeriodShouldNotBeMerged()
                {
                    var period1i = PeriodBuilder.New(MIDDAY).Idle();
                    var period2a = PeriodBuilder.NewAfter(period1i).Active();
                    var period3a = PeriodBuilder.NewAfter(period2a).Idle();

                    timeTableSUT.Add(period1i);
                    timeTableSUT.Add(period2a);
                    timeTableSUT.Add(period3a);

                    var mergedPeriod = new ActivePeriod(period1i.Start, period3a.End);

                    CollectionAssert.AreEquivalent(periodStorage.GetAll(),
                        new ActivityPeriod[] {period1i, period2a, period3a});
                }
            }
        }
    }
}
