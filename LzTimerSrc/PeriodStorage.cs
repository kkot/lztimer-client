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
        SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod);
        SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime);
        void Reset();
    }

    public interface TestablePeriodStorage : PeriodStorage
    {
        SortedSet<ActivityPeriod> GetAll();
    }

    public class MemoryPeriodStorage : TestablePeriodStorage
    {
        private SortedSet<ActivityPeriod> periods = new SortedSet<ActivityPeriod>();

        public void Remove(ActivityPeriod activityPeriod)
        {
            periods.Remove(activityPeriod);
        }

        public SortedSet<ActivityPeriod> GetAll()
        {
            return periods;
        }

        public void Add(ActivityPeriod activityPeriod)
        {
            periods.Add(activityPeriod);
        }

        public SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod)
        {
            return new SortedSet<ActivityPeriod>(periods.Where(p =>
                p.End > searchedPeriod.Start &&
                p.Start < searchedPeriod.End));
        }

        public SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime)
        {
            return new SortedSet<ActivityPeriod>(periods.Where(p =>
                p.End > dateTime));
        }

        public List<ActivityPeriod> GetSinceFirstActivePeriodBefore(DateTime dateTime)
        {
            DateTime fromDate = periods.Where(p => p.Start < dateTime).ToList().Last().Start;
            return periods.Where((p) => p.Start >= fromDate).ToList();
        }

        public void Dispose()
        {
            periods = null;
        }

        public void Reset()
        {
            periods.Clear();
        }
    }

    public class SqlitePeriodStorage : TestablePeriodStorage
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

        public void Add(ActivityPeriod activityPeriod)
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

        public void Remove(ActivityPeriod activityPeriod)
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

        public SortedSet<ActivityPeriod> GetPeriodsFromTimePeriod(Period searchedPeriod)
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

        public SortedSet<ActivityPeriod> GetPeriodsAfter(DateTime dateTime)
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

        public void Dispose()
        {
            conn.Close();
        }

        public void Reset()
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "DELETE FROM Periods";
            command.ExecuteNonQuery();
        }
    }
}
