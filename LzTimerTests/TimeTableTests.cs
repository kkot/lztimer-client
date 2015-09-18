using System;
using System.IO;
using System.Linq;
using FakeItEasy;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LzTimerTests
{
    public class TimeTableTests
    {
        private static readonly DateTime MIDDAY = new DateTime(2014, 1, 1, 12, 0, 0, 0);
        private static readonly DateTime MIDNIGHT_BEFORE = new DateTime(2014, 1, 1, 0, 0, 0, 0);
        private static readonly Period WHOLE_DAY = new Period(MIDDAY.AddHours(-12), MIDDAY.AddHours(12));

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

                    var merged = timeTableSUT.AddPeriod(period);

                    Assert.AreEqual(period, merged);
                    Assert.AreEqual(1, periodStorage.GetAll().Count);
                    CollectionAssert.Contains(periodStorage.GetAll(), merged);
                }

                [TestMethod]
                public void twoCloseActivePeriodShouldBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.s()).Active();

                    timeTableSUT.AddPeriod(period1);
                    timeTableSUT.AddPeriod(period2);

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

                    timeTableSUT.AddPeriod(period1);
                    timeTableSUT.AddPeriod(period2);
                    timeTableSUT.AddPeriod(period3);

                    var periodMerged = new IdlePeriod(period2.Start, period3.End);

                    CollectionAssert.AreEquivalent(new ActivityPeriod[] { period1, periodMerged }, periodStorage.GetAll());
                }

                [TestMethod]
                public void twoDistantActivePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS + 1.s()).Active();

                    var returned1 = timeTableSUT.AddPeriod(period1);
                    var returned2 = timeTableSUT.AddPeriod(period2);

                    Assert.AreEqual(period1, returned1);
                    Assert.AreEqual(period2, returned2);
                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new[] { period2, period1 });
                }

                [TestMethod]
                public void closeActiveAndIdlePeriodShouldNotBeMerged()
                {
                    var period1 = PeriodBuilder.New(MIDDAY).Active();
                    var period2 = PeriodBuilder.NewAfter(period1, IDLE_TIMEOUT_SECS - 1.s()).Idle();

                    timeTableSUT.AddPeriod(period1);
                    timeTableSUT.AddPeriod(period2);

                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new ActivityPeriod[] { period2, period1 });
                }

                [TestMethod]
                public void twoCloseActivePeriodEnclosingIdlePeriodShouldBeMerged()
                {
                    var period1a = PeriodBuilder.New(MIDDAY).Active();
                    var period2i = PeriodBuilder.NewAfter(period1a).Idle();
                    var period3a = PeriodBuilder.NewAfter(period2i).Active();

                    timeTableSUT.AddPeriod(period1a);
                    timeTableSUT.AddPeriod(period2i);
                    timeTableSUT.AddPeriod(period3a);

                    var mergedPeriod = new ActivePeriod(period1a.Start, period3a.End);

                    CollectionAssert.DoesNotContain(periodStorage.GetAll(), period2i);
                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new[] { mergedPeriod });
                }

                [TestMethod]
                public void twoCloseIdlePeriodEnclosingActivePeriodShouldNotBeMerged()
                {
                    var period1i = PeriodBuilder.New(MIDDAY).Idle();
                    var period2a = PeriodBuilder.NewAfter(period1i).Active();
                    var peroid3i = PeriodBuilder.NewAfter(period2a).Idle();

                    timeTableSUT.AddPeriod(period1i);
                    timeTableSUT.AddPeriod(period2a);
                    timeTableSUT.AddPeriod(peroid3i);

                    CollectionAssert.AreEquivalent(periodStorage.GetAll(),
                        new ActivityPeriod[] {period1i, period2a, peroid3i});
                }
            }

            [TestClass]
            public class UserActivityNofierTests : TimeTableTest
            {
                private UserActivityListner listenerActivityListnerMock;

                [TestInitialize]
                public override void setUp()
                {
                    base.setUp();
                    listenerActivityListnerMock = A.Fake<UserActivityListner>();
                    timeTableSUT.registerUserActivityListener(listenerActivityListnerMock);
                }

                [TestMethod]
                public void shouldNotifyWhenBeforeActiveThereIsLongIdlePeriod()
                {
                    // arrange
                    var periodIdleLong = PeriodBuilder.New(MIDDAY).Length(IDLE_TIMEOUT_SECS.longerThan()).Idle();
                    var periodActive = PeriodBuilder.NewAfter(periodIdleLong).Length(1.s()).Active();

                    // act
                    timeTableSUT.PeriodPassed(periodIdleLong);
                    timeTableSUT.PeriodPassed(periodActive);

                    // assert
                    A.CallTo(() => listenerActivityListnerMock.NotifyActiveAfterBreak(A<TimeSpan>.Ignored))
                        .MustHaveHappened(Repeated.Exactly.Once);
                }

                [TestMethod]
                public void shouldNotNotifyIfWasntIdleBefore()
                {
                    // arrange
                    ActivePeriod period = PeriodBuilder.New(MIDDAY).Length(1.s()).Active();

                    // act
                    timeTableSUT.PeriodPassed(period);

                    // assert
                    A.CallTo(() => listenerActivityListnerMock.NotifyActiveAfterBreak(A<TimeSpan>.Ignored)).MustNotHaveHappened();
                }

                [TestMethod]
                public void shouldNotNotifyIfWasShortIdleBefore()
                {
                    // arrange
                    var periodIdleShort = PeriodBuilder.New(MIDDAY).Length(IDLE_TIMEOUT_SECS.shorterThan()).Idle();
                    var periodActive = periodIdleShort.NewAfter().Length(1.s()).Active();

                    // act
                    timeTableSUT.PeriodPassed(periodIdleShort);
                    timeTableSUT.PeriodPassed(periodActive);

                    // assert
                    A.CallTo(() => listenerActivityListnerMock.NotifyActiveAfterBreak(A<TimeSpan>.Ignored)).MustNotHaveHappened();
                }

                [TestMethod]
                public void shouldNotNotifyIfBeforeIsIdleAndOff()
                {
                    // arrange
                    var periodIdle = PeriodBuilder.New(MIDDAY).Length(IDLE_TIMEOUT_SECS.longerThan()).Idle();
                    var offTime = 1.s();
                    var periodActive = periodIdle.NewAfter(offTime).Active();

                    // act
                    timeTableSUT.PeriodPassed(periodIdle);
                    timeTableSUT.PeriodPassed(periodActive);

                    // assert
                    A.CallTo(() => listenerActivityListnerMock.NotifyActiveAfterBreak(A<TimeSpan>.Ignored)).MustNotHaveHappened();
                }
            }
        }
    }
}
