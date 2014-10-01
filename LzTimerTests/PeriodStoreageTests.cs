using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ActivePeriod activePeriod = PeriodBuilder.New().Active();
            IdlePeriod idlePeriod = PeriodBuilder.New().Idle();

            SqlitePeriodStorage instance1 = GetStorage();
            instance1.Add(activePeriod);
            instance1.Add(idlePeriod);
            
            var expected = new Period[] {activePeriod, idlePeriod};
            CollectionAssert.AreEquivalent(expected, instance1.GetAll());
            instance1.Close();
            WaitForConnectionToDbClosed();

            SqlitePeriodStorage instance2 = GetStorage();
            CollectionAssert.AreEquivalent(expected, instance2.GetAll());
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
