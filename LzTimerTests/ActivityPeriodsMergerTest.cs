﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace LzTimerTests
{
    public class MergingTests
    {
        private static readonly DateTime START_DATETIME = new DateTime(2014, 1, 1, 12, 0, 0, 0);

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
                var start = START_DATETIME;
                var end = start.AddSeconds(1);
                var period = new ActivePeriod(start, end);

                subjectUnderTest.ActivityPeriod(period);

                Mock.Assert(() => eventListnerMock.ActivityPeriod(period));
            }

            [TestMethod]
            public void twoCloseActivityPeriodShouldBeMerged()
            {
                var start1 = START_DATETIME;
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
                var start = START_DATETIME;
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
                var start1 = START_DATETIME;
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
                var start1 = START_DATETIME;
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
                CollectionAssert.AreEquivalent(mergingPeriodListSut.List, new [] { period2, period1 });
            }
        }

        [TestClass]
        public class TimeTableTest
        {
            private TimeTable timeTableSut;
            private const int IDLE_TIMEOUT = 5;

            [TestInitializeAttribute]
            public void setUp()
            {
                timeTableSut = new TimeTable(IDLE_TIMEOUT);
            }

            [TestMethod]
            public void singlePeriodShouldBeStoredAndReterned()
            {
                var start = START_DATETIME;
                var period = ActivePeriod(start, 1);

                var merged = timeTableSut.Add(period);

                Assert.AreEqual(period, merged);
                Assert.AreEqual(1, timeTableSut.List.Count);
                CollectionAssert.Contains(timeTableSut.List, merged);
            }

            [TestMethod]
            public void twoCloseActivePeriodShouldBeMerged()
            {
                var start1 = START_DATETIME;
                var period1 = ActivePeriod(start1, 1);

                var start2 = period1.End.AddSeconds(IDLE_TIMEOUT-1);
                var period2 = ActivePeriod(start2, 1);

                timeTableSut.Add(period1);
                timeTableSut.Add(period2);

                var periodMerged = new ActivePeriod(period1.Start, period2.End);

                Assert.AreEqual(periodMerged, timeTableSut.List[0]);
                CollectionAssert.AreEquivalent(new [] { periodMerged }, timeTableSut.List);
            }

            [TestMethod]
            public void twoDistantActivePeriodShouldNotBeMerged()
            {
                var start1 = START_DATETIME;
                var period1 = ActivePeriod(start1, 1);

                var start2 = period1.End.AddSeconds(IDLE_TIMEOUT+1);
                var period2 = ActivePeriod(start2, 2);

                var returned1 = timeTableSut.Add(period1);
                var returned2 = timeTableSut.Add(period2);

                Assert.AreEqual(period1, returned1);
                Assert.AreEqual(period2, returned2);
                CollectionAssert.AreEquivalent(timeTableSut.List, new[] { period2, period1 });
            }

            [TestMethod]
            public void twoCloseActiveAndIdlePeriodShouldNotBeMerged()
            {
                var start1 = START_DATETIME;
                var period1 = ActivePeriod(start1, 1);

                var start2 = period1.End.AddSeconds(IDLE_TIMEOUT - 1);
                var period2 = IdlePeriod(start2, 2);

                timeTableSut.Add(period1);
                timeTableSut.Add(period2);

                CollectionAssert.AreEquivalent(timeTableSut.List,  new Period[] { period2, period1 });
            }

            public static ActivePeriod ActivePeriod(DateTime start, int intervalSeconds)
            {
                return new ActivePeriod(start, start.AddSeconds(intervalSeconds));
            }

            public static IdlePeriod IdlePeriod(DateTime start, int intervalSeconds)
            {
                return new IdlePeriod(start, start.AddSeconds(intervalSeconds));
            }
        }
    }
}
