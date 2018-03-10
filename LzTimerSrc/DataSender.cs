using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using log4net.Config;
using Newtonsoft.Json;

namespace kkot.LzTimer
{
    class DataSender : TimeTableUpdateListener
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private HttpClient client;

        private const string Url = "http://localhost:8080/api/periods";

        private readonly TokenReceiver tokenReceiver;
        private CookieContainer cookieContainer;

        public DataSender(TokenReceiver tokenReceiver)
        {
            this.tokenReceiver = tokenReceiver;
            client = new HttpClient();
        }

        class Period
        {
            public String beginTime;
            public String endTime;
            public bool active;

            public Period(ActivityPeriod period)
            {
                beginTime = period.Start.ToUniversalTime().ToString("o");
                endTime = period.End.ToUniversalTime().ToString("o");
                active = period.IsActive();
            }
        }

        class PeriodList
        {
            public List<Period> periods = new List<Period>();
        }

        PeriodList ConvertPeriods(List<ActivityPeriod> periods)
        {
            var result = new PeriodList();
            foreach (var perio in periods)
            {
                result.periods.Add(new Period(perio));
            }

            return result;
        }

        public async void Updated()
        {
            if (tokenReceiver.Token == null)
            {
                return;
            }

            client = new HttpClient();
            var activePeriod1 = new ActivePeriod(DateTime.Now, DateTime.Now.AddSeconds(10));
            var activePeriod2 = new ActivePeriod(DateTime.Now.AddSeconds(10), DateTime.Now.AddSeconds(5));

            var activityPeriods = new List<ActivityPeriod> {activePeriod1, activePeriod2};
            var periodList = JsonConvert.SerializeObject(ConvertPeriods(activityPeriods));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenReceiver.Token);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var content = new StringContent(periodList, Encoding.UTF8, "application/json");
            var resonse = await client.PostAsync(Url, content);

            Log.Info(resonse);
            if (resonse.StatusCode == HttpStatusCode.Unauthorized
                || resonse.StatusCode == HttpStatusCode.Forbidden)
            {
                Log.Info("Unauthorized - removing token");
                tokenReceiver.InvalidateToken();
            }
        }
    }
}