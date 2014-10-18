using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace kkot.LzTimer
{
    public class SqlitePeriodStorage : PeriodStorage
    {
        private readonly SQLiteConnection conn;

        public SqlitePeriodStorage(String name)
        {
            if (!File.Exists(name))
            {
                SQLiteConnection.CreateFile(name);
            }

            //conn = new SQLiteConnection("Data Source=" + name + ";Synchronous=Full");
            conn = new SQLiteConnection(String.Format("Data Source={0}",name));
            conn.Open();
            CreateTable();
            PragmaExlusiveAccess();
        }

        public void Add(Period period)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "INSERT INTO Periods (start, end, type) VALUES (:start, :end, :type)";
            command.Parameters.AddWithValue("start", period.Start);
            command.Parameters.AddWithValue("end", period.End);
            command.Parameters.AddWithValue("type", period is ActivePeriod ? "A" : "I");
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

        private void ExecuteNonQuery(String sql)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public List<Period> GetSinceFirstActivePeriodBefore(DateTime dateTime)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            conn.Close();
        }

        public void Remove(Period period)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "DELETE FROM Periods WHERE start = :start AND end = :end AND type = :type";
            command.Parameters.AddWithValue("start", period.Start);
            command.Parameters.AddWithValue("end", period.End);
            command.Parameters.AddWithValue("type", period is ActivePeriod ? "A" : "I");
            command.ExecuteNonQuery();
        }

        public SortedSet<Period> GetAll()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT start, end, type FROM Periods", conn);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        public SortedSet<Period> GetPeriodsFromTimePeriod(TimePeriod period)
        {
            var sql = "SELECT start, end, type " +
                      "FROM Periods " +
                      "WHERE start >= :start AND end <= :end ";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.Parameters.AddWithValue("start", period.Start);
            command.Parameters.AddWithValue("end", period.End);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            var sql = "SELECT start, end, type " +
                "FROM Periods " +
                "WHERE start >= :start ";
            SQLiteCommand command = new SQLiteCommand(sql, conn);
            command.Parameters.AddWithValue("start", dateTime);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            while (reader.Read())
            {
                result.Add(CreatePeriodFromReader(reader));
            }
            return result;
        }

        private static Period CreatePeriodFromReader(SQLiteDataReader reader)
        {
            Period period;
            var start = reader["start"].ToString();
            var end = reader["end"].ToString();
            var type = reader["type"].ToString();
            if (type == "A")
            {
                period = new ActivePeriod(DateTime.Parse(start), DateTime.Parse(end));
            }
            else
            {
                period = new IdlePeriod(DateTime.Parse(start), DateTime.Parse(end));
            }
            return period;
        }
    }
}
