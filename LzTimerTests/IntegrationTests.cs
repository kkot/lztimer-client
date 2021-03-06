﻿using System.Linq;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace LzTimerTests
{
    public abstract class IntegrationTestsBase
    {
        protected ActivityChecker activityChecker;
        protected TimeTablePolicies policies;
        protected StatsReporter statsReporter;
        protected DateTime simulationStart;
        protected const bool ACTIVE = true;
        protected const bool IDLE = false;

        [TestInitializeAttribute]
        public void setUp()
        {
            policies = new TimeTablePolicies();
            var timeTable = new TimeTable(policies);
            activityChecker = new ActivityChecker(GetLastActivityProbeStub(), GetClockStub());
            activityChecker.SetActivityListner(timeTable);
            statsReporter = new StatsReporterImpl(timeTable, policies, GetClockStub());
        }

        public abstract LastActivityProbe GetLastActivityProbeStub();

        public abstract Clock GetClockStub();

        protected void AssertTotalActive(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetTotalActiveToday(simulationStart));
        }

        protected void AssertLastInactiveTimespan(TimeSpan expected)
        {
            Assert.AreEqual(expected, statsReporter.GetLastInactiveTimespan());
        }

        protected void AssertCurrentLogicalPeriod(bool expectedActive, TimeSpan expectedLength)
        {
            ActivityPeriod activityPeriod = statsReporter.GetCurrentLogicalPeriod();
            Assert.AreEqual(expectedActive, activityPeriod is ActivePeriod);
            Assert.AreEqual(expectedLength, activityPeriod.Length);
        }
    }

    [TestClass]
    public class IntegrationTestsBasic : IntegrationTestsBase
    {
        protected ArrangableClockStub clockStub;
        protected ArrangableLastActivityProbeStub probeStub;

        public IntegrationTestsBasic()
        {
            clockStub = new ArrangableClockStub();
            probeStub = new ArrangableLastActivityProbeStub();
        }

        public override LastActivityProbe GetLastActivityProbeStub()
        {
            return probeStub;
        }

        public override Clock GetClockStub()
        {
            return clockStub;
        }

        private void Simulate(params object[] simulated)
        {
            simulationStart = new DateTime(2014, 1, 1, 12, 0, 0);

            var simulatedActivity = simulated.OfType<int>().ToArray();
            var periodLengths = simulated.OfType<TimeSpan>().ToArray();

            clockStub.Arrange(simulationStart, periodLengths);
            probeStub.Arrange(simulatedActivity);

            for (var i = 0; i < simulatedActivity.Length; i++)
            {
                activityChecker.Check();
                probeStub.NextValue();
                clockStub.NextValue();
            }
        }

        [TestMethod]
        public void twoActive()
        {
            policies.IdleTimeout = 5.secs();
            Simulate(1, 2, 3);
            AssertTotalActive(2.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 2.secs());
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void twoActiveWithShortIdleInside()
        {
            policies.IdleTimeout = 5.secs();
            Simulate(1, 2, 3, 3, 4);
            AssertTotalActive(4.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 4.secs());
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void checkTwoActiveShortIdleActiveIdleWithoutTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            Simulate(1, 2, 3, 3, 4, 4, 4);
            AssertTotalActive(6.secs());
            AssertLastInactiveTimespan(0.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 6.secs());
        }

        [TestMethod]
        public void checkActiveLongInactiveActiveShortInactive()
        {
            policies.IdleTimeout = 3.secs();
            Simulate(1, 2, 3, 3, 3, 3, 3, 4, 5, 6, 6);
            AssertTotalActive(6.secs());
            AssertLastInactiveTimespan(4.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 4.secs());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            Simulate(1, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4);
            AssertTotalActive(4.secs());
            AssertLastInactiveTimespan(8.secs());
            AssertCurrentLogicalPeriod(IDLE, 8.secs());
        }

        [TestMethod]
        public void checkWithTimeout()
        {
            policies.IdleTimeout = 2.secs();

            Simulate(1, 2, 3, 3, 3, 3, 4);
            AssertTotalActive(3.secs());
        }

        [TestMethod]
        public void checkWithoutTimeout()
        {
            policies.IdleTimeout = 4.secs();

            Simulate(1, 2, 3, 3, 3, 3, 4);
            AssertTotalActive(6.secs());
        }

        [TestMethod]
        public void lastBreakShouldntBeCountIfNotAfterTimeout()
        {
            policies.IdleTimeout = 3.secs();

            Simulate(1, 3, 4, 4, 4);
            AssertTotalActive(4.secs());
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void lastBreak_IfNotAfterTimeoutTakesPreviousBreak()
        {
            policies.IdleTimeout = 3.secs();

            Simulate(1, 2, 2, 2, 2, 2, 3, 4);
            AssertTotalActive(3.secs());
            AssertLastInactiveTimespan(4.secs());
        }

        [TestMethod]
        public void lastInactiveTimespan_AfterStartMustBe0()
        {
            policies.IdleTimeout = 3.secs();

            Simulate(1, 2);
            AssertTotalActive(1.secs());
            AssertLastInactiveTimespan(0.secs());
        }

        [TestMethod]
        public void lastInactiveTimespan_InactiveBeforeActive()
        {
            policies.IdleTimeout = 3.secs();

            Simulate(1, 1, 2);
            AssertTotalActive(1.secs());
            AssertLastInactiveTimespan(1.secs());
        }

        [TestMethod]
        public void lastInactiveTimespan_SleepAndActive()
        {
            policies.IdleTimeout = 3.secs();

            Simulate(
                1, 1.secs(), 1, 10.secs(), 1, 1.secs(), 2);
            AssertTotalActive(1.secs());
            AssertLastInactiveTimespan(11.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 1.secs());
        }

        [TestMethod]
        public void ActiveSleepIdleActive()
        {
            policies.IdleTimeout = 3.secs();

            Simulate(
                1, 1.secs(), 2, 10.secs(), 2, 1.secs(), 2, 1.secs(), 3);
            AssertTotalActive(2.secs());
            AssertLastInactiveTimespan(11.secs());
            AssertCurrentLogicalPeriod(ACTIVE, 1.secs());
        }
    }

    [TestClass]
    public class IntegrationTestsPeriods : IntegrationTestsBase
    {
         enum TestPeriodType { ACTIVE, IDLE, SLEEP };

        class TestPeriod
        {
            public TestPeriodType Type;
            public TimeSpan Length;
        }

        protected SimpleClockStub clockStub;
        protected SimpleLastActivityProbeStub probeStub;

        public IntegrationTestsPeriods()
        {
            clockStub = new SimpleClockStub();
            probeStub = new SimpleLastActivityProbeStub();
        }

        public override LastActivityProbe GetLastActivityProbeStub()
        {
            return probeStub;
        }

        public override Clock GetClockStub()
        {
            return clockStub;
        }

        private void SimulatePeriods(params TestPeriod[] periods)
        {
            simulationStart = new DateTime(2014, 1, 1, 12, 0, 0);

            /*
            var simulatedActivity = simulated.Where(e => e is int).Select(e => (int)e).ToArray();
            var periodLengths = simulated.Where(e => e is TimeSpan).Select(e => (TimeSpan)e).ToArray();

            clockStub.Arrange(simulationStart, periodLengths);
            probeStub.Arrange(simulatedActivity);

            for (var i = 0; i < simulatedActivity.Length; i++)
            {
                activityChecker.Check();
                probeStub.NextValue();
                clockStub.NextValue();
            }
            */
        }
    }
}