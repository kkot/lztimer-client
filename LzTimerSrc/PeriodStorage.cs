using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.IO;

namespace kkot.LzTimer
{
    public interface PeriodStorage : IDisposable
    {
        void Add(ActivityPeriod activityPeriod);
        void Remove(ActivityPeriod activityPeriod);


        /// <summary>
        /// Removes periods fully inside <paramref name="period"/>.
        /// </summary>
        /// <param name="period"></param>
        void RemoveFromTimePeriod(Period period);

        SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod);
        SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime);
        ActivityPeriod GetPeriodBefore(DateTime start);
        void Reset();
        void ExecuteInTransaction(Action executeInTransaction);
    }

    public interface TestablePeriodStorage : PeriodStorage
    {
        SortedSet<ActivityPeriod> GetAll();
    }

    public abstract class AbstractPeriodStorage : PeriodStorage
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public abstract void Add(ActivityPeriod activityPeriod);
        public abstract void Remove(ActivityPeriod activityPeriod);

        public void RemoveFromTimePeriod(Period periodToRemove)
        {
            log.Debug("remove from time period " + periodToRemove);
            foreach (var period in GetPeriodsFromTimePeriod(periodToRemove))
            {
                if (period.Start < periodToRemove.Start 
                    || period.End > periodToRemove.End)
                    continue;

                Remove(period);
            }
        }

        public abstract SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod);
        public abstract SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime);
        public abstract ActivityPeriod GetPeriodBefore(DateTime start);
        public abstract void Reset();
        public abstract void Dispose();
        public abstract void ExecuteInTransaction(Action executeInTransaction);
    }

    public class MemoryPeriodStorage : AbstractPeriodStorage, TestablePeriodStorage
    {
        private SortedSet<ActivityPeriod> periods = new SortedSet<ActivityPeriod>();

        public override void Add(ActivityPeriod activityPeriod)
        {
            periods.Add(activityPeriod);
        }

        public override void Remove(ActivityPeriod activityPeriod)
        {
            periods.Remove(activityPeriod);
        }

        public SortedSet<ActivityPeriod> GetAll()
        {
            return periods;
        }

        public override SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod)
        {
            return new SortedSet<ActivityPeriod>(periods.Where(p =>
                p.End > searchedPeriod.Start &&
                p.Start < searchedPeriod.End));
        }

        public override SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime)
        {
            return new SortedSet<ActivityPeriod>(periods.Where(p =>
                p.End > dateTime));
        }

        public override ActivityPeriod GetPeriodBefore(DateTime start)
        {
            return periods.Where(p => p.End <= start).OrderBy(period => period.Start).LastOrDefault();
        }

        public override void Reset()
        {
            periods.Clear();
        }

        public override void Dispose()
        {
            periods = null;
        }

        public override void ExecuteInTransaction(Action executeInTransaction)
        {
            var oldPeriods = new SortedSet<ActivityPeriod>(periods);
            try
            {
                executeInTransaction();
            }
            catch (Exception e)
            {
                periods = oldPeriods;
            }
        }
    }

    public class SqlitePeriodStorage : AbstractPeriodStorage, TestablePeriodStorage
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SQLiteConnection conn;

        public SqlitePeriodStorage(string name)
        {
            if (!File.Exists(name))
            {
                SQLiteConnection.CreateFile(name);
            }

            conn = new SQLiteConnection(string.Format("Data Source={0};Synchronous=Full", name));
            conn.Open();
            CreateTable();
            CreateIndex();
            PragmaExlusiveAccess();
        }

        private void PragmaExlusiveAccess()
        {
            ExecuteNonQuery("PRAGMA locking_mode=EXCLUSIVE");
        }

        private void CreateTable()
        {
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Periods (start, end, type)");
        }

        private void CreateIndex()
        {
            ExecuteNonQuery("CREATE INDEX IF NOT EXISTS start1 on Periods (start)");
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var command = conn.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        private SortedSet<ActivityPeriod> SelectActivityPeriods(string sql, Dictionary<string, object> parameters = null)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();

            using (var command = new SQLiteCommand(sql, conn))
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
                using (var reader = command.ExecuteReader())
                {
                    var result = new SortedSet<ActivityPeriod>();
                    while (reader.Read())
                    {
                        result.Add(CreatePeriodFromReader(reader));
                    }
                    return result;
                }
            }
        }

        private static ActivityPeriod CreatePeriodFromReader(SQLiteDataReader reader)
        {
            ActivityPeriod activityPeriod;
            var start = reader["start"].ToString();
            var end = reader["end"].ToString();
            var type = reader["type"].ToString();
            if (type == "A")
            {
                activityPeriod = new ActivePeriod(DateTime.Parse(start), DateTime.Parse(end));
            }
            else
            {
                activityPeriod = new IdlePeriod(DateTime.Parse(start), DateTime.Parse(end));
            }
            return activityPeriod;
        }

        public override void Add(ActivityPeriod activityPeriod)
        {
            log.Debug("add period " + activityPeriod);
            using (var command = conn.CreateCommand())
            {
                command.CommandText = "INSERT INTO Periods (start, end, type) VALUES (:start, :end, :type)";
                command.Parameters.AddWithValue("start", activityPeriod.Start);
                command.Parameters.AddWithValue("end", activityPeriod.End);
                command.Parameters.AddWithValue("type", activityPeriod is ActivePeriod ? "A" : "I");
                var rowsAffected = command.ExecuteNonQuery();
                log.Debug("rows affected " + rowsAffected);
            }
        }

        public override void Remove(ActivityPeriod activityPeriod)
        {
            log.Debug("remove period " + activityPeriod);
            using (var command = conn.CreateCommand())
            {
                command.CommandText = "DELETE FROM Periods WHERE start = :start AND end = :end AND type = :type";
                command.Parameters.AddWithValue("start", activityPeriod.Start);
                command.Parameters.AddWithValue("end", activityPeriod.End);
                command.Parameters.AddWithValue("type", activityPeriod is ActivePeriod ? "A" : "I");
                var rowsAffected = command.ExecuteNonQuery();
                log.Debug("rows affected " + rowsAffected);
            }
        }

        public SortedSet<ActivityPeriod> GetAll()
        {
            return SelectActivityPeriods("SELECT start, end, type FROM Periods");
        }

        public override SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod)
        {
            var sql = " SELECT start, end, type " +
                      " FROM Periods " +
                      " WHERE end > :start AND start < :end ";
            var parameters = new Dictionary<string, object>
            {
                {"start", searchedPeriod.Start },
                {"end", searchedPeriod.End }
            };
            return SelectActivityPeriods(sql, parameters);
        }

        public override SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime)
        {
            var sql = "SELECT start, end, type " +
                "FROM Periods " +
                "WHERE end > :start ";
            var parameters = new Dictionary<string, object>
            {
                {"start", dateTime}
            };
            return SelectActivityPeriods(sql, parameters);
        }

        public override ActivityPeriod GetPeriodBefore(DateTime start)
        {
            var sql = " SELECT start, end, type" +
                      " FROM Periods" +
                      " WHERE end <= :start" +
                      " ORDER BY end DESC" +
                      " LIMIT 1";
            var parameters = new Dictionary<string, object>
            {
                {"start", start }
            };
            var result = SelectActivityPeriods(sql, parameters);
            return result.FirstOrDefault();
        }

        public override void Dispose()
        {
            conn.Close();
        }

        public override void Reset()
        {
            ExecuteNonQuery("DELETE FROM Periods");
        }

        public override void ExecuteInTransaction(Action executeInTransaction)
        {
            using (var transaction = conn.BeginTransaction())
            {
                executeInTransaction();
                transaction.Commit();
            }
        }
    }
}
