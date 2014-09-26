using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace LzTimerTests
{
    public class PeriodBuilder
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public static PeriodBuilder New(DateTime start)
        {
            var periodBuilder = new PeriodBuilder(start, start.AddSeconds(1));
            return periodBuilder;
        }

        private PeriodBuilder(Period period)
        {
            this.Start = period.Start;
            this.End = period.End;
        }

        public PeriodBuilder Length(int seconds)
        {
            End = Start.AddSeconds(seconds);
            return this;
        }

        public PeriodBuilder afterMs(int miliseconds)
        {
            var periodBuilder = New(this.End.AddMilliseconds(miliseconds));
            return periodBuilder;
        }

        public static PeriodBuilder NewAfter(Period period, int miliseconds)
        {
            return new PeriodBuilder(period).afterMs(miliseconds);
        }

        public static PeriodBuilder NewAfter(ActivePeriod period1)
        {
            return NewAfter(period1, 0);
        }

        public IdlePeriod Idle()
        {
            return new IdlePeriod(Start, End);
        }

        private PeriodBuilder(DateTime start, DateTime end)
        {
            this.Start = start;
            this.End = end;
        }

        public ActivePeriod Active()
        {
            return new ActivePeriod(Start, End);
        }
    }

    public class MergingTests
    {
        private static readonly DateTime START = new DateTime(2014, 1, 1, 12, 0, 0, 0);

        [TestClass]
        public class ActivityPeriodMergerTest
        {
            private ActivityPeriodsMerger subjectUnderTest;
            private ActivityPeriodsListener eventListnerMock;

            [TestInitializeAttribute]
            public void setUp()
            {
                eventListnerMock = Mock.Create<ActivityPeriodsListener>();
                subjectUnderTest = new ActivityPeriodsMerger();
                subjectUnderTest.addActivityPeriodListener(eventListnerMock);
            }

            [TestMethod]
            public void singleActivityPeriodShouldBePassed()
            {
                var start = START;
                var end = start.AddSeconds(1);
                var period = new ActivePeriod(start, end);

                subjectUnderTest.ActivityPeriod(period);

                Mock.Assert(() => eventListnerMock.ActivityPeriod(period));
            }

            [TestMethod]
            public void twoCloseActivityPeriodShouldBeMerged()
            {
                var start1 = START;
                var end1 = start1.AddSeconds(1);
                var period1 = new ActivePeriod(start1, end1);

                var start2 = end1;
                var end2 = start2.AddSeconds(1);
                var period2 = new ActivePeriod(start2, end2);

                subjectUnderTest.ActivityPeriod(period1);
                subjectUnderTest.ActivityPeriod(period2);

                var mergedPeriod = new ActivePeriod(period1.Start, period2.End);
                Mock.Assert(() => eventListnerMock.ActivityPeriod(mergedPeriod));
            }
        }

        [TestClass]
        public class MergingPeriodListTest
        {
            private MergingPeriodList mergingPeriodListSut;

            [TestInitializeAttribute]
            public void setUp()
            {
                mergingPeriodListSut = new MergingPeriodList();
            }

            [TestMethod]
            public void singlePeriodShouldBeStoredAndReterned()
            {
                var start = START;
                var end = start.AddSeconds(1);
                var period = new ActivePeriod(start, end);

                var merged = mergingPeriodListSut.Add(period);

                Assert.AreEqual(period, merged);
                Assert.AreEqual(1, mergingPeriodListSut.List.Count);
                CollectionAssert.Contains(mergingPeriodListSut.List, merged);
            }

            [TestMethod]
            public void twoClosePeriodShouldBeMerged()
            {
                var start1 = START;
                var end1 = start1.AddSeconds(1);
                var period1 = new ActivePeriod(start1, end1);

                var start2 = period1.End.AddMilliseconds(100);
                var end2 = start2.AddSeconds(1);
                var period2 = new ActivePeriod(start2, end2);

                var periodMerged = new ActivePeriod(period1.Start, period2.End);

                mergingPeriodListSut.Add(period1);
                var merged = mergingPeriodListSut.Add(period2);

                Assert.AreEqual(merged, periodMerged);
                Assert.AreEqual(1, mergingPeriodListSut.List.Count);
                CollectionAssert.Contains(mergingPeriodListSut.List, merged);
            }

            [TestMethod]
            public void twoDistantPeriodShouldNotBeMerged()
            {
                var start1 = START;
                var end1 = start1.AddSeconds(1);
                var period1 = new ActivePeriod(start1, end1);

                var start2 = period1.End.AddMinutes(10);
                var end2 = start2.AddSeconds(2);
                var period2 = new ActivePeriod(start2, end2);

                var returned1 = mergingPeriodListSut.Add(period1);
                var returned2 = mergingPeriodListSut.Add(period2);

                Assert.AreEqual(period1, returned1);
                Assert.AreEqual(period2, returned2);
                Assert.AreEqual(2, mergingPeriodListSut.List.Count);
                CollectionAssert.AreEquivalent(mergingPeriodListSut.List, new[] { period2, period1 });
            }
        }

        [TestClass]
        public class TimeTableTest
        {
            protected TimeTable timeTableSUT;
            protected const int IDLE_TIMEOUT = 5;
            protected const int SHORT_IDLE = 3;

            public static ActivePeriod ActivePeriod(DateTime start, int intervalSeconds)
            {
                return new ActivePeriod(start, start.AddSeconds(intervalSeconds));
            }

            public static IdlePeriod IdlePeriod(DateTime start, int intervalSeconds)
            {
                return new IdlePeriod(start, start.AddSeconds(intervalSeconds));
            }

            [TestInitializeAttribute]
            public void setUp()
            {
                timeTableSUT = new TimeTable(IDLE_TIMEOUT);
            }

            [TestClass]
            public class MergingTests : TimeTableTest
            {
                [TestMethod]
                public void singlePeriodShouldBeStoredAndReterned()
                {
                    var period = ActivePeriod(START, 1);

                    var merged = timeTableSUT.Add(period);

                    Assert.AreEqual(period, merged);
                    Assert.AreEqual(1, timeTableSUT.List.Count);
                    CollectionAssert.Contains(timeTableSUT.List, merged);
                }

                [TestMethod]
                public void twoCloseActivePeriodShouldBeMerged()
                {
                    var period1 = ActivePeriod(START, 1);

                    var start2 = period1.End.AddSeconds(IDLE_TIMEOUT - 1);
                    var period2 = ActivePeriod(start2, 1);

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var periodMerged = new ActivePeriod(period1.Start, period2.End);

                    Assert.AreEqual(periodMerged, timeTableSUT.List.First());
                    CollectionAssert.AreEquivalent(new[] { periodMerged }, timeTableSUT.List);
                }

                [TestMethod]
                public void twoCloseIdlePeriodShouldBeMerged()
                {
                    var periodBuilder1 = PeriodBuilder.New(START);
                    var periodBuilder2 = periodBuilder1.afterMs(10);
                    var periodBuilder3 = periodBuilder2.afterMs(10);

                    var period1 = periodBuilder1.Active();
                    var period2 = periodBuilder2.Idle();
                    var period3 = periodBuilder3.Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);
                    timeTableSUT.Add(period3);

                    var periodMerged = new IdlePeriod(period2.Start, period3.End);

                    CollectionAssert.AreEquivalent(new Period[] { period1, periodMerged }, timeTableSUT.List);
                }

                [TestMethod]
                public void twoDistantActivePeriodShouldNotBeMerged()
                {
                    var period1 = ActivePeriod(START, 1);

                    var start2 = period1.End.AddSeconds(IDLE_TIMEOUT + 1);
                    var period2 = ActivePeriod(start2, 2);

                    var returned1 = timeTableSUT.Add(period1);
                    var returned2 = timeTableSUT.Add(period2);

                    Assert.AreEqual(period1, returned1);
                    Assert.AreEqual(period2, returned2);
                    CollectionAssert.AreEquivalent(timeTableSUT.List, new[] { period2, period1 });
                }

                [TestMethod]
                public void twoCloseActiveAndIdlePeriodShouldNotBeMerged()
                {
                    var period1 = ActivePeriod(START, 1);

                    var start2 = period1.End.AddSeconds(IDLE_TIMEOUT - 1);
                    var period2 = IdlePeriod(start2, 2);

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    CollectionAssert.AreEquivalent(timeTableSUT.List, new Period[] { period2, period1 });
                }

                [TestMethod]
                public void twoCloseActivePeriodEnclosingIdlePeriodShouldBeMerged()
                {
                    var builder1 = PeriodBuilder.New(START);
                    var builder2 = builder1.afterMs(0);
                    var builder3 = builder2.afterMs(0);

                    var peroid1 = builder1.Active();
                    var period2 = builder2.Idle();
                    var period3 = builder3.Active();

                    timeTableSUT.Add(peroid1);
                    timeTableSUT.Add(period2);
                    timeTableSUT.Add(period3);

                    var mergedPeriod = new ActivePeriod(peroid1.Start, period3.End);

                    CollectionAssert.DoesNotContain(timeTableSUT.List, period2);
                    CollectionAssert.AreEquivalent(timeTableSUT.List, new[] { mergedPeriod });
                }
            }

            [TestClass]
            public class LastActiveLengthTests : TimeTableTest
            {
                [TestMethod]
                public void whenLastPeriodActive_shouldBeEqualLengthOfLastPeriod()
                {
                    var period1 = PeriodBuilder.New(START).Length(10).Active();
                    var period2 = PeriodBuilder.NewAfter(period1).Length(10).Active();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var merged = new ActivePeriod(period1.Start, period2.End);
                    Assert.AreEqual(merged, timeTableSUT.CurrentPeriod);
                }

                [TestMethod]
                public void whenLastIdlePeriodIsShort_shouldBeEqualLengthOfLastActiveAndIdlePeriod()
                {
                    var period1 = PeriodBuilder.New(START).Length(10).Active();
                    var period2 = PeriodBuilder.NewAfter(period1).Length(SHORT_IDLE-1).Idle();

                    timeTableSUT.Add(period1);
                    timeTableSUT.Add(period2);

                    var merged = new ActivePeriod(period1.Start, period2.End);
                    Assert.AreEqual(merged, timeTableSUT.CurrentPeriod);
                }
            }
        }
    }
}
