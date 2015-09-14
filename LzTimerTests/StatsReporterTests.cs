using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FakeItEasy;
using kkot.LzTimer;

namespace LzTimerTests
{
    [TestClass]
    public class StatsReporterTests
    {
        private StatsReporter statsReporterSUT;
        private TimeTablePolicies policies;
        private Clock clockMock;
        private PeriodsInfoProvider periodsInfoProvider;
        private ActivityPeriodsListener activityPeriodsListener;

        [TestInitializeAttribute]
        public void SetUp()
        {
            policies = new TimeTablePolicies();
            var timeTable = new TimeTable(policies);
            periodsInfoProvider = timeTable;
            activityPeriodsListener = timeTable;
            clockMock = A.Fake<Clock>();
            statsReporterSUT = new StatsReporterImpl(periodsInfoProvider, policies, clockMock);
        }

        [TestMethod]
        public void ShouldReturnOnlyPeriodsFromDay()
        {
            var day1 = new DateTime(2015, 1, 1);
            var day2 = day1.AddDays(1);
            var day3 = day2.AddDays(1);

            var period1 = PassOnePeriod(day1);
            var period2 = PassOnePeriod(day2);

            var periods = statsReporterSUT.PeriodsFromDay(day1);
            CollectionAssert.AreEquivalent(new ActivityPeriod[] { period1 }, periods.ToList());

            periods = statsReporterSUT.PeriodsFromDay(day2);
            CollectionAssert.AreEquivalent(new ActivityPeriod[] { period2 }, periods.ToList());

            periods = statsReporterSUT.PeriodsFromDay(day3);
            Assert.IsTrue(periods.ToList().Any() == true);
        }

        private ActivePeriod PassOnePeriod(DateTime day)
        {
            var period = PeriodBuilder.New(day.AddHours(12)).Length(1.hours()).Active();
            activityPeriodsListener.PeriodPassed(period);
            return period;
        }
    }
}
