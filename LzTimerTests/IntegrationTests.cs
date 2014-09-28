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
            statsReporter = new StatsReporterImpl(timeTable, policies.IdleTimeout);
        }

        [TestMethod]
        public void checkIdleMerged()
        {
            policies.IdleTimeout = 30.secs();

            TestActivity(new[] {1, 2, 3});
            AssertSecondsToday(2);

            TestActivity(new[] { 1, 2, 3, 3, 3 });
            AssertSecondsToday(2);

            TestActivity(new[] { 1, 2, 3, 3, 3, 4 });
            AssertSecondsToday(5);
        }

        private void AssertSecondsToday(int seconds)
        {
            Assert.AreEqual(seconds, statsReporter.GetStats(firstCheck).TotalToday.TotalSeconds);
        }

        [TestMethod]
        public void checkTimeout()
        {
            policies.IdleTimeout = 2.secs();

            TestActivity(new[] { 1, 2, 3, 3, 3, 3, 4 });
            AssertSecondsToday(3);

            policies.IdleTimeout = 4.secs();

            TestActivity(new[] { 1, 2, 3, 3, 3, 3, 4 });
            AssertSecondsToday(6);
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
