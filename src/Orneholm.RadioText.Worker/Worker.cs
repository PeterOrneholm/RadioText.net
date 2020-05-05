using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Request;

namespace Orneholm.RadioText.Worker
{
    public class Worker : BackgroundService
    {
        private static readonly Dictionary<int, int> SrPrograms = new Dictionary<int, int>
        {
            { SrProgramIds.Ekot, 1500 },

            //{ SrProgramIds.RadioSweden_English, 5 },

            //{ SrProgramIds.P4_Stockholm, 20 }
        };


        private static readonly Dictionary<int, DateRange> SrProgramWithDates = new Dictionary<int, DateRange>
        {
            { SrProgramIds.Ekot, new DateRange(DateTime.Now.Date.AddDays(-10)) }
        };

        private static readonly List<int> SrEpisodes = new List<int>
        {
            1467478
        };


        private readonly ILogger<Worker> _logger;
        private readonly SrWorker _srWorker;
        private readonly IStorage _storage;
        private readonly ISverigesRadioApiClient _sverigesRadioApiClient;

        public Worker(ILogger<Worker> logger, SrWorker srWorker, IStorage storage, ISverigesRadioApiClient sverigesRadioApiClient)
        {
            _logger = logger;
            _srWorker = srWorker;
            _storage = storage;
            _sverigesRadioApiClient = sverigesRadioApiClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var episodeId in SrEpisodes)
            {
                await _storage.DeleteEpisodeStatus(episodeId);
                await _storage.DeleteStoredEpisode(episodeId);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                //await _srWorker.Work(SrEpisodes, true, stoppingToken);

                //await _srWorker.ReRunPhase("Collect", "Error", true, stoppingToken);
                await ReRunWith(null, SrStoredEpisodeStates.Error, stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task ReRunWith(SrStoredEpisodePhases? phase, string? state, CancellationToken stoppingToken)
        {
            var allErrorEpisodes = await _storage.GetEpisodesWithStatus(phase?.ToString(), state);
            if (!allErrorEpisodes.Any())
            {
                return;
            }

            var episodeInfos = await _sverigesRadioApiClient.GetEpisodesAsync(allErrorEpisodes.Select(x => x.EpisodeId).ToList(), ListPagination.TakeFirst(allErrorEpisodes.Count));
            var ids = new StringBuilder();
            foreach (var episode in episodeInfos.Episodes.OrderBy(x => x.PublishDateUtc))
            {
                await _storage.DeleteEpisodeStatus(episode.Id);
                await _storage.DeleteStoredEpisode(episode.Id);

                _logger.LogInformation($"Deleting {episode.Id} - {episode.PublishDateUtc} - {episode.Title}...");
            }

            await _srWorker.Work(allErrorEpisodes.Select(x => x.EpisodeId).ToList(), true, stoppingToken);
        }

        private async Task WriteEpisodeInfos(SrStoredEpisodePhases? phase, string? state)
        {
            var allErrorEpisodes = await _storage.GetEpisodesWithStatus(phase?.ToString(), state);
            if (!allErrorEpisodes.Any())
            {
                return;
            }

            var episodeInfos = await _sverigesRadioApiClient.GetEpisodesAsync(allErrorEpisodes.Select(x => x.EpisodeId).ToList(), ListPagination.TakeFirst(allErrorEpisodes.Count));
            var ids = new StringBuilder();
            foreach (var episode in episodeInfos.Episodes.OrderBy(x => x.PublishDateUtc))
            {
                ids.AppendLine($"{episode.Id} - {episode.PublishDateUtc} - {episode.Title}...");
            }

            var idsString = ids.ToString();
            Console.Write(idsString);

            Console.ReadLine();
        }
    }
}
