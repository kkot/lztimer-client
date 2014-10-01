using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace kkot.LzTimer
{
    public class DbPeriod
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        
        public string Type { get; set; }

        public DbPeriod()
        {
            
        }

        public DbPeriod(Period period)
        {
            Start = period.Start;
            End = period.End;
            Type = period is ActivePeriod ? "A" : "I";
        }

        public Period ToPeriod()
        {
            if (Type == "A")
                return new ActivePeriod(this.Start, this.End);
            else
                return new IdlePeriod(this.Start, this.End);
        }
    }

    public class SqlitePeriodStorage : PeriodStorage
    {
        private SQLiteConnection conn;

        public SqlitePeriodStorage(String name)
        {
            conn = new SQLiteConnection("Data Source="+name);
            conn.Open();
        }

        public void Add(Period period)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "INSERT INTO Periods (start, end, type) VALUES (:start, :end, :type)";
            command.Parameters.AddWithValue("start", period.Start);
            command.Parameters.AddWithValue("end", period.End);
            command.Parameters.AddWithValue("type", "A");
            command.ExecuteNonQuery();
        }

        public void Remove(Period period)
        {
            throw new NotImplementedException();
        }

        public SortedSet<Period> getAll()
        {
            SQLiteCommand command = new SQLiteCommand("SELECT Start, End, Type FROM Periods", conn);
            SQLiteDataReader reader = command.ExecuteReader();

            SortedSet<Period> result = new SortedSet<Period>();
            while (reader.Read())
            {
                result.Add(new ActivePeriod(DateTime.Parse(reader["Start"].ToString()), DateTime.Parse(reader["End"].ToString())));
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
