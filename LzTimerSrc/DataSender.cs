using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using log4net.Config;
using Newtonsoft.Json;

namespace kkot.LzTimer
{
    class DataSender : TimeTableUpdateListener
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly HttpClient client = new HttpClient();

        private static readonly string url = "http://localhost:8080/api/periods";

        private readonly TokenReceiver tokenReceiver;

        public DataSender(TokenReceiver tokenReceiver)
        {
            this.tokenReceiver = tokenReceiver;
        }

        public Dictionary<string, string> ConvertPeriod(ActivityPeriod period)
        {
            return new Dictionary<string, string>
            {
                { "beginTime" , period.Start.ToUniversalTime().ToString("o")},
                { "endTime" , period.Start.ToUniversalTime().ToString("o")},
                { "active", period.IsActive().ToString() }
            };
        }

        public async void Updated()
        {
            if (tokenReceiver.Token == null)
            {
                return;
            }
            var obj = JsonConvert.SerializeObject(
                    ConvertPeriod(new ActivePeriod(DateTime.Now, DateTime.Now.AddSeconds(10))));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenReceiver.Token);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var content = new StringContent(obj, Encoding.UTF8, "application/json");
            var resonse = await client.PostAsync(url, content);
            Log.Info(resonse);
        }
    }
}
