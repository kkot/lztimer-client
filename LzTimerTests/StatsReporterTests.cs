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
        private PeriodsInfoProvider _periodsInfoProviderMock;

        [TestInitializeAttribute]
        public void SetUp()
        {
            _periodsInfoProviderMock = A.Fake<PeriodsInfoProvider>();
            _policies = new TimeTablePolicies();
            _clockMock = A.Fake<Clock>();
            _statsReporterSUT = new StatsReporterImpl(_periodsInfoProviderMock, _policies, _clockMock);
        }

        [TestMethod]
        public void ShouldReturnOnlyPeriodsFromToday()
        {
            Assert.Fail("TODO");
            
        }
    }
}
