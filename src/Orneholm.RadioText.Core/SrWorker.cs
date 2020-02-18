using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core
{
    public class SrWorker
    {
        private readonly ILogger<SrWorker> _logger;
        private readonly IStorage _storage;

        private readonly SrEpisodesLister _srEpisodesLister;
        private readonly SrEpisodeCollector _srEpisodeCollector;
        private readonly SrEpisodeTranscriber _srEpisodeTranscriber;
        private readonly SrEpisodeTextEnricher _srEpisodeTextEnricher;
        private readonly SrEpisodeSummarizer _srEpisodeSummarizer;
        private readonly SrEpisodeSpeaker _srEpisodeSpeaker;

        public SrWorker(ILogger<SrWorker> logger, IStorage storage, SrEpisodesLister srEpisodesLister, SrEpisodeCollector srEpisodeCollector, SrEpisodeTranscriber srEpisodeTranscriber, SrEpisodeTextEnricher srEpisodeTextEnricher, SrEpisodeSummarizer srEpisodeSummarizer, SrEpisodeSpeaker srEpisodeSpeaker)
        {
            _logger = logger;
            _storage = storage;

            _srEpisodesLister = srEpisodesLister;
            _srEpisodeCollector = srEpisodeCollector;
            _srEpisodeTranscriber = srEpisodeTranscriber;
            _srEpisodeTextEnricher = srEpisodeTextEnricher;
            _srEpisodeSummarizer = srEpisodeSummarizer;
            _srEpisodeSpeaker = srEpisodeSpeaker;
        }

        public async Task Work(Dictionary<int, int> srPrograms, bool cleanTranscriptions, CancellationToken stoppingToken)
        {
            if (cleanTranscriptions)
            {
                await _srEpisodeTranscriber.CleanExistingTranscriptions();
            }

            var listEpisode = await ListEpisodes(srPrograms);
            var listEpisodeIds = listEpisode.Select(x => x.Id).ToList();
            var tasks = new List<Task>();

            foreach (var episodeId in listEpisodeIds)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation($"Working on episode {episodeId}");

                        await CollectEpisode(episodeId);

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
        }

        private async Task<List<Episode>> ListEpisodes(Dictionary<int, int> srPrograms)
        {
            return await _srEpisodesLister.List(srPrograms);
        }

        private async Task CollectEpisode(int episodeId)
        {
            await RunPhase(episodeId, SrStoredEpisodePhases.Collect, async () =>
            {
                await _srEpisodeCollector.Collect(episodeId);
            }, async () =>
            {
                await TranscribeEpisode(episodeId);
            });
        }

        private async Task TranscribeEpisode(int episodeId)
        {
            await RunPhase(episodeId, SrStoredEpisodePhases.Transcribe, async () =>
            {
                await _srEpisodeTranscriber.TranscribeAndPersist(episodeId);
            }, async () =>
            {
                await EnrichEpisode(episodeId);
            });
        }

        private async Task EnrichEpisode(int episodeId)
        {
            await RunPhase(episodeId, SrStoredEpisodePhases.Enrich, async () =>
            {
                await _srEpisodeTextEnricher.Enrich(episodeId);
            }, async () =>
            {
                await SpeakEpisode(episodeId);
            });
        }

        private async Task SpeakEpisode(int episodeId)
        {
            await RunPhase(episodeId, SrStoredEpisodePhases.GenerateSpeech, async () =>
            {
                await _srEpisodeSpeaker.GenerateSpeak(episodeId);
            }, async () =>
            {
                await SummarizeEpisode(episodeId);
            });
        }

        private async Task SummarizeEpisode(int episodeId)
        {
            await RunPhase(episodeId, SrStoredEpisodePhases.Summarize, async () =>
            {
                await _srEpisodeSummarizer.Summarize(episodeId);
            }, () => Task.CompletedTask);
        }

        private async Task RunPhase(int episodeId, SrStoredEpisodePhases phase, Func<Task> action, Func<Task> actionOnSuccess)
        {
            try
            {
                var status = await _storage.GetEpisodeStatus(episodeId);
                Enum.TryParse<SrStoredEpisodePhases>(status.Phase, out var currentPhase);

                if (currentPhase > phase ||
                    currentPhase == phase && status.State != SrStoredEpisodeStates.Unknown)
                {
                    if (currentPhase == phase && status.State != SrStoredEpisodeStates.Error)
                    {
                        await actionOnSuccess();
                    }

                    return;
                }

                await _storage.StoreEpisodeStatus(episodeId, SrStoredEpisodeStatus.Started(episodeId, phase));
                await action();
                await _storage.StoreEpisodeStatus(episodeId, SrStoredEpisodeStatus.Done(episodeId, phase));

                await actionOnSuccess();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error on phase {phase} for episode {episodeId}", e);
                await _storage.StoreEpisodeStatus(episodeId, SrStoredEpisodeStatus.Error(episodeId, phase, e.Message));
            }
        }
    }
}
