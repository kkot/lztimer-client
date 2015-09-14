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
        private StatsReporter _statsReporterSUT;
        private TimeTablePolicies _policies;
        private Clock _clockMock;
        private PeriodsInfoProvider _periodsInfoProvider;
        private ActivityPeriodsListener _activityPeriodsListener;

        [TestInitializeAttribute]
        public void SetUp()
        {
            _policies = new TimeTablePolicies();
            var timeTable = new TimeTable(_policies);
            _periodsInfoProvider = timeTable;
            _activityPeriodsListener = timeTable;
            _clockMock = A.Fake<Clock>();
            _statsReporterSUT = new StatsReporterImpl(_periodsInfoProvider, _policies, _clockMock);
        }

        [TestMethod]
        public void ShouldReturnOnlyPeriodsFromDay()
        {
            var day1 = new DateTime(2015, 1, 1);
            var day2 = day1.AddDays(1);
            var day3 = day2.AddDays(1);

            var period1 = PassOnePeriod(day1);
            var period2 = PassOnePeriod(day2);

            var periods = _statsReporterSUT.PeriodsFromDay(day1);
            CollectionAssert.AreEquivalent(new Period[] { period1 }, periods.ToList());

            periods = _statsReporterSUT.PeriodsFromDay(day2);
            CollectionAssert.AreEquivalent(new Period[] { period2 }, periods.ToList());

            periods = _statsReporterSUT.PeriodsFromDay(day3);
            Assert.IsTrue(periods.ToList().Any() == false);
        }

        private ActivePeriod PassOnePeriod(DateTime day)
        {
            var period = PeriodBuilder.New(day.AddHours(12)).Length(1.hours()).Active();
            _activityPeriodsListener.PeriodPassed(period);
            return period;
        }
    }
}
