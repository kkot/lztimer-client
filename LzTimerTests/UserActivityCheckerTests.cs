using System;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace LzTimerTests
{
    [TestClass]
    public class UserActivityCheckerTests
    {
        private LastActivityProbe probeMock;
        private UserActivityChecker userActivityCheckerSUT;
        private ActivityPeriodsListener activityListenerMock;
        private Clock clockMock;

        [TestInitialize]
        public void setUp()
        {
            probeMock = Mock.Create<LastActivityProbe>();
            activityListenerMock = Mock.Create<ActivityPeriodsListener>();
            clockMock = Mock.Create<Clock>();
            userActivityCheckerSUT = new UserActivityChecker(probeMock, clockMock);  
            userActivityCheckerSUT.setActivityListner(activityListenerMock);
        }

        private void AssertActivePeriodPassed()
        {
            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<ActivePeriod>()));
        }

        private void AssertIdlePeriodPassed()
        {
            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<IdlePeriod>()));
        }

        private void SetActivityTime(DateTime time)
        {
            Mock.Arrange(() => clockMock.CurrentTime()).Returns(time);
        }

        private void SetLastInputTick(int tick)
        {
            Mock.Arrange(() => probeMock.getLastInputTick()).Returns(tick);
        }

        [TestMethod]
        public void whenCheckingShouldAskProbe()
        {
            userActivityCheckerSUT.check();

            Mock.Assert(() => probeMock.getLastInputTick());
        }

        [TestMethod]
        public void whenAfterFirstCheck_ShouldNotNotify()
        {
            SetLastInputTick(123);
            userActivityCheckerSUT.check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<Period>()), Occurs.Never());
        }

        [TestMethod]
        public void whenLastInputTimeDontChangeShouldNotifyIdlePeriod()
        {
            SetLastInputTick(1);
            userActivityCheckerSUT.check();
            userActivityCheckerSUT.check();

            AssertIdlePeriodPassed();
        }

        [TestMethod]
        public void whenLastInputTimeDoChangeShouldNotifyActivePeriod()
        {
            SetLastInputTick(1);
            userActivityCheckerSUT.check();

            SetLastInputTick(2);
            userActivityCheckerSUT.check();

            AssertActivePeriodPassed();
        }

        [TestMethod]
        public void whenLastInputTimeChangeEverySecondTimeNotifyActiveAndIdlePeriod()
        {
            SetLastInputTick(1);
            userActivityCheckerSUT.check();

            SetLastInputTick(1);
            userActivityCheckerSUT.check();
            AssertIdlePeriodPassed();

            SetLastInputTick(2);
            userActivityCheckerSUT.check();
            AssertActivePeriodPassed();

            SetLastInputTick(2);
            userActivityCheckerSUT.check();
            AssertIdlePeriodPassed();
        }

        [TestMethod]
        public void periodLengthShouldDependOnCheckInterval()
        {
            SetLastInputTick(1);
            TimeSpan interval1 = TimeSpan.FromMilliseconds(1500);
            TimeSpan interval2 = TimeSpan.FromMilliseconds(1000);
            DateTime time1 = new DateTime();
            DateTime time2 = time1 + interval1;
            DateTime time3 = time2 + interval2;

            SetActivityTime(time1);
            userActivityCheckerSUT.check();

            SetActivityTime(time2);
            userActivityCheckerSUT.check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.Matches<Period>( p => p.Length == interval1)), Occurs.Once());   
         
            SetActivityTime(time3);
            userActivityCheckerSUT.check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.Matches<Period>(p => p.Length == interval2)), Occurs.Once());   
        }
    }
}