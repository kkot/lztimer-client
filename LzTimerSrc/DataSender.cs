using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace kkot.LzTimer
{
    class PeriodDTO
    {
        public string beginTime { get; }
        public string endTime { get; }
        public bool active { get; }

        public PeriodDTO(ActivityPeriod period)
        {
            beginTime = period.Start.ToUniversalTime().ToString("o");
            endTime = period.End.ToUniversalTime().ToString("o");
            active = period.IsActive();
        }
    }

    class PeriodListDto
    {
        public List<PeriodDTO> periods { get; } = new List<PeriodDTO>();
    }

    class DataSender : TimeTableUpdateListener
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TimeSpan sentInterval = TimeSpan.FromSeconds(10);

        private DateTime lastSentDateTime = DateTime.MinValue;

        private readonly HttpClient client;

        private const string Url = "http://localhost:8080/api/periods";

        private readonly TokenReceiver tokenReceiver;

        private readonly PeriodStorage periodStorage;

        public DataSender(TokenReceiver tokenReceiver, PeriodStorage periodStorage)
        {
            this.tokenReceiver = tokenReceiver;
            this.periodStorage = periodStorage;
            client = new HttpClient();
        }

        PeriodListDto ConvertToDto(IEnumerable<ActivityPeriod> periods)
        {
            var result = new PeriodListDto();
            foreach (var period in periods)
            {
                result.periods.Add(new PeriodDTO(period));
            }

            return result;
        }

        public async void Updated()
        {
            if (tokenReceiver.Token == null)
            {
                return;
            }

            var now = DateTime.Now;
            if ((now - lastSentDateTime) < sentInterval)
            {
                Log.Debug($"Not sending because it was sent recently, now {now} lastSent {lastSentDateTime}");
                return;
            }

            while (true)
            {
                var activityPeriods = periodStorage.GetNotSent(10);
                if (activityPeriods.Count == 0)
                    return;

                var periodListDto = ConvertToDto(activityPeriods);
                Log.Debug("dto to sent  " + periodListDto);
                var periodListJson = JsonConvert.SerializeObject(periodListDto);
                Log.Debug("json to sent " + periodListJson);

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenReceiver.Token);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                var content = new StringContent(periodListJson, Encoding.UTF8, "application/json");
                var resonse = await client.PostAsync(Url, content);

                Log.Debug(resonse);
                if (resonse.StatusCode == HttpStatusCode.Unauthorized
                    || resonse.StatusCode == HttpStatusCode.Forbidden)
                {
                    Log.Info("Unauthorized - removing token");
                    tokenReceiver.InvalidateToken();
                    return;
                }

                if (resonse.StatusCode == HttpStatusCode.Created)
                {
                    var periodSent = new Period(activityPeriods.First().Start, activityPeriods.Last().End);
                    var updated = periodStorage.UpdateAsSent(periodSent);
                    if (updated != activityPeriods.Count)
                    {
                        // todo: I must update inside transaction, otherwise update is not consistent with read
                        throw new Exception("Wrong number of updated periods");
                    }
                    lastSentDateTime = now;
                }
                else
                {
                    throw new Exception("Unknown status code " + resonse.StatusCode);
                }
                Thread.Sleep(100); // TODO: remove only for development
            }
        }

        private static List<ActivityPeriod> ActivityPeriods()
        {
            var activePeriod1 = new ActivePeriod(DateTime.Now, DateTime.Now.AddSeconds(10));
            var activePeriod2 = new ActivePeriod(DateTime.Now.AddSeconds(10), DateTime.Now.AddSeconds(5));
            var activityPeriods = new List<ActivityPeriod> {activePeriod1, activePeriod2};
            return activityPeriods;
        }
    }
}