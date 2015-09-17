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
        void RemoveFromTimePeriod(Period period);
        SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod);
        SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime);
        void Reset();
        ActivityPeriod GetPeriodBefore(DateTime start);
    }

    public interface TestablePeriodStorage : PeriodStorage
    {
        SortedSet<ActivityPeriod> GetAll();
    }

    public abstract class AbstractPeriodStorage : PeriodStorage
    {
        public abstract void Add(ActivityPeriod activityPeriod);
        public abstract void Remove(ActivityPeriod activityPeriod);

        public void RemoveFromTimePeriod(Period periodToRemove)
        {
            foreach (var period in GetPeriodsFromTimePeriod(periodToRemove))
            {
                Remove(period);
            }
        }

        public abstract SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod);
        public abstract SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime);
        public abstract void Reset();
        public abstract ActivityPeriod GetPeriodBefore(DateTime start);
        public abstract void Dispose();
    }

    public class MemoryPeriodStorage : AbstractPeriodStorage, TestablePeriodStorage
    {
        private SortedSet<ActivityPeriod> periods = new SortedSet<ActivityPeriod>();

        public override void Remove(ActivityPeriod activityPeriod)
        {
            periods.Remove(activityPeriod);
        }

        public SortedSet<ActivityPeriod> GetAll()
        {
            return periods;
        }

        public override void Add(ActivityPeriod activityPeriod)
        {
            periods.Add(activityPeriod);
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

        public override void Dispose()
        {
            periods = null;
        }

        public override void Reset()
        {
            periods.Clear();
        }
    }

    public class SqlitePeriodStorage : AbstractPeriodStorage, TestablePeriodStorage
    {
        private readonly SQLiteConnection conn;

        public SqlitePeriodStorage(String name)
        {
            if (!File.Exists(name))
            {
                SQLiteConnection.CreateFile(name);
            }

            conn = new SQLiteConnection(String.Format("Data Source={0};Synchronous=Full;locking_mode=NORMAL", name));
            conn.Open();
            CreateTable();
            CreateIndex();
            //PragmaExlusiveAccess();
        }

        public override void Add(ActivityPeriod activityPeriod)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "INSERT INTO Periods (start, end, type) VALUES (:start, :end, :type)";
            command.Parameters.AddWithValue("start", activityPeriod.Start);
            command.Parameters.AddWithValue("end", activityPeriod.End);
            command.Parameters.AddWithValue("type", activityPeriod is ActivePeriod ? "A" : "I");
            command.ExecuteNonQuery();
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

        private void ExecuteNonQuery(String sql)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public override void Remove(ActivityPeriod activityPeriod)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "DELETE FROM Periods WHERE start = :start AND end = :end AND type = :type";
            command.Parameters.AddWithValue("start", activityPeriod.Start);
            command.Parameters.AddWithValue("end", activityPeriod.End);
            command.Parameters.AddWithValue("type", activityPeriod is ActivePeriod ? "A" : "I");
            command.ExecuteNonQuery();
        }

        public SortedSet<ActivityPeriod> GetAll()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT start, end, type FROM Periods", conn);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<ActivityPeriod> result = new SortedSet<ActivityPeriod>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        public override SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod)
        {
            var sql = "SELECT start, end, type " +
                      "FROM Periods " +
                      "WHERE end > :start AND start < :end ";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.Parameters.AddWithValue("start", searchedPeriod.Start);
            command.Parameters.AddWithValue("end", searchedPeriod.End);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<ActivityPeriod> result = new SortedSet<ActivityPeriod>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        public override SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime)
        {
            var sql = "SELECT start, end, type " +
                "FROM Periods " +
                "WHERE end > :start ";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.Parameters.AddWithValue("start", dateTime);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<ActivityPeriod> result = new SortedSet<ActivityPeriod>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
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

        public override ActivityPeriod GetPeriodBefore(DateTime start)
        {
            var sql = " SELECT start, end, type" +
                      " FROM Periods" +
                      " WHERE end <= :start" +
                      " ORDER BY end DESC" +
                      " LIMIT 1";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.Parameters.AddWithValue("start", start);
            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return CreatePeriodFromReader(reader);
            }
            return null;
        }

        public override void Dispose()
        {
            conn.Close();
        }

        public override void Reset()
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "DELETE FROM Periods";
            command.ExecuteNonQuery();
        }
    }
}
