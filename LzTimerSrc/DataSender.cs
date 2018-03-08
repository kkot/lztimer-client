using System;
using System.Collections.Generic;
using System.Net.Http;

namespace kkot.LzTimer
{
    class DataSender : TimeTableUpdateListener
    {
        private static readonly HttpClient client = new HttpClient();

        private static readonly string url = "localhost:8080/api/periods";

        private readonly TokenReceiver tokenReceiver;

        public DataSender(TokenReceiver tokenReceiver)
        {
            this.tokenReceiver = tokenReceiver;
        }

        public Dictionary<String, String> ConvertPeriod(ActivityPeriod period)
        {
            return new Dictionary<string, string>
            {
                { "beginTime" , period.Start.ToString("o")},
                { "endTime" , period.Start.ToString("o")},
                { "active", period.IsActive().ToString() }
            };
        }

        public void Updated()
        {
            if (tokenReceiver.Token == null)
            {
                return;
            }



        }
    }
}
