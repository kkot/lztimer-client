using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telerik.JustMock;

namespace LzTimerTests
{
    [TestClass]
    public class IntegrationTests
    {
        private UserActivityChecker userActivityChecker;
        private TimeTable timeTable;
        private SimpleClock clockStub;
        private LastActivityProbeStub probeStub;
        private TimeTablePolicies policies;

        [TestInitializeAttribute]
        public void setUp()
        {
            policies  = new TimeTablePolicies() {IdleTimeoutPenalty = 30.secs()};
            probeStub = new LastActivityProbeStub();
            clockStub = new SimpleClock();
            timeTable = new TimeTable(policies);
            userActivityChecker = new UserActivityChecker(probeStub, clockStub);
            userActivityChecker.setActivityListner(timeTable);
        }

        [TestMethod]
        public void checkIdleMerged()
        {
            policies.IdleTimeout = 30.secs();

            var timePeriod = TestActivity(new[] {1, 2, 3});
            Assert.AreEqual(2, timeTable.GetTotalActiveTimespan(timePeriod).TotalSeconds);

            timePeriod = TestActivity(new[] { 1, 2, 3, 3, 3 });
            Assert.AreEqual(2, timeTable.GetTotalActiveTimespan(timePeriod).TotalSeconds);

            timePeriod = TestActivity(new[] { 1, 2, 3, 3, 3, 4 });
            Assert.AreEqual(5, timeTable.GetTotalActiveTimespan(timePeriod).TotalSeconds);
        }

        [TestMethod]
        public void checkTimeout()
        {
            policies.IdleTimeout = 2.secs();

            var timePeriod = TestActivity(new[] { 1, 2, 3, 3, 3, 3, 4 });
            Assert.AreEqual(3, timeTable.GetTotalActiveTimespan(timePeriod).TotalSeconds);

            policies.IdleTimeout = 4.secs();

            timePeriod = TestActivity(new[] { 1, 2, 3, 3, 3, 3, 4 });
            Assert.AreEqual(6, timeTable.GetTotalActiveTimespan(timePeriod).TotalSeconds);
        }



        private TimePeriod TestActivity(int[] activityChecks)
        {
            var firstCheck = new DateTime(2014, 1, 1, 12, 0, 0);
            clockStub.StartTime(firstCheck);

            probeStub.Arrange(activityChecks);

            for (int i = 0; i < activityChecks.Length; i++)
            {
                userActivityChecker.check();
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
