using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Core
{
    public class SrEpisodeTextEnricher
    {
        private readonly TextAnalyticsClient _textAnalyticsClient;
        private readonly IStorage _storage;
        private readonly ILogger<SrEpisodeTextEnricher> _logger;

        public SrEpisodeTextEnricher(TextAnalyticsClient textAnalyticsClient, IStorage storage, ILogger<SrEpisodeTextEnricher> logger)
        {
            _textAnalyticsClient = textAnalyticsClient;
            _storage = storage;
            _logger = logger;
        }


        public async Task Enrich(int episodeId)
        {
            var storedEpisode = await _storage.GetEpisode(episodeId);
            if (storedEpisode == null)
            {
                _logger.LogInformation($"Episode {episodeId} isn't available...");
                return;
            }

            var storedEpisodeTranscription = await _storage.GetEpisodeTranscription(episodeId);
            if (storedEpisodeTranscription == null || storedEpisodeTranscription.Status != "Transcribed")
            {
                _logger.LogInformation($"Episode {episodeId} isn't transcribed...");
                return;
            }

            var enrichedEpisode = await _storage.GetEnrichedEpisode(episodeId);
            if (enrichedEpisode != null)
            {
                _logger.LogInformation($"Episode {episodeId} already enriched...");
                return;
            }

            await Enrich(episodeId, storedEpisode, storedEpisodeTranscription);
        }

        private async Task Enrich(int episodeId, SrStoredEpisode storedEpisode, SrStoredEpisodeTranscription storedEpisodeTranscription)
        {
            _logger.LogInformation($"Enriching episode {episodeId}...");

            var titleAnalytics = await Analyze(storedEpisode.Episode.Title);
            var descriptionAnalytics = await Analyze(storedEpisode.Episode.Description);
            var transcriptionAnalytics = await Analyze(storedEpisodeTranscription.CombinedDisplayResult);

            var storedEnrichedEpisode = new SrStoredEnrichedEpisode
            {
                OriginalLocale = storedEpisode.AudioLocale
            };

            if (storedEpisode.AudioLocale == "sv-SE")
            {
                storedEnrichedEpisode.Title_SV = titleAnalytics;
                storedEnrichedEpisode.Description_SV = descriptionAnalytics;
                storedEnrichedEpisode.Transcription_SV = transcriptionAnalytics;
            }
            else if (storedEpisode.AudioLocale == "en-US")
            {
                storedEnrichedEpisode.Title_EN = titleAnalytics;
                storedEnrichedEpisode.Description_EN = descriptionAnalytics;
                storedEnrichedEpisode.Transcription_EN = transcriptionAnalytics;
            }

            await _storage.StoreEnrichedEpisode(episodeId, storedEnrichedEpisode);

            _logger.LogInformation($"Enriched episode {episodeId}...");
        }

        private async Task<EnrichedText> Analyze(string text)
        {
            var limitedText = GetMaxLengthTextForAnalytics(text);

            var keyPhrases = await _textAnalyticsClient.KeyPhrasesAsync(limitedText);
            var sentiment = await _textAnalyticsClient.SentimentAsync(limitedText);
            var entities = await _textAnalyticsClient.EntitiesAsync(limitedText);

            return new EnrichedText
            {
                Text = text,
                KeyPhrases = keyPhrases.KeyPhrases?.ToList() ?? new List<string>(),
                Entities = entities.Entities?.ToList() ?? new List<EntityRecord>(),
                Sentiment = sentiment.Score
            };
        }

        private static string GetMaxLengthTextForAnalytics(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 5120)
            {
                return text;
            }

            return text.Substring(0, 5120);
        }
    }
}
