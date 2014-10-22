using System;
using System.IO;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LzTimerTests
{
    public abstract class PeriodStoreageTests
    {
        private DateTime START_DATETIME = new DateTime(2014, 1, 1, 12, 0, 0);

        public void howTreatIdenticalPeriods()
        {
            var activePeriod = PeriodBuilder.New(START_DATETIME).Active();
            var idlePeriod = PeriodBuilder.New(START_DATETIME).Idle();
            // todo:
        }

        [TestMethod]
        public void addedPeriod_shouldBeReaderByAnotherInstance()
        {
            var activePeriod = PeriodBuilder.New(START_DATETIME).Active();
            var idlePeriod = PeriodBuilder.NewAfter(activePeriod, 1.s()).Idle();
            var expected = new Period[] { activePeriod, idlePeriod };
            {
                PeriodStorage instance1 = GetStorage();
                instance1.Add(activePeriod);
                instance1.Add(idlePeriod);

                CollectionAssert.AreEquivalent(expected, instance1.GetAll());
                instance1.Dispose();
            }

            if (IsPersisent())
            {
                WaitForConnectionToDbClosed();
                PeriodStorage instance2 = GetStorage();
                CollectionAssert.AreEquivalent(expected, instance2.GetAll());
                instance2.Dispose();
            }
        }

        [TestMethod]
        public void removePeriod_shouldWork()
        {
            var firstPeriod = PeriodBuilder.New(START_DATETIME).Length(5.s()).Active();
            var secondPeriod = PeriodBuilder.NewAfter(firstPeriod, 10.s()).Idle();

            PeriodStorage periodStorageSUT = GetStorage();
            periodStorageSUT.Add(firstPeriod);
            periodStorageSUT.Add(secondPeriod);
            var expected = new Period[] { secondPeriod };

            periodStorageSUT.Remove(firstPeriod);
            CollectionAssert.AreEquivalent(expected, periodStorageSUT.GetAll());
            periodStorageSUT.Dispose();

            if (IsPersisent())
            {
                WaitForConnectionToDbClosed();
                PeriodStorage newInstance = GetStorage();
                CollectionAssert.AreEquivalent(expected, newInstance.GetAll());
                newInstance.Dispose();
            }
        }

        [TestMethod]
        public void GetPeriodsFromTimePeriod_shouldReturnPeriodsPartiallyInsideRange()
        {
            var firstPeriod = START_DATETIME.Length(5.s()).Active();
            var secondPeriod = firstPeriod.NewAfter(10.s()).Length(5.s()).Active();
            var thirdPeriod = secondPeriod.NewAfter(10.s()).Length(5.s()).Active();

            var enclosingSearchPeriod = new TimePeriod(
                secondPeriod.Start - 1.s(),
                secondPeriod.End + 1.s());

            var enclosingOnlyEndSearchTimePriod = new TimePeriod(
                secondPeriod.Start + 1.s(),
                secondPeriod.End + 1.s());

            var enclosingOnlyStartSearchTimePeriod = new TimePeriod(
                secondPeriod.Start - 1.s(),
                secondPeriod.End - 1.s());

            var notEnclosingSearchTimePeriod = new TimePeriod(
                secondPeriod.Start - 2.s(),
                secondPeriod.Start - 1.s());

            using (PeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                periodStorageSUT.Add(secondPeriod);
                periodStorageSUT.Add(thirdPeriod);

                var found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    enclosingSearchPeriod);
                CollectionAssert.AreEquivalent(new Period[] {secondPeriod}, found);

                found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    enclosingOnlyEndSearchTimePriod);
                CollectionAssert.AreEquivalent(new Period[] {secondPeriod}, found);

                found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    enclosingOnlyStartSearchTimePeriod);
                CollectionAssert.AreEquivalent(new Period[] {secondPeriod}, found);

                found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    notEnclosingSearchTimePeriod);
                CollectionAssert.AreEquivalent(new Period[] {}, found);
            }
        }

        [TestMethod]
        public void getPeriodsAfter_shouldReturnPeriodsPartiallyAfter()
        {
            var periodBefore = PeriodBuilder.New(START_DATETIME).Length(5.s()).Active();
            var period = PeriodBuilder.NewAfter(periodBefore, 10.s()).Length(5.s()).Idle();

            PeriodStorage periodStorageSUT = GetStorage();
            periodStorageSUT.Add(periodBefore);
            periodStorageSUT.Add(period);
            var expected = new Period[] { period };

            var beforeStart = period.Start - 1.s();
            var found = periodStorageSUT.GetPeriodsAfter(beforeStart);
            CollectionAssert.AreEquivalent(expected, found);

            var afterStart = period.Start + 1.s();
            found = periodStorageSUT.GetPeriodsAfter(afterStart);
            CollectionAssert.AreEquivalent(expected, found);

            var atEnd = period.End;
            found = periodStorageSUT.GetPeriodsAfter(atEnd);
            CollectionAssert.AreEquivalent(new Period[] { }, found);

            var afterEnd = period.End + 1.s();
            found = periodStorageSUT.GetPeriodsAfter(afterEnd);
            CollectionAssert.AreEquivalent(new Period[] { }, found);

            periodStorageSUT.Dispose();
        }

        [TestMethod]
        public void GetSinceFirstActivePeriodBeforeTest()
        {
            var firstPeriod = START_DATETIME.Length(5.s()).Active();
            var secondPeriod = firstPeriod.NewAfter(10.s()).Idle();

            PeriodStorage periodStorageSUT = GetStorage();
            periodStorageSUT.Add(firstPeriod);
            periodStorageSUT.Add(secondPeriod);
            var expected = new Period[] { secondPeriod };

            var found = periodStorageSUT.GetPeriodsAfter(firstPeriod.End + 1.s());
            CollectionAssert.AreEquivalent(expected, found);
            periodStorageSUT.Dispose();
        }

        protected abstract PeriodStorage GetStorage();

        protected abstract bool IsPersisent();

        protected static void WaitForConnectionToDbClosed()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    [TestClass]
    public class SqliteStoragePeriodTests : PeriodStoreageTests
    {
        private const string DB_FILE = "test.db";

        [TestInitializeAttribute]
        public void setUp()
        {
            WaitForConnectionToDbClosed();
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
