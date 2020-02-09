using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly SrEpisodesLister _srEpisodesLister;
        private readonly SrEpisodeCollector _srEpisodeCollector;
        private readonly SrEpisodeTranscriber _srEpisodeTranscriber;

        public Worker(ILogger<Worker> logger, SrEpisodesLister srEpisodesLister, SrEpisodeCollector srEpisodeCollector, SrEpisodeTranscriber srEpisodeTranscriber)
        {
            _logger = logger;

            _srEpisodesLister = srEpisodesLister;
            _srEpisodeCollector = srEpisodeCollector;
            _srEpisodeTranscriber = srEpisodeTranscriber;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _srEpisodeTranscriber.CleanExistingTranscriptions();

                var listEpisodes = await ListEpisodes();
                var tasks = new List<Task>();

                foreach (var episode in listEpisodes)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await CollectEpisode(episode.Id);
                        await TranscribeEpisode(episode.Id);
                    }, stoppingToken).ContinueWith(task =>
                    {
                        if (task.Exception != null)
                        {
                            throw task.Exception;
                        }
                    }, stoppingToken));
                }

                await Task.WhenAll(tasks);

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task<List<Episode>> ListEpisodes()
        {
            int[] srProgramIds =
            {
                //SverigesRadioApiIds.Programs.Ekot,
                SverigesRadioApiIds.Programs.RadioSweden
            };

            var srProgramIdCount = 1;

            return await _srEpisodesLister.List(srProgramIds.ToList(), srProgramIdCount);
        }

        private async Task<SrStoredEpisode?> CollectEpisode(int episodeId)
        {
            return await _srEpisodeCollector.Collect(episodeId);
        }

        private async Task TranscribeEpisode(int episodeId)
        {
            await _srEpisodeTranscriber.TranscribeAndPersist(episodeId);
        }
    }
}
