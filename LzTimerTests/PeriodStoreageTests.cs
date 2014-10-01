using System;
using System.Linq;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LzTimerTests
{
  
    [TestClass]
    public class PeriodStoreageTests
    {
        [TestInitializeAttribute]
        public void setUp()
        {
            
        }

        [TestMethod]
        public void addedPeriod_shouldBeReaderByAnotherInstance()
        {
            ActivePeriod activePeriod = PeriodBuilder.New().Active();

            PeriodStorage instance1 = GetStorage();
            instance1.Add(activePeriod);
            Assert.AreEqual(activePeriod, instance1.getAll().First());

            PeriodStorage instance2 = GetStorage();
            Assert.AreEqual(activePeriod, instance2.getAll().First());
        }

        private static PeriodStorage GetStorage()
        {
            return new SqlitePeriodStorage("test.db");
        }
    }
}
