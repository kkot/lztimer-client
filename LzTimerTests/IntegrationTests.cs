using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

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

        [TestInitializeAttribute]
        public void setUp()
        {
            policies  = new TimeTablePolicies() {IdleTimeoutPenalty = 30.secs()};
            probeStub = new LastActivityProbeStub();
            clockStub = new SimpleClock();
            timeTable = new TimeTable(policies);
            activityChecker = new ActivityChecker(probeStub, clockStub);
            activityChecker.setActivityListner(timeTable);
            statsReporter = new StatsReporterImpl(timeTable, policies);
        }

        private void AssertSecondsAfter(TimeSpan timeSpan)
        {
            Assert.AreEqual(timeSpan, statsReporter.GetStatsAfter(firstCheck).TotalActive);
        }

        private void AssertLastBreak(TimeSpan timeSpan)
        {
            Assert.AreEqual(timeSpan, statsReporter.GetStatsAfter(firstCheck).LastBreak);
        }

        private void AssertCurrentPeriodLength(bool active, TimeSpan length)
        {
            Period period = statsReporter.GetStatsAfter(firstCheck).CurrentPeriod;
            Assert.AreEqual(active, period is ActivePeriod);
            Assert.AreEqual(length, period.Length);
        }

        private TimePeriod TestActivity(int[] activityChecks)
        {
            firstCheck = new DateTime(2014, 1, 1, 12, 0, 0);
            clockStub.StartTime(firstCheck);

            probeStub.Arrange(activityChecks);

            for (int i = 0; i < activityChecks.Length; i++)
            {
                activityChecker.check();
            }

            var timePeriod = new TimePeriod(firstCheck, clockStub.PeekCurrentTime());
            return timePeriod;
        }

        [TestMethod]
        public void checkTwoActive()
        {
            policies.IdleTimeout = 5.secs();
            TestActivity(new[] {1, 2, 3});
            AssertSecondsAfter(2.secs());
            AssertCurrentPeriodLength(active: true, length: 2.secs());
            AssertLastBreak(0.secs());
        }

        [TestMethod]
        public void checkTwoActiveWithIdleInside()
        {
            policies.IdleTimeout = 5.secs();
            TestActivity(new[] { 1, 2, 3, 3, 4 });
            AssertSecondsAfter(4.secs());
            AssertCurrentPeriodLength(true, 4.secs());
            AssertLastBreak(0.secs());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithoutTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            TestActivity(new[] { 1, 2, 3, 3, 4, 4, 4 });
            AssertSecondsAfter(6.secs());
            AssertCurrentPeriodLength(active: true, length: 6.secs());
        }

        [TestMethod]
        public void checkTwoActiveIdleActiveIdleWithTimeOut()
        {
            policies.IdleTimeout = 5.secs();
            TestActivity(new[] { 1, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4 });
            AssertSecondsAfter(4.secs());
            AssertLastBreak(8.secs());
            AssertCurrentPeriodLength(active: false, length: 8.secs());
        }

        [TestMethod]
        public void checkWithTimeout()
        {
            policies.IdleTimeout = 2.secs();

            TestActivity(new[] {1, 2, 3, 3, 3, 3, 4});
            AssertSecondsAfter(3.secs());
        }

        public void checkWithoutTimeout()
        {
            policies.IdleTimeout = 4.secs();

            TestActivity(new[] { 1, 2, 3, 3, 3, 3, 4 });
            AssertSecondsAfter(6.secs());
        }
    }

    public class ClockStub : Clock
    {
        private Queue<DateTime> queue;

        public void Arrange(params DateTime[] dateTimes)
        {
            queue = new Queue<DateTime>(dateTimes);
        }

        public DateTime CurrentTime()
        {
            return queue.Dequeue();
        }
    }

    public class SimpleClock : Clock
    {
        private DateTime currentTime;

        public void StartTime(DateTime dateTime)
        {
            this.currentTime = dateTime;
        }

        public DateTime CurrentTime()
        {
            DateTime time = currentTime;
            currentTime = currentTime.AddSeconds(1);
            return time;
        }
        public DateTime PeekCurrentTime()
        {
            return currentTime;
        }
    }

    public class LastActivityProbeStub : LastActivityProbe
    {
        private Queue<int> queue;

        public void Arrange(params int[] dateTimes)
        {
            queue = new Queue<int>(dateTimes);
        }

        public int getLastInputTick()
        {
            return queue.Dequeue();
        }
    }

}
