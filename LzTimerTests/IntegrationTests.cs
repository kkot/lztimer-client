using System.Linq;
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
        private DateTime simulationStart;
        private const bool ACTIVE = true;
        private const bool IDLE = false;

        [TestInitializeAttribute]
        public void setUp()
        {
            policies = new TimeTablePolicies {IdleTimeoutPenalty = 30.s()};
            probeStub = new LastActivityProbeStub();
            simpleClockStub = new ClockStub();
            var timeTable = new TimeTable(policies);
            activityChecker = new ActivityChecker(probeStub, simpleClockStub);
            activityChecker.SetActivityListner(timeTable);
            statsReporter = new StatsReporterImpl(timeTable, policies);
        }

        private void AssertTotalActive(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetStatsAfter(simulationStart).TotalActiveToday);
        }

        private void AssertLastInactiveTimespan(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetStatsAfter(simulationStart).LastInactiveTimespan);
        }

        private void AssertCurrentLogicalPeriod(bool expectedActive, TimeSpan expectedLength)
        {
            Period period = statsReporter.GetStatsAfter(simulationStart).CurrentLogicalPeriod;
            Assert.AreEqual(expectedActive, period is ActivePeriod);
            Assert.AreEqual(expectedLength, period.Length);
        }

        private void Simulate(params object[] simulated)
        {
            simulationStart = new DateTime(2014, 1, 1, 12, 0, 0);

            var simulatedActivity = simulated.Where(e => e is int).Select(e => (int)e).ToArray();
            var periodLengths = simulated.Where(e => e is TimeSpan).Select(e => (TimeSpan)e).ToArray();

            simpleClockStub.Arrange(simulationStart, periodLengths);
            probeStub.Arrange(simulatedActivity);

            for (var i = 0; i < simulatedActivity.Length; i++)
            {
                activityChecker.Check();
                probeStub.NextValue();
                simpleClockStub.NextValue();
            }
        }

        [TestMethod]
        public void checkTwoActive()
        {
            policies.IdleTimeout = 5.s();
            Simulate(1, 2, 3);
            AssertTotalActive(2.s());
            AssertCurrentLogicalPeriod(ACTIVE, 2.s());
            AssertLastInactiveTimespan(0.s());
        }

        [TestMethod]
        public void checkTwoActiveWithIdleInside()
        {
            policies.IdleTimeout = 5.s();
            Simulate(1, 2, 3, 3, 4);
            AssertTotalActive(4.s());
            AssertCurrentLogicalPeriod(ACTIVE, 4.s());
            AssertLastInactiveTimespan(0.s());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithoutTimeOut()
        {
            policies.IdleTimeout = 5.s();
            Simulate(1, 2, 3, 3, 4, 4, 4);
            AssertTotalActive(6.s());
            AssertLastInactiveTimespan(0.s());
            AssertCurrentLogicalPeriod(ACTIVE, 6.s());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithTimeOut()
        {
            policies.IdleTimeout = 5.s();
            Simulate(1, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4);
            AssertTotalActive(4.s());
            AssertLastInactiveTimespan(8.s());
            AssertCurrentLogicalPeriod(IDLE, 8.s());
        }

        [TestMethod]
        public void checkWithTimeout()
        {
            policies.IdleTimeout = 2.s();

            Simulate(1, 2, 3, 3, 3, 3, 4);
            AssertTotalActive(3.s());
        }

        [TestMethod]
        public void checkWithoutTimeout()
        {
            policies.IdleTimeout = 4.s();

            Simulate(1, 2, 3, 3, 3, 3, 4);
            AssertTotalActive(6.s());
        }

        [TestMethod]
        public void lastBreakShouldntBeCountIfNotAfterTimeout()
        {
            policies.IdleTimeout = 3.s();

            Simulate(1, 3, 4, 4, 4);
            AssertTotalActive(4.s());
            AssertLastInactiveTimespan(0.s());
        }

        [TestMethod]
        public void lastBreak_IfNotAfterTimeoutTakesPreviousBreak()
        {
            policies.IdleTimeout = 3.s();

            Simulate(1, 2, 2, 2, 2, 2, 3, 4);
            AssertTotalActive(3.s());
            AssertLastInactiveTimespan(4.s());
        }

        [TestMethod]
        public void lastInactiveTimespan_AfterStartMustBe0()
        {
            policies.IdleTimeout = 3.s();

            Simulate(1, 2);
            AssertTotalActive(1.s());
            AssertLastInactiveTimespan(0.s());
        }

        [TestMethod]
        public void lastInactiveTimespan_InactiveBeforeActive()
        {
            policies.IdleTimeout = 3.s();

            Simulate(1, 1, 2);
            AssertTotalActive(1.s());
            AssertLastInactiveTimespan(1.s());
        }

        [TestMethod]
        public void lastInactiveTimespan_SleepAndActive()
        {
            policies.IdleTimeout = 3.s();

            Simulate(
                1, 10.s(), 1, 1.s(), 2);
            AssertTotalActive(1.s());
            AssertLastInactiveTimespan(10.s());
            AssertCurrentLogicalPeriod(ACTIVE, 1.s());
        }

        [TestMethod]
        public void ActiveSleepIdleActive()
        {
            policies.IdleTimeout = 3.s();

            Simulate(
                1, 1.s(), 2, 10.s(), 2, 1.s(), 2, 1.s(), 3);
            AssertTotalActive(2.s());
            AssertLastInactiveTimespan(11.s());
            AssertCurrentLogicalPeriod(ACTIVE, 1.s());
        }
    }
}

