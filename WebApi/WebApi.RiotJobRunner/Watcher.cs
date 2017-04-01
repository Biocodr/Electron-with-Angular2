﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using WebApi.RiotApiClient;
using WebApi.RiotApiClient.Misc;
using WebApi.RiotApiClient.Services.Interfaces;
using WebApi.RiotJobRunner.Jobs;

namespace WebApi.RiotJobRunner
{
    // TODO remove code duplication

    internal class Watcher
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private const int MaxSummonersPerRegion = 1000; 
        private const int MaxMatchesPerRegion = 10000;      // #summoners * 10
        private const int MaxMatchupsPerRegion = 900000;    // #matches * 90

        private readonly IJobRunner _jobRunner;
        private readonly ILeagueService _leagueService;
        private readonly IMatchService _matchService;
        private readonly IWebApiService _webApiService;

        private readonly ConcurrentDictionary<Region, ConcurrentQueue<long>> _regionSummonerIds;
        private readonly ConcurrentDictionary<Region, ConcurrentQueue<long>> _regionMatchIds;
        //private readonly ConcurrentDictionary<Region, ConcurrentQueue<Matchup>> _regionMatchups;

        public Watcher(
            IJobRunner jobRunner,
            ILeagueService leagueService,
            IMatchService matchService,
            IWebApiService webApiService)
        {
            _jobRunner = jobRunner;
            _leagueService = leagueService;
            _matchService = matchService;
            _webApiService = webApiService;

            _regionSummonerIds = new ConcurrentDictionary<Region, ConcurrentQueue<long>>();
            _regionMatchIds = new ConcurrentDictionary<Region, ConcurrentQueue<long>>();
            //_regionMatchups = new ConcurrentDictionary<Region, ConcurrentQueue<Matchup>>();
        }

        public async void PollHighTierPlayersAsync(Region region, TierLeague tierLeague, TimeSpan interval)
        {
            var rankedQueues = GameConstants.GetRankedQueueTypes();
            var regionSummonerIds = GetRegionSummonerIds(region);

            var jobs = rankedQueues
                .Select(queue => new TierLeagueJob(region, tierLeague, queue, _leagueService, _webApiService))
                .ToArray();

            do
            {
                if (regionSummonerIds.Count < MaxSummonersPerRegion)
                {
                    _jobRunner.EnqueueJobs(jobs);
                }

                await Task.Delay(interval);

            } while (_jobRunner.IsRunning);
        }

        public async void WatchMatchlistsAsync(TimeSpan interval)
        {
            var rankedQueues = GameConstants.GetRankedQueueTypes();
            var seasons = GameConstants.GetCurrentSeasons();

            do
            {
                var leagues = await _webApiService.GetLeaguesAsync();

                foreach (var league in leagues)
                {
                    var region = (Region)Enum.Parse(typeof(Region), league.Region);
                    var summonerId = league.PlayerOrTeamId;

                    var job = new MatchListJob(region, summonerId, rankedQueues, seasons, _matchService, _webApiService);

                    // TODO enqueue
                }

                await Task.Delay(interval);

            } while (_jobRunner.IsRunning);
        }

        //public async void WatchMatchupsAsync(Region region, TimeSpan interval)
        //{
        //    var regionMatchIds = GetRegionMatchIds(region);
        //    var regionMatchups = GetRegionMatchups(region);

        //    do
        //    {
        //        if (!regionMatchIds.IsEmpty
        //            && regionMatchIds.TryDequeue(out long matchId)
        //            && regionMatchups.Count < MaxMatchupsPerRegion)
        //        {
        //            var job = new MatchJob(region, matchId, _matchService, matchups => EnqueueMatchups(region, matchups));
        //            _jobRunner.EnqueueJob(job);
        //        }

        //        await Task.Delay(interval);

        //    } while (_jobRunner.IsRunning);
        //}

        //public async void WriteMatchupsToDatabase(Region region, TimeSpan interval)
        //{
        //    do
        //    {


        //        await Task.Delay(interval);

        //    } while (_databaseJobsRunner.IsRunning);
        //}

        private void EnqueueSummonerIds(Region region, IEnumerable<long> summonerIds)
        {
            var regionSummonerIds = GetRegionSummonerIds(region);

            foreach (var summonerId in summonerIds)
            {
                // TODO don't enqueue the same IDs over and over again (or don't process them)
                regionSummonerIds.Enqueue(summonerId);
            }

            Logger.Info($"{region} Summoner ID Queue contains {regionSummonerIds.Count} items.");
        }

        //private void EnqueueMatchIds(Region region, IEnumerable<long> matchIds)
        //{
        //    var regionMatchIds = GetRegionMatchIds(region);

        //    foreach (var matchId in matchIds)
        //    {
        //        // TODO don't enqueue the same IDs over and over again (or don't process them)
        //        regionMatchIds.Enqueue(matchId);
        //    }

        //    Logger.Info($"{region} Match ID Queue contains {regionMatchIds.Count} items.");
        //}

        //private void EnqueueMatchups(Region region, IEnumerable<Matchup> matchups)
        //{
        //    var regionMatchups = GetRegionMatchups(region);

        //    foreach (var matchup in matchups)
        //    {
        //        // TODO don't enqueue the same IDs over and over again (or don't process them)
        //        regionMatchups.Enqueue(matchup);
        //    }

        //    Logger.Info($"{region} Matchup Queue contains {regionMatchups.Count} items.");
        //}

        private ConcurrentQueue<long> GetRegionSummonerIds(Region region)
        {
            return _regionSummonerIds.GetOrAdd(region, new ConcurrentQueue<long>());
        }

        //private ConcurrentQueue<long> GetRegionMatchIds(Region region)
        //{
        //    return _regionMatchIds.GetOrAdd(region, new ConcurrentQueue<long>());
        //}

        //private ConcurrentQueue<Matchup> GetRegionMatchups(Region region)
        //{
        //    return _regionMatchups.GetOrAdd(region, new ConcurrentQueue<Matchup>());
        //}
    }
}
