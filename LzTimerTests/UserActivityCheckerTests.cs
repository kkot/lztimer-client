using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace LzTimerTests
{
    [TestClass]
    public class UserActivityCheckerTests
    {
        private LastActivityProbe probeMock;
        private UserActivityChecker userActivityChecker;
        private ActivityPeriodsListener activityListenerMock;
        private Clock clockMock;

        [TestInitialize]
        public void setUp()
        {
            probeMock = Mock.Create<LastActivityProbe>();
            activityListenerMock = Mock.Create<ActivityPeriodsListener>();
            clockMock = Mock.Create<Clock>();
            userActivityChecker = new UserActivityChecker(probeMock, clockMock);  
            userActivityChecker.setActivityListner(activityListenerMock);
        }

        [TestMethod]
        public void whenCheckingShouldAskProbe()
        {
            userActivityChecker.check();

            Mock.Assert(() => probeMock.getLastInputTime());
        }

        [TestMethod]
        public void whenAfterFirstCheck_ShouldNotNotify()
        {
            Mock.Arrange(() => probeMock.getLastInputTime()).Returns(123);
            userActivityChecker.check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<Period>()), Occurs.Never());
        }

        [TestMethod]
        public void whenLastInputTimeDontChangeShouldNotifyIdlePeriod()
        {
            Mock.Arrange(() => probeMock.getLastInputTime()).Returns(1);
            userActivityChecker.check();
            userActivityChecker.check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<IdlePeriod>()), Occurs.Once());
        }

        [TestMethod]
        public void whenLastInputTimeDoChangeShouldNotifyActivePeriod()
        {
            Mock.Arrange(() => probeMock.getLastInputTime()).Returns(1);
            userActivityChecker.check();

            Mock.Arrange(() => probeMock.getLastInputTime()).Returns(2);
            userActivityChecker.check();

            Mock.Assert(() => activityListenerMock.PeriodPassed(Arg.IsAny<ActivePeriod>()), Occurs.Once());
        }
    }
}