using System;
using System.IO;
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
            File.Delete("test.db");
            ActivePeriod activePeriod = PeriodBuilder.New().Active();

            SqlitePeriodStorage instance1 = GetStorage();
            instance1.Add(activePeriod);
            Assert.AreEqual(activePeriod, instance1.getAll().First());
            instance1.Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            SqlitePeriodStorage instance2 = GetStorage();
            Assert.AreEqual(activePeriod, instance2.getAll().First());
        }

        private static SqlitePeriodStorage GetStorage()
        {
            return new SqlitePeriodStorage("test.db");
        }
    }
}
