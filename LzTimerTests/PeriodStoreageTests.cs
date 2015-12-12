using System;
using System.IO;
using kkot.LzTimer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LzTimerTests
{
    public abstract class PeriodStoreageTests
    {
        private DateTime START_DATETIME = new DateTime(2014, 1, 1, 12, 0, 0);

        [TestMethod]
        public void AddedPeriodsShouldBeReaderByAnotherInstance()
        {
            var period1 = START_DATETIME.Length(1.secs()).Active();
            var period2 = period1.NewPeriodAfter(2.secs()).Idle();
            var expected = new ActivityPeriod[] { period1, period2 };

            using (TestablePeriodStorage instance1 = GetStorage())
            {
                instance1.Add(period1);
                instance1.Add(period2);

                CollectionAssert.AreEquivalent(expected, instance1.GetAll());
            }

            if (IsStoragePersisent())
            {
                WaitForConnectionToDbClosed();
                using (TestablePeriodStorage instance2 = GetStorage())
                {
                    CollectionAssert.AreEquivalent(expected, instance2.GetAll());
                }
            }
        }

        [TestMethod]
        public void RemovePeriod()
        {
            var firstPeriod = START_DATETIME.Length(5.secs()).Active();
            var secondPeriod = firstPeriod.NewPeriodAfter(10.secs()).Idle();

            using (var periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                periodStorageSUT.Add(secondPeriod);

                periodStorageSUT.Remove(firstPeriod);
            }

            if (!IsStoragePersisent())
                return;

            WaitForConnectionToDbClosed();
            using (var newInstance = GetStorage())
            {
                CollectionAssert.AreEquivalent(new[] { secondPeriod }, newInstance.GetAll());
            }
        }

        [TestMethod]
        public void RemovePeriodShouldRemoveOnlyExactMatches()
        {
            var period1 = START_DATETIME.Length(5.secs()).Active();
            var period2 = period1.NewPeriodAfter().Idle();
            var notExactOutside2 = PeriodBuilder.New(period2.Start - 1.ms()).WithEnd(period2.End + 1.ms()).Active();
            var notExactInside2 = PeriodBuilder.New(period2.Start + 1.ms()).WithEnd(period2.End - 1.ms()).Idle();

            using (TestablePeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(period1);
                periodStorageSUT.Add(period2);

                periodStorageSUT.Remove(period1);
                periodStorageSUT.Remove(notExactOutside2);
                periodStorageSUT.Remove(notExactInside2);
                CollectionAssert.AreEquivalent(new[] { period2 }, periodStorageSUT.GetAll());
            }
        }

        [TestMethod]
        public void GetPeriodsFromTimePeriodShouldReturnPeriodsPartiallyInsideRange()
        {
            var firstPeriod = START_DATETIME.Length(5.secs()).Active();
            var secondPeriod = firstPeriod.NewPeriodAfter(10.secs()).Length(5.secs()).Active();
            var thirdPeriod = secondPeriod.NewPeriodAfter(10.secs()).Length(5.secs()).Active();

            var enclosingSearchPeriod = new Period(
                secondPeriod.Start - 1.secs(),
                secondPeriod.End + 1.secs());

            var enclosingOnlyEndSearchTimePriod = new Period(
                secondPeriod.Start + 1.secs(),
                secondPeriod.End + 1.secs());

            var enclosingOnlyStartSearchTimePeriod = new Period(
                secondPeriod.Start - 1.secs(),
                secondPeriod.End - 1.secs());

            var notEnclosingSearchTimePeriod = new Period(
                secondPeriod.Start - 2.secs(),
                secondPeriod.Start - 1.secs());

            using (PeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                periodStorageSUT.Add(secondPeriod);
                periodStorageSUT.Add(thirdPeriod);

                var found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    enclosingSearchPeriod);
                CollectionAssert.AreEquivalent(new ActivityPeriod[] { secondPeriod }, found);

                found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    enclosingOnlyEndSearchTimePriod);
                CollectionAssert.AreEquivalent(new ActivityPeriod[] { secondPeriod }, found);

                found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    enclosingOnlyStartSearchTimePeriod);
                CollectionAssert.AreEquivalent(new ActivityPeriod[] { secondPeriod }, found);

                found = periodStorageSUT.GetPeriodsFromTimePeriod(
                    notEnclosingSearchTimePeriod);
                CollectionAssert.AreEquivalent(new ActivityPeriod[] { }, found);
            }
        }

        [TestMethod]
        public void GetPeriodBeforeShouldReturnPeriodDirectlyBefore()
        {
            var firstPeriod = START_DATETIME.Length(5.secs()).Active();
            var secondPeriod = firstPeriod.NewPeriodAfter().Length(5.secs()).Active();

            using (PeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                periodStorageSUT.Add(secondPeriod);

                var found = periodStorageSUT.GetPeriodBefore(secondPeriod.Start);
                Assert.AreEqual(firstPeriod, found);
            }
        }

        [TestMethod]
        public void GetPeriodBeforeShouldReturnPeriodNotDirectlyBefore()
        {
            var TIME_GAP = 1.secs();
            var firstPeriod = START_DATETIME.NewPeriod().Active();
            var secondPeriod = firstPeriod.NewPeriodAfter(TIME_GAP).Active();

            using (PeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                periodStorageSUT.Add(secondPeriod);

                var found = periodStorageSUT.GetPeriodBefore(secondPeriod.Start);
                Assert.AreEqual(firstPeriod, found);
            }
        }

        [TestMethod]
        public void GetPeriodBeforeShouldReturnLatest()
        {
            var firstPeriod = START_DATETIME.NewPeriod().Active();
            var secondPeriod = firstPeriod.NewPeriodAfter().Active();
            var thirdPeriod = secondPeriod.NewPeriodAfter().Active();

            using (PeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                periodStorageSUT.Add(secondPeriod);
                periodStorageSUT.Add(thirdPeriod);

                var found = periodStorageSUT.GetPeriodBefore(thirdPeriod.Start);
                Assert.AreEqual(secondPeriod, found);
            }
        }

        [TestMethod]
        public void GetPeriodsAfterShouldReturnPeriodsPartiallyAfter()
        {
            var periodBefore = START_DATETIME.Length(5.secs()).Active();
            var period = periodBefore.NewPeriodAfter(10.secs()).Length(5.secs()).Idle();

            using (PeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(periodBefore);
                periodStorageSUT.Add(period);
                var expected = new ActivityPeriod[] { period };

                var beforeStart = period.Start - 1.secs();
                var found = periodStorageSUT.GetPeriodsAfter(beforeStart);
                CollectionAssert.AreEquivalent(expected, found);

                var afterStart = period.Start + 1.secs();
                found = periodStorageSUT.GetPeriodsAfter(afterStart);
                CollectionAssert.AreEquivalent(expected, found);

                var atEnd = period.End;
                found = periodStorageSUT.GetPeriodsAfter(atEnd);
                CollectionAssert.AreEquivalent(new ActivityPeriod[] { }, found);

                var afterEnd = period.End + 1.secs();
                found = periodStorageSUT.GetPeriodsAfter(afterEnd);
                CollectionAssert.AreEquivalent(new ActivityPeriod[] { }, found);
            }
        }

        [TestMethod]
        public void ExecuteInTransactionShouldBeExecuted()
        {
            var firstPeriod = START_DATETIME.NewPeriod().Active();
            var secondPeriod = firstPeriod.NewPeriodAfter().Active();

            using (TestablePeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                periodStorageSUT.ExecuteInTransaction(() =>
                {
                    periodStorageSUT.Remove(firstPeriod);
                    periodStorageSUT.Add(secondPeriod);
                });

                var found = periodStorageSUT.GetAll();
                CollectionAssert.AreEqual(new ActivityPeriod[] { secondPeriod }, found);
            }
        }

        [TestMethod]
        public void ExecuteInTransactionShouldNotBeExecutedWhenExceptionOccured()
        {
            var firstPeriod = START_DATETIME.NewPeriod().Active();
            var secondPeriod = firstPeriod.NewPeriodAfter().Active();

            using (TestablePeriodStorage periodStorageSUT = GetStorage())
            {
                periodStorageSUT.Add(firstPeriod);
                try
                {
                    periodStorageSUT.ExecuteInTransaction(() =>
                    {
                        periodStorageSUT.Remove(firstPeriod);
                        periodStorageSUT.Add(secondPeriod);
                        throw new ArgumentException();
                    });
                    Assert.Fail("Exception in action should be propagated");
                }
                catch (Exception e) {
                }

                var found = periodStorageSUT.GetAll();
                CollectionAssert.AreEqual(new ActivityPeriod[] { firstPeriod }, found);
            }
        }

        protected abstract TestablePeriodStorage GetStorage();

        protected abstract bool IsStoragePersisent();

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

        protected override TestablePeriodStorage GetStorage()
        {
            return new SqlitePeriodStorage(DB_FILE);
        }

        protected override bool IsStoragePersisent()
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

        protected override TestablePeriodStorage GetStorage()
        {
            return new MemoryPeriodStorage();
        }

        protected override bool IsStoragePersisent()
        {
            return false;
        }
    }
}
