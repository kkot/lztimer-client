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
            conn = new SQLiteConnection("Data Source=" + name);            
            conn.Open();
            CreateTable();
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

        private void CreateTable()
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "CREATE TABLE IF NOT EXISTS Periods (start, end, type)";
            command.ExecuteNonQuery();
        }

        public void Close()
        {
            conn.Close();
        }

        public void Remove(Period period)
        {
            throw new NotImplementedException();
        }

        public SortedSet<Period> GetAll()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT start, end, type FROM Periods", conn);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            if (!reader.HasRows)
            {
                return result;                
            }

            while (reader.Read())
            {
                var start = reader["start"].ToString();
                var end = reader["end"].ToString();
                var type = reader["type"].ToString();
                if (type == "A")
                {
                    result.Add(new ActivePeriod(DateTime.Parse(start), DateTime.Parse(end)));
                }
                else
                {
                    result.Add(new IdlePeriod(DateTime.Parse(start), DateTime.Parse(end)));
                }
            }
            return result;
        }

        public SortedSet<Period> GetPeriodsFromPeriod(TimePeriod period)
        {
            throw new NotImplementedException();
        }

        public SortedSet<Period> GetPeriodsAfter(DateTime dateTime)
        {
            throw new NotImplementedException();
        }
    }
}
