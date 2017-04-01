﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using WebApi.Model.Dtos.League;

namespace WebApi.RiotJobRunner
{
    internal class WebApiService : IWebApiService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private const string Domain = "http://localhost:5000";

        private readonly HttpClient _httpClient;

        public WebApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<IEnumerable<LeagueEntry>> GetLeaguesAsync()
        {
            var url = $"{Domain}/api/leagueentry";

            var response = await GetRequestAsync(url);

            return JsonConvert.DeserializeObject<LeagueEntry[]>(response);
        }

        public async Task SendLeagueAsync(League league)
        {
            var url = $"{Domain}/api/leagueentry";

            // TODO batch
            foreach (var leagueEntry in league.Entries)
            {
                var jsonObject = JsonConvert.SerializeObject(leagueEntry);

                await PostRequestAsync(url, jsonObject);
            }
        }

        private async Task PostRequestAsync(string url, string content)
        {
            using (var response = await _httpClient.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json")))
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private async Task<string> GetRequestAsync(string url)
        {
            string result;

            using (var response = await _httpClient.GetAsync(url))
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                using (var content = response.Content)
                {
                    result = await content.ReadAsStringAsync();
                }
            }

            return result;
        }
    }
}
