using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace TravisBot
{
    public class CovidModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private static string _jsonUrl;
        private static CovidDeathStatCollection _covidCache;

        private static System.Timers.Timer _timer;
        private static ILogger _logger;

        public CovidModule(IConfiguration configuration, ILogger<CovidModule> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _jsonUrl = _configuration.GetValue<string>("covidUrl");

            CovidModule.UpdateCache(this, null);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += UpdateCache;
            _timer.Interval = TimeSpan.FromMinutes(30).TotalMilliseconds;
            _timer.Start();
        }

        private static void UpdateCache(object source, ElapsedEventArgs e)
        {
            _covidCache = new CovidDeathStatCollection().ReadFromHTTP(_jsonUrl);
            _logger.LogInformation($"Cache Updated at {DateTime.Now.ToLongTimeString()}");
        }

        [Command("covid")]
        public async Task CovidAsync()
        {
            await Context.Channel.SendMessageAsync(_covidCache.ToString());
        }
    }

    public class CovidDeathStatCollection
    {
        public List<CovidDeathCountryStat> CountryStats = new List<CovidDeathCountryStat>();
        public DateTime LastUpdatedAt;
        public CovidDeathStatCollection ReadFromHTTP(string url)
        {
            using HttpClient client = new HttpClient();
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            JObject o = JObject.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            JToken metadata = o["metadata"];
            LastUpdatedAt = DateTime.Parse(metadata["lastUpdatedAt"].ToString());
            JArray countries = JArray.FromObject(o["countries"]);
            foreach (var i in countries)
            {
                CountryStats.Add(new CovidDeathCountryStat
                {
                    AreaCode = i["areaCode"].ToString(),
                    AreaName = i["areaName"].ToString(),
                    ReportingDate = string.IsNullOrEmpty(i["reportingDate"].ToString()) ? new DateTime() : DateTime.Parse(i["reportingDate"].ToString()),
                    DailyChangeInDeaths = string.IsNullOrEmpty(i["dailyChangeInDeaths"].ToString()) ? 0 : (int)i["dailyChangeInDeaths"],
                    CumulativeDeaths = string.IsNullOrEmpty(i["cumulativeDeaths"].ToString()) ? 0 : (int)i["cumulativeDeaths"]
                });
            }
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Deaths on {LastUpdatedAt.ToShortDateString()}");
            foreach(var c in CountryStats.Where(x => x.ReportingDate.Date == LastUpdatedAt.Date).GroupBy(x => x.AreaName))
            {
                sb.Append($"\n{c.Key}: {c.Sum(x => x.DailyChangeInDeaths)}");
            }
            return sb.ToString();
        }
    }

    public class CovidDeathCountryStat
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public DateTime ReportingDate { get; set; }
        public int DailyChangeInDeaths { get; set; }
        public int CumulativeDeaths { get; set; }

        public override string ToString()
        {
            return $"Area Code: {AreaCode}\nArea Name: {AreaName}\nReporting Date: {ReportingDate}\n" +
                $"Daily Change in Deaths: {DailyChangeInDeaths}\nCumulative Deaths: {CumulativeDeaths}";
        }
    }
}
