using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace kkot.LzTimer
{
    interface IDataSenterSettingsProvider
    {
        string ServerAddress { get; }
        string TaskName { get; }
    }

    interface IDataSentStatusReceiver
    {
        void Report(String status);
    }

    class PeriodDTO
    {
        public string beginTime { get; }
        public string endTime { get; }
        public bool active { get; }
        public string task { get; }

        public PeriodDTO(ActivityPeriod period, string taskName)
        {
            beginTime = period.Start.ToUniversalTime().ToString("o");
            endTime = period.End.ToUniversalTime().ToString("o");
            active = period.IsActive();
            task = taskName;
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

        private readonly TokenReceiver tokenReceiver;
        private readonly PeriodStorage periodStorage;
        private readonly IDataSenterSettingsProvider settingsProvider;
        private readonly IDataSentStatusReceiver statusReceiver;

        public DataSender(TokenReceiver tokenReceiver, PeriodStorage periodStorage,
            IDataSenterSettingsProvider settingsProvider, IDataSentStatusReceiver statusReceiver)
        {
            this.tokenReceiver = tokenReceiver;
            this.periodStorage = periodStorage;
            this.settingsProvider = settingsProvider;
            this.statusReceiver = statusReceiver;
            client = new HttpClient();
        }

        PeriodListDto ConvertToDto(IEnumerable<ActivityPeriod> periods, string taskName)
        {
            var result = new PeriodListDto();
            foreach (var period in periods)
            {
                result.periods.Add(new PeriodDTO(period, taskName));
            }

            return result;
        }

        public async void Updated()
        {
            if (tokenReceiver.Token == null)
            {
                statusReceiver.Report("No valid token");
                return;
            }

            var server = settingsProvider.ServerAddress;
            if (server.Trim().Length == 0)
            {
                statusReceiver.Report("Address is empty");
                return;
            }

            var url = server + "/api/periods";

            var now = DateTime.Now;
            if (now - lastSentDateTime < sentInterval)
            {
                Log.Debug($"Not sending because it was sent recently, now {now} lastSent {lastSentDateTime}");
                return;
            }

            lastSentDateTime = now; // there might be exception but still I prefer to wait with retry

            try
            {
                while (true)
                {
                    var activityPeriods = periodStorage.GetNotSent(10);
                    if (activityPeriods.Count == 0)
                        return;

                    var task = settingsProvider.TaskName;
                    var periodListDto = ConvertToDto(activityPeriods, task);
                    Log.Debug("dto to sent  " + periodListDto);
                    var periodListJson = JsonConvert.SerializeObject(periodListDto);
                    Log.Debug("json to sent " + periodListJson);

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenReceiver.Token);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    var content = new StringContent(periodListJson, Encoding.UTF8, "application/json");

                    var resonse = await client.PostAsync(url, content);

                    Log.Debug(resonse);
                    if (resonse.StatusCode == HttpStatusCode.Unauthorized
                        || resonse.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Info("Unauthorized - removing token");
                        tokenReceiver.InvalidateToken();
                        ShowMessage();
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

                        statusReceiver.Report("Sent " + now);
                    }
                    else
                    {
                        throw new Exception("Unknown status code " + resonse.StatusCode);
                    }

                    Thread.Sleep(100); // TODO: remove only for development
                }
            }
            catch (Exception exception)
            {
                statusReceiver.Report("Error " + exception.Message);
                Log.Error("cannot sent data", exception);
            }
        }

        private static List<ActivityPeriod> ActivityPeriods()
        {
            var activePeriod1 = new ActivePeriod(DateTime.Now, DateTime.Now.AddSeconds(10));
            var activePeriod2 = new ActivePeriod(DateTime.Now.AddSeconds(10), DateTime.Now.AddSeconds(5));
            var activityPeriods = new List<ActivityPeriod> {activePeriod1, activePeriod2};
            return activityPeriods;
        }

        private void ShowMessage()
        {
            string message = "Token is invalid, log in again.";
            string caption = "Token invalid";
            var buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }
    }
}