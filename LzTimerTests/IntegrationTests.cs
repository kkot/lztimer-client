using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LzTimerTests
{
    [TestClass]
    public class IntegrationTests
    {
        private ActivityChecker activityChecker;
        private ClockStub simpleClockStub;
        private LastActivityProbeStub probeStub;
        private TimeTablePolicies policies;
        private StatsReporter statsReporter;
        private DateTime firstCheck;
        private const bool ACTIVE = true;
        private const bool IDLE = false;

        [TestInitializeAttribute]
        public void setUp()
        {
            policies = new TimeTablePolicies {IdleTimeoutPenalty = 30.secs()};
            probeStub = new LastActivityProbeStub();
            simpleClockStub = new ClockStub();
            var timeTable = new TimeTable(policies);
            activityChecker = new ActivityChecker(probeStub, simpleClockStub);
            activityChecker.SetActivityListner(timeTable);
            statsReporter = new StatsReporterImpl(timeTable, policies);
        }

        private void AssertTotalActive(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetStatsAfter(firstCheck).TotalActive);
        }

        private void AssertLastInactiveTimespan(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetStatsAfter(firstCheck).LastInactiveTimespan);
        }

        private void AssertCurrentLogicalPeriod(bool expectedActive, TimeSpan expectedLength)
        {
            Period period = statsReporter.GetStatsAfter(firstCheck).CurrentLogicalPeriod;
            Assert.AreEqual(expectedActive, period is ActivePeriod);
            Assert.AreEqual(expectedLength, period.Length);
        }

        private void SimulateActivity(params int[] simulatedActivity)
        {
            firstCheck = new DateTime(2014, 1, 1, 12, 0, 0);
            simpleClockStub.Arrange(firstCheck);
            probeStub.Arrange(simulatedActivity);

            for (int i = 0; i < simulatedActivity.Length; i++)
            {
                activityChecker.Check();
                probeStub.NextValue();
                simpleClockStub.NextValue();
            }
        }

        private void SimulateActivityAndClock(int[] simulatedActivity, TimeSpan[] clockIntervalsSecs)
        {
            firstCheck = new DateTime(2014, 1, 1, 12, 0, 0);
            simpleClockStub.Arrange(firstCheck, clockIntervalsSecs);
            probeStub.Arrange(simulatedActivity);

            for (int i = 0; i < simulatedActivity.Length; i++)
            {
                activityChecker.Check();
                probeStub.NextValue();
                simpleClockStub.NextValue();
            }
        }

        [TestMethod]
        public void checkTwoActive()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3);
            AssertTotalActive(2.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 2.secs());
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void checkTwoActiveWithIdleInside()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3, 3, 4);
            AssertTotalActive(4.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 4.secs());
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithoutTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3, 3, 4, 4, 4);
            AssertTotalActive(6.secs());
            AssertLastInactiveTimespan(0.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 6.secs());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            SimulateActivity(1, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4);
            AssertTotalActive(4.secs());
            AssertLastInactiveTimespan(8.secs());
            AssertCurrentLogicalPeriod(IDLE, 8.secs());
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
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void lastBreak_IfNotAfterTimeoutTakesPreviousBreak()
        {
            policies.IdleTimeout = 3.secs();

            SimulateActivity(1, 2, 2, 2, 2, 2, 3, 4);
            AssertTotalActive(3.secs());
            AssertLastInactiveTimespan(4.secs());
        }

        [TestMethod]
        public void lastInactiveTimespan_AfterStartMustBe0()
        {
            policies.IdleTimeout = 3.secs();

            SimulateActivity(1, 2);
            AssertTotalActive(1.secs());
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void lastInactiveTimespan_InactiveBeforeActive()
        {
            policies.IdleTimeout = 3.secs();

            SimulateActivity(1, 1, 2);
            AssertTotalActive(1.secs());
            AssertLastInactiveTimespan(1.secs());
        }

        [TestMethod]
        public void lastInactiveTimespan_SleepAndActive()
        {
            policies.IdleTimeout = 3.secs();

            SimulateActivityAndClock(
                new[] {1, 1, 2},
                new[] {10, 1, 1}.secs());
            AssertTotalActive(1.secs());
            AssertLastInactiveTimespan(10.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 1.secs());
        }

        [TestMethod]
        public void ActiveSleepIdleActive()
        {

        }
    }
}

