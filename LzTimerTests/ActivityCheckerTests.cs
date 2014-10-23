using System;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace LzTimerTests
{
    [TestClass]
    public class ActivityCheckerTests
    {
        private LastActivityProbe probeMock;
        private ActivityChecker activityCheckerSut;
        private ActivityPeriodsListener activityListenerMock;
        private Clock clockMock;
        private readonly DateTime START = new DateTime(2014, 1, 1, 12, 0, 0);

        [TestInitializeAttribute]
        public void setUp()
        {
            probeMock = Mock.Create<LastActivityProbe>();
            activityListenerMock = Mock.Create<ActivityPeriodsListener>();
            clockMock = Mock.Create<Clock>();
            activityCheckerSut = new ActivityChecker(probeMock, clockMock);
            activityCheckerSut.SetActivityListner(activityListenerMock);
            SetCurrentTime(DateTime.Now);
        }

        private void AssertActivePeriodPassed()
        {
            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<ActivePeriod>()));
        }

        private void AssertIdlePeriodPassed()
        {
            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<IdlePeriod>()));
        }

        private void SetCurrentTime(DateTime time)
        {
            Mock.Arrange(() => clockMock.CurrentTime()).Returns(time);
        }

        private void SetLastInputTick(int tick)
        {
            Mock.Arrange(() => probeMock.GetLastInputTick()).Returns(tick);
        }

        [TestMethod]
        public void whenCheckingShouldAskProbe()
        {
            activityCheckerSut.Check();

            Mock.Assert(() => probeMock.GetLastInputTick());
        }

        [TestMethod]
        public void whenAfterFirstCheck_ShouldNotNotify()
        {
            SetLastInputTick(123);
            activityCheckerSut.Check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<Period>()), Occurs.Never());
        }

        [TestMethod]
        public void whenLastInputTimeDontChangeShouldNotifyIdlePeriod()
        {
            SetLastInputTick(1);
            activityCheckerSut.Check();
            activityCheckerSut.Check();

            AssertIdlePeriodPassed();
        }

        [TestMethod]
        public void whenLastInputTimeDoChangeShouldNotifyActivePeriod()
        {
            SetLastInputTick(1);
            activityCheckerSut.Check();

            SetLastInputTick(2);
            activityCheckerSut.Check();

            AssertActivePeriodPassed();
        }

        [TestMethod]
        public void whenLastInputTimeChangeEverySecondTimeNotifyActiveAndIdlePeriod()
        {
            SetCurrentTime(DateTime.Now);
            SetLastInputTick(1);
            activityCheckerSut.Check();

            SetLastInputTick(1);
            activityCheckerSut.Check();
            AssertIdlePeriodPassed();

            SetLastInputTick(2);
            activityCheckerSut.Check();
            AssertActivePeriodPassed();

            SetLastInputTick(2);
            activityCheckerSut.Check();
            AssertIdlePeriodPassed();
        }

        [TestMethod]
        public void periodLengthShouldDependOnCheckInterval()
        {
            SetLastInputTick(1);
            var interval1 = 1500.ms();
            var interval2 = 1000.ms();
            DateTime time1 = START;
            DateTime time2 = time1 + interval1;
            DateTime time3 = time2 + interval2;

            SetCurrentTime(time1);
            activityCheckerSut.Check();

            SetCurrentTime(time2);
            activityCheckerSut.Check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.Matches<Period>(p => p.Length == interval1)), Occurs.Once());

            SetCurrentTime(time3);
            activityCheckerSut.Check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.Matches<Period>(p => p.Length == interval2)), Occurs.Once());
        }

        [TestMethod]
        public void periodLengthShouldBeRoundedTo100ms()
        {
            DebugView.IsTraceEnabled = true;

            SetLastInputTick(1);
            var interval1 = 1051.ms();
            var interval2 = 1049.ms();
            DateTime time1 = START;
            DateTime time2 = time1 + interval1;
            DateTime time3 = time2 + interval2;

            Mock.Arrange(() => activityListenerMock.PeriodPassed(Arg.Matches<Period>(p => p.Length == 1100.ms()))).InOrder();
            Mock.Arrange(() => activityListenerMock.PeriodPassed(Arg.Matches<Period>(p => p.Length == 1000.ms()))).InOrder();

            SetCurrentTime(time1);
            activityCheckerSut.Check();

            SetCurrentTime(time2);
            activityCheckerSut.Check();

            SetCurrentTime(time3);
            activityCheckerSut.Check();

            Mock.Assert(activityListenerMock);
        }

        private DateTime AssertPeriodLengthAfter(DateTime time, TimeSpan after, TimeSpan expectedLength)
        {
            Mock.Reset();

            time = time + after;

            SetLastInputTick(1);
            SetCurrentTime(time);

            Mock.Arrange(() => activityListenerMock.PeriodPassed(Arg.IsAny<Period>())).
                DoInstead((Period p) => Console.WriteLine("Actual " + p.Length + " expected " + expectedLength));

            activityCheckerSut.Check();
            AssertPassedPeriodLength(expectedLength);
            return time;
        }

        private void AssertPassedPeriodLength(TimeSpan expectedLength)
        {
            Mock.Assert(() => activityListenerMock.PeriodPassed(
                Arg.Matches<Period>(e => e.Length == expectedLength)));
        }
    }
}