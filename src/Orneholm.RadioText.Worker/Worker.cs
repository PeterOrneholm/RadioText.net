using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core;
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
        private readonly SrEpisodeTextEnricher _srEpisodeTextEnricher;
        private readonly SrEpisodeSummarizer _srEpisodeSummarizer;
        private readonly SrEpisodeSpeaker _srEpisodeSpeaker;

        public Worker(ILogger<Worker> logger, SrEpisodesLister srEpisodesLister, SrEpisodeCollector srEpisodeCollector, SrEpisodeTranscriber srEpisodeTranscriber, SrEpisodeTextEnricher srEpisodeTextEnricher, SrEpisodeSummarizer srEpisodeSummarizer, SrEpisodeSpeaker srEpisodeSpeaker)
        {
            _logger = logger;

            _srEpisodesLister = srEpisodesLister;
            _srEpisodeCollector = srEpisodeCollector;
            _srEpisodeTranscriber = srEpisodeTranscriber;
            _srEpisodeTextEnricher = srEpisodeTextEnricher;
            _srEpisodeSummarizer = srEpisodeSummarizer;
            _srEpisodeSpeaker = srEpisodeSpeaker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _srEpisodeTranscriber.CleanExistingTranscriptions();

                var listEpisode = await ListEpisodes();
                var listEpisodeIds = listEpisode.Select(x => x.Id).ToList();
                //listEpisodeIds = new List<int>()
                //{
                //    1445938,
                //    1442349
                //};
                var tasks = new List<Task>();

                foreach (var episodeId in listEpisodeIds)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation($"Working on episode {episodeId}");

                            await CollectEpisode(episodeId);
                            await TranscribeEpisode(episodeId);
                            await EnrichTextForEpisode(episodeId);
                            await SpeakEpisode(episodeId);
                            await SummarizeEpisode(episodeId);

                            _logger.LogInformation($"Worked on episode {episodeId}");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Failed working on episode {episodeId}");
                        }
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
            var srPrograms = new Dictionary<int, int>
            {
                { SverigesRadioApiIds.Programs.Ekot, 5 },
                { SverigesRadioApiIds.Programs.RadioSweden, 10 }, // Radio Sweden - English
                { 2494, 5 }, // Radio Sweden - Arabic
            };

            return await _srEpisodesLister.List(srPrograms);
        }

        private async Task CollectEpisode(int episodeId)
        {
            await _srEpisodeCollector.Collect(episodeId);
        }

        private async Task TranscribeEpisode(int episodeId)
        {
            await _srEpisodeTranscriber.TranscribeAndPersist(episodeId);
        }

        private async Task EnrichTextForEpisode(int episodeId)
        {
            await _srEpisodeTextEnricher.Enrich(episodeId);
        }

        private async Task SummarizeEpisode(int episodeId)
        {
            await _srEpisodeSummarizer.Summarize(episodeId);
        }

        private async Task SpeakEpisode(int episodeId)
        {
            await _srEpisodeSpeaker.GenerateSpeak(episodeId);
        }
    }
}
