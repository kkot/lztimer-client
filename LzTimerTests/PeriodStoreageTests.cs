using System;
using System.IO;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LzTimerTests
{
    public abstract class PeriodStoreageTests
    {
        public void howTreatIdenticalPeriods()
        {
            var activePeriod = PeriodBuilder.New(new DateTime(2014, 1, 1, 12, 0, 0)).Active();
            var idlePeriod = PeriodBuilder.New(new DateTime(2014, 1, 1, 12, 0, 0)).Idle();
            // todo:
        }

        [TestMethod]
        public void addedPeriod_shouldBeReaderByAnotherInstance()
        {

            var activePeriod = PeriodBuilder.New(new DateTime(2014, 1, 1, 12, 0, 0)).Active();
            var idlePeriod = PeriodBuilder.NewAfter(activePeriod, 1.secs()).Idle();

            PeriodStorage instance1 = GetStorage();
            instance1.Add(activePeriod);
            instance1.Add(idlePeriod);

            var expected = new Period[] { activePeriod, idlePeriod };
            CollectionAssert.AreEquivalent(expected, instance1.GetAll());
            instance1.Close();

            if (IsPersisent())
            {
                PeriodStorage instance2 = GetStorage();
                CollectionAssert.AreEquivalent(expected, instance2.GetAll());
                instance2.Close();
            }
        }

        private static void WaitForConnectionToDbClosed()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        protected abstract PeriodStorage GetStorage();

        protected abstract bool IsPersisent();
    }

    [TestClass]
    public class SqliteStoragePeriodTests : PeriodStoreageTests
    {
        private const string DB_FILE = "test.db";

        [TestInitializeAttribute]
        public void setUp()
        {
            File.Delete(DB_FILE);
        }

        protected override PeriodStorage GetStorage()
        {
            return new SqlitePeriodStorage(DB_FILE);
        }

        protected override bool IsPersisent()
        {
            return true;
        }
    }

    [TestClass]
    public class MemoryStoragePeriodTests : PeriodStoreageTests
    {
        [TestInitializeAttribute]
        public void setUp()
        {
        }

        protected override PeriodStorage GetStorage()
        {
            return new MemoryPeriodStorage();
        }

        protected override bool IsPersisent()
        {
            return false;
        }
    }
}
