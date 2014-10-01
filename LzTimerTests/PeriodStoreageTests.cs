using System;
using System.IO;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LzTimerTests
{
  
    [TestClass]
    public class PeriodStoreageTests
    {
        private readonly static string DB_FILE = "test.db";

        [TestInitializeAttribute]
        public void setUp()
        {
            
        }

        [TestMethod]
        public void addedPeriod_shouldBeReaderByAnotherInstance()
        {
            File.Delete(DB_FILE);
            var activePeriod = PeriodBuilder.New(new DateTime(2014, 1, 1, 12, 0, 0)).Active();
            var idlePeriod = PeriodBuilder.NewAfter(activePeriod, 1.secs()).Idle();

            SqlitePeriodStorage instance1 = GetStorage();
            instance1.Add(activePeriod);
            instance1.Add(idlePeriod);
            
            var expected = new Period[] {activePeriod, idlePeriod};
            CollectionAssert.AreEquivalent(expected, instance1.GetAll());
            instance1.Close();

            //WaitForConnectionToDbClosed();
            SqlitePeriodStorage instance2 = GetStorage();
            CollectionAssert.AreEquivalent(expected, instance2.GetAll());
            instance2.Close();
        }

        private static void WaitForConnectionToDbClosed()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static SqlitePeriodStorage GetStorage()
        {
            return new SqlitePeriodStorage(DB_FILE);
        }
    }
}
