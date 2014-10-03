using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LzTimerTests
{
    [TestClass]
    public class IntegrationTests
    {
        private ActivityChecker activityChecker;
        private TimeTable timeTable;
        private SimpleClock clockStub;
        private LastActivityProbeStub probeStub;
        private TimeTablePolicies policies;
        private StatsReporter statsReporter;
        private DateTime firstCheck;
        private const bool ACTIVE = true;
        private const bool IDLE = false;

        [TestInitializeAttribute]
        public void setUp()
        {
            policies  = new TimeTablePolicies {IdleTimeoutPenalty = 30.secs()};
            probeStub = new LastActivityProbeStub();
            clockStub = new SimpleClock();
            timeTable = new TimeTable(policies);
            activityChecker = new ActivityChecker(probeStub, clockStub);
            activityChecker.setActivityListner(timeTable);
            statsReporter = new StatsReporterImpl(timeTable, policies);
        }

        private void AssertTotalActive(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetStatsAfter(firstCheck).TotalActive);
        }

        private void AssertLastBreak(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetStatsAfter(firstCheck).LastBreak);
        }

        private void AssertCurrentPeriodLength(bool expectedActive, TimeSpan expectedLength)
        {
            Period period = statsReporter.GetStatsAfter(firstCheck).CurrentPeriod;
            Assert.AreEqual(expectedActive, period is ActivePeriod);
            Assert.AreEqual(expectedLength, period.Length);
        }

        private void SimulateActivity(params int[] activityChecks)
        {
            firstCheck = new DateTime(2014, 1, 1, 12, 0, 0);
            clockStub.StartTime(firstCheck);

            probeStub.Arrange(activityChecks);

            for (int i = 0; i < activityChecks.Length; i++)
            {
                activityChecker.check();
            }
            //var timePeriod = new TimePeriod(firstCheck, clockStub.PeekCurrentTime());
        }

        [TestMethod]
        public void checkTwoActive()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3);
            AssertTotalActive(2.secs());
            AssertCurrentPeriodLength(ACTIVE, 2.secs());
            AssertLastBreak(0.secs());
        }

        [TestMethod]
        public void checkTwoActiveWithIdleInside()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3, 3, 4);
            AssertTotalActive(4.secs());
            AssertCurrentPeriodLength(ACTIVE, 4.secs());
            AssertLastBreak(0.secs());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithoutTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3, 3, 4, 4, 4);
            AssertTotalActive(6.secs());
            AssertLastBreak(0.secs());
            AssertCurrentPeriodLength(ACTIVE, 6.secs());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4);
            AssertTotalActive(4.secs());
            AssertLastBreak(8.secs());
            AssertCurrentPeriodLength(IDLE, 8.secs());
        }

        [TestMethod]
        public void checkWithTimeout()
        {
            policies.IdleTimeout = 2.secs();

            SimulateActivity(1, 2, 3, 3, 3, 3, 4);
            AssertTotalActive(3.secs());
        }

        [TestMethod]
        public void checkWithoutTimeout()
        {
            policies.IdleTimeout = 4.secs();

            SimulateActivity(1, 2, 3, 3, 3, 3, 4);
            AssertTotalActive(6.secs());
        }

        [TestMethod]
        public void lastBreakShouldntBeCountIfNotAfterTimeout()
        {
            policies.IdleTimeout = 3.secs();

            SimulateActivity(1, 3, 4, 4, 4);
            AssertTotalActive(4.secs());
            AssertLastBreak(0.secs());    
        }

        [TestMethod]
        public void lastBreak_IfNotAfterTimeoutTakesPreviousBreak()
        {
            policies.IdleTimeout = 3.secs();

            SimulateActivity(1, 2, 2, 2, 2, 2, 3, 4);
            AssertTotalActive(3.secs());
            AssertLastBreak(4.secs());
        }
    }
}
