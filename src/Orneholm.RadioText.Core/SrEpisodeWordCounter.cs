using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core
{
    public class SrEpisodeWordCounter
    {
        private readonly IStorage _storage;
        private readonly ISummaryStorage _summaryStorage;
        private readonly IWordCountStorage _wordCountStorage;
        private readonly ILogger<SrEpisodeWordCounter> _logger;
        private readonly string[][] _words = {
            new [] { "corona", "korona", "co rona", "ko rona" },
            new [] { "covid", "kovid", "kolv vid", "ko vid" },
            new [] { "sars" },

            new [] { "pandemi" },
            new [] { "flockimmunitet" },
            new [] { "kris" },
            new [] { "beredskap" },
            new [] { "karantän" },

            new [] { "plana ut" },
            new [] { "peak" },

            new [] { "virus" },
            new [] { "symtom", "symptom" },

            new [] { "folkhälsomyndigheten" },
            new [] { "statsepidemiolog" },
            new [] { "tegnell" },

            new [] { "toalettpapper" },
            new [] { "handsprit" },
            new [] { "bunkra" },

            new [] { "konkurs" },
            new [] { "arbetslös" },
            new [] { "uppsägning" },
            new [] { "varsel", "varsla" },
            new [] { "permittering", "permittera" },
            new [] { "recession" },
            new [] { "depression" },
            new [] { "ras" },

            new [] { "usa" },
            new [] { "kina" },
            new [] { "italien" },
            new [] { "spanien" },
            new [] { "sverige" },
        };

        public SrEpisodeWordCounter(ISummaryStorage summaryStorage, IWordCountStorage wordCountStorage, ILogger<SrEpisodeWordCounter> logger, IStorage storage)
        {
            _summaryStorage = summaryStorage;
            _wordCountStorage = wordCountStorage;
            _logger = logger;
            _storage = storage;
        }


        public async Task CountWords(int episodeId)
        {
            var storedEpisode = await _storage.GetEpisode(episodeId);
            if (storedEpisode == null)
            {
                _logger.LogWarning($"Episode {episodeId} isn't available...");
                return;
            }

            var summarizedEpisode = await _summaryStorage.GetSummarizedEpisode(episodeId);
            if (summarizedEpisode == null)
            {
                _logger.LogWarning($"Episode {episodeId} isn't summarized...");
                return;
            }

            await CountWords(episodeId, storedEpisode, summarizedEpisode);
        }

        private async Task CountWords(int episodeId, SrStoredEpisode storedEpisode, SrStoredSummarizedEpisode summarizedEpisode)
        {
            _logger.LogInformation($"Counting words for episode {episodeId}...");

            var transcription = summarizedEpisode.Transcription ?? string.Empty;
            var wordCount = GetWordsCount(transcription, _words);

            var wordCountEpisode = new SrStoredWordCountEpisode
            {
                EpisodeId = episodeId,

                EpisodeAudioUrl = summarizedEpisode.OriginalAudioUrl,
                EpisodeAudioLocale = summarizedEpisode.AudioLocale,
                EpisodeAudioDurationInSeconds = GetEpisodeDuration(storedEpisode.Episode),

                EpisodeTitle = summarizedEpisode.Title,
                EpisodeUrl = summarizedEpisode.Url,
                EpisodePublishDateUtc = summarizedEpisode.PublishDateUtc,

                ProgramId = summarizedEpisode.ProgramId,
                ProgramName = summarizedEpisode.ProgramName,

                EpisodeAudioTranscription = GetMaxLengthForTableStorage(transcription),

                WordCount = wordCount
            };

            await _wordCountStorage.StoreWordCounterEpisode(episodeId, wordCountEpisode);

            _logger.LogInformation($"Counted words on episode {episodeId}...");
        }

        private Dictionary<string, int> GetWordsCount(string text, string[][] allWords)
        {
            var wordCounts = new Dictionary<string, int>();
            var normalizedText = text?.ToLower().Trim() ?? string.Empty;

            foreach (var words in allWords)
            {
                var count = 0;

                foreach (var word in words)
                {
                    count += WordCount(normalizedText, word);
                }

                wordCounts.Add(words.First(), count);
            }

            return wordCounts;
        }

        private int WordCount(string text, string word)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(word))
            {
                return 0;
            }

            int count = 0, n = 0;

            while ((n = text.IndexOf(word, n, StringComparison.InvariantCulture)) != -1)
            {
                n += word.Length;
                ++count;
            }

            return count;
        }

        private static string GetMaxLengthForTableStorage(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 10000)
            {
                return text;
            }

            return text.Substring(0, 10000 - 3) + "...";
        }

        private static int GetEpisodeDuration(Episode episode)
        {
            var duration = episode?.Broadcast?.BroadcastFiles.FirstOrDefault()?.Duration;
            duration ??= episode?.DownloadPodfile?.DurationInSeconds;

            return duration ?? 0;
        }
    }
}
