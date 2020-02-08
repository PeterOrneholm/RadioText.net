using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.RadioText.Core.SverigesRadio;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly SrEpisodesLister _srEpisodesLister;
        private readonly SrEpisodeCollector _srEpisodeCollector;

        public Worker(ILogger<Worker> logger, SrEpisodesLister srEpisodesLister, SrEpisodeCollector srEpisodeCollector)
        {
            _logger = logger;

            _srEpisodesLister = srEpisodesLister;
            _srEpisodeCollector = srEpisodeCollector;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var listEpisodes = await ListEpisodes();
                var tasks = new List<Task>();

                foreach (var episode in listEpisodes)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var collectedEpisode = await CollectEpisode(episode.Id);
                        if (collectedEpisode != null)
                        {
                            _logger.LogInformation($"Collected: {collectedEpisode.Episode?.Title}");
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task<List<Episode>> ListEpisodes()
        {
            int[] srProgramIds =
            {
                SverigesRadioApiIds.Programs.Ekot,
                SverigesRadioApiIds.Programs.RadioSweden
            };

            var srProgramIdCount = 50;

            return await _srEpisodesLister.List(srProgramIds.ToList(), srProgramIdCount);
        }

        private async Task<SrStoredEpisode?> CollectEpisode(int episodeId)
        {
            return await _srEpisodeCollector.Collect(episodeId);
        }
    }
}
