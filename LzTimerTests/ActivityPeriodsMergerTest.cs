using System;
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
                var period = new Period(start, end);

                subjectUnderTest.ActivityPeriod(period);

                Mock.Assert(() => eventListnerMock.ActivityPeriod(period));
            }

            [TestMethod]
            public void twoCloseActivityPeriodShouldBeMerged()
            {
                var start1 = START_DATETIME;
                var end1 = start1.AddSeconds(1);
                var period1 = new Period(start1, end1);

                var start2 = end1;
                var end2 = start2.AddSeconds(1);
                var period2 = new Period(start2, end2);

                subjectUnderTest.ActivityPeriod(period1);
                subjectUnderTest.ActivityPeriod(period2);

                var mergedPeriod = new Period(period1.Start, period2.End);
                Mock.Assert(() => eventListnerMock.ActivityPeriod(mergedPeriod));
            }
        }

        [TestClass]
        public class PeriodListTest
        {
            private PeriodList subjectUnderTest;

            [TestInitializeAttribute]
            public void setUp()
            {
                subjectUnderTest = new PeriodList();
            }

            [TestMethod]
            public void singlePeriodShouldBeStoredAndReterned()
            {
                var start = START_DATETIME;
                var end = start.AddSeconds(1);
                var period = new Period(start, end);

                var merged = subjectUnderTest.Add(period);

                Assert.AreEqual(period, merged);
            }

            [TestMethod]
            public void twoClosePeriodShouldBeMerged()
            {
                var start1 = START_DATETIME;
                var end1 = start1.AddSeconds(1);
                var period1 = new Period(start1, end1);

                var start2 = period1.End;
                var end2 = start2.AddSeconds(1);
                var period2 = new Period(start2, end2);

                var periodMerged = new Period(period1.Start, period2.End);

                subjectUnderTest.Add(period1);
                var merged = subjectUnderTest.Add(period2);

                Assert.AreEqual(merged, periodMerged);
            }
        }
    }
}
