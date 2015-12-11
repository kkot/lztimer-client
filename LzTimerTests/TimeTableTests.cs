using System;
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

            protected readonly TimeSpan LESS_THAN_IDLE_TIMEOUT = 9.secs();
            protected readonly TimeSpan IDLE_TIMEOUT = 10.secs();
            protected readonly TimeSpan MORE_THAN_IDLE_TIMEOUT = 11.secs();

            protected readonly TimeSpan BIGGER_LENGTH = 2.secs();

            [TestInitializeAttribute]
            public virtual void setUp()
            {
                TimeTablePolicies policies = new TimeTablePolicies()
                {
                    IdleTimeout = IDLE_TIMEOUT,
                };
                periodStorage = new MemoryPeriodStorage();
                timeTableSUT = new TimeTable(policies, periodStorage);
            }

            [TestClass]
            public class MergingTests : TimeTableTest
            {
                [TestMethod]
                private void singlePeriodShouldBeStoredAndReterned()
                {
                    var period = MIDDAY.NewPeriod().Active();
                    var merged = timeTableSUT.AddPeriod(period);
                    Assert.AreEqual(period, merged);
                    CollectionAssert.AreEquivalent(new[] { merged }, periodStorage.GetAll());
                }

                [TestMethod]
                public void twoCloseActivePeriodShouldBeMerged()
                {
                    var period1 = MIDDAY.NewPeriod().Active();
                    var period2 = period1.NewPeriodAfter(LESS_THAN_IDLE_TIMEOUT).Active();

                    timeTableSUT.AddPeriod(period1);
                    timeTableSUT.AddPeriod(period2);

                    var periodMerged = new ActivePeriod(period1.Start, period2.End);

                    Assert.AreEqual(periodMerged, periodStorage.GetAll().First());
                    CollectionAssert.AreEquivalent(new[] { periodMerged }, periodStorage.GetAll());
                }

                [TestMethod]
                private void twoCloseIdlePeriodShouldBeMerged()
                {
                    var period1 = MIDDAY.NewPeriod().Active();
                    var period2 = period1.NewPeriodAfter().Length(BIGGER_LENGTH).Idle();
                    var period3 = period2.NewPeriodAfter().Idle();

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
                    var period2 = PeriodBuilder.NewAfter(period1, MORE_THAN_IDLE_TIMEOUT).Active();

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
                    var period2 = PeriodBuilder.NewAfter(period1, LESS_THAN_IDLE_TIMEOUT).Idle();

                    timeTableSUT.AddPeriod(period1);
                    timeTableSUT.AddPeriod(period2);

                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new ActivityPeriod[] { period2, period1 });
                }

                [TestMethod]
                public void twoCloseActivePeriodEnclosingIdlePeriodsShouldBeMerged()
                {
                    var period0i = MIDDAY.NewPeriod().Idle();
                    var period1a = period0i.NewPeriodAfter().Active();
                    var period2i = period1a.NewPeriodAfter().Idle();
                    var period3i = period2i.NewPeriodAfter().Idle();
                    var period4a = period3i.NewPeriodAfter().Active();

                    timeTableSUT.AddPeriod(period0i);
                    timeTableSUT.AddPeriod(period1a);
                    timeTableSUT.AddPeriod(period2i);
                    timeTableSUT.AddPeriod(period3i);
                    timeTableSUT.AddPeriod(period4a);

                    var mergedPeriod = new ActivePeriod(period1a.Start, period4a.End);
                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new ActivityPeriod[] { period0i, mergedPeriod });
                }

                [TestMethod]
                public void mergingActivePeriodsShouldNotDeletePartialIdlePeriods()
                {
                    var period0i = MIDDAY.NewPeriod().Idle();
                    var period1a = period0i.NewPeriodAfter(-1.ms()).Active();
                    var period2i = period1a.NewPeriodAfter().Idle();
                    var period3a = period2i.NewPeriodAfter().Active();
                    var period4i = period3a.NewPeriodAfter(-1.ms()).Idle();

                    timeTableSUT.AddPeriod(period0i);
                    timeTableSUT.AddPeriod(period1a);
                    timeTableSUT.AddPeriod(period2i);
                    timeTableSUT.AddPeriod(period3a);
                    timeTableSUT.AddPeriod(period4i);

                    var mergedPeriod = new ActivePeriod(period1a.Start, period3a.End);
                    CollectionAssert.AreEquivalent(periodStorage.GetAll(), new ActivityPeriod[] { period0i, mergedPeriod, period4i });
                }

                [TestMethod]
                public void twoCloseIdlePeriodEnclosingActivePeriodShouldNotBeMerged()
                {
                    var period1a = MIDDAY.NewPeriod().Active();
                    var period2i = period1a.NewPeriodAfter().Length(MORE_THAN_IDLE_TIMEOUT).Idle();
                    var period3a = period2i.NewPeriodAfter().Active();
                    var peroid4i = period3a.NewPeriodAfter().Idle();

                    timeTableSUT.AddPeriod(period1a);
                    timeTableSUT.AddPeriod(period2i);
                    timeTableSUT.AddPeriod(period3a);
                    timeTableSUT.AddPeriod(peroid4i);

                    CollectionAssert.AreEquivalent(periodStorage.GetAll(),
                        new ActivityPeriod[] { period1a, period2i, period3a, peroid4i });
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
                    timeTableSUT.RegisterUserActivityListener(listenerActivityListnerMock);
                }

                [TestMethod]
                public void shouldNotifyWhenBeforeActiveThereIsLongIdlePeriod()
                {
                    // arrange
                    var idlePeriodLength = IDLE_TIMEOUT + 2.secs();
                    var periodIdleLong = PeriodBuilder.New(MIDDAY).Length(idlePeriodLength).Idle();
                    var periodActive = PeriodBuilder.NewAfter(periodIdleLong).Length(1.secs()).Active();

                    // act
                    timeTableSUT.PeriodPassed(periodIdleLong);
                    timeTableSUT.PeriodPassed(periodActive);

                    // assert
                    A.CallTo(() => listenerActivityListnerMock.NotifyActiveAfterBreak(idlePeriodLength))
                        .MustHaveHappened(Repeated.Exactly.Once);
                }

                [TestMethod]
                public void shouldNotNotifyIfWasntIdleBefore()
                {
                    // arrange
                    ActivePeriod period = PeriodBuilder.New(MIDDAY).Length(1.secs()).Active();

                    // act
                    timeTableSUT.PeriodPassed(period);

                    // assert
                    A.CallTo(() => listenerActivityListnerMock.NotifyActiveAfterBreak(A<TimeSpan>.Ignored)).MustNotHaveHappened();
                }

                [TestMethod]
                public void shouldNotNotifyIfWasShortIdleBefore()
                {
                    // arrange
                    var periodIdleShort = PeriodBuilder.New(MIDDAY).Length(LESS_THAN_IDLE_TIMEOUT).Idle();
                    var periodActive = periodIdleShort.NewPeriodAfter().Length(1.secs()).Active();

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
                    var periodIdle = PeriodBuilder.New(MIDDAY).Length(MORE_THAN_IDLE_TIMEOUT).Idle();
                    var offTime = 1.secs();
                    var periodActive = periodIdle.NewPeriodAfter(offTime).Active();

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
