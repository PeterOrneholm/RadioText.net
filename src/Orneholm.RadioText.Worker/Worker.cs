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
            //{ SrProgramIds.Ekot, new DateRange(new DateTime(2019, 12, 01), new DateTime(2020, 04, 25)) }
            { SrProgramIds.Ekot, new DateRange(DateTime.Now.Date.AddDays(-8)) }
        };

        private static  readonly  List<int> SrEpisodes = new List<int>
        {
            1407958
        };


        private readonly ILogger<Worker> _logger;
        private readonly SrWorker _srWorker;
        private readonly IStorage _storage;

        public Worker(ILogger<Worker> logger, SrWorker srWorker, IStorage storage)
        {
            _logger = logger;
            _srWorker = srWorker;
            _storage = storage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var allEpisodesWithStatus = await _storage.GetEpisodesWithStatus();
            var allEpisodesWithStatusIds = allEpisodesWithStatus.Select(x => x.EpisodeId);
            var allStoredEpisodes = await _storage.GetStoredEpisodes();
            var allStoredEpisodesIds = allStoredEpisodes.Select(x => x.Episode.Id);

            var storedEpisodesWithoutStatus = allStoredEpisodesIds.Except(allEpisodesWithStatusIds).ToList();
            foreach (var episodeId in storedEpisodesWithoutStatus)
            {
                Console.WriteLine($"Deleting episode {episodeId}");
                await _storage.DeleteStoredEpisode(episodeId);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await _srWorker.Work(SrProgramWithDates, true, stoppingToken);
                //await _srWorker.Work(SrEpisodes, true, stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}
