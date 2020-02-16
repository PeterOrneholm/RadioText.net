using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Azure.TranslatorClient;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Core
{
    public class SrEpisodeTextEnricher
    {
        private readonly TextAnalyticsClient _textAnalyticsClient;
        private readonly TranslatorClient _translatorClient;
        private readonly IStorage _storage;
        private readonly ILogger<SrEpisodeTextEnricher> _logger;

        public SrEpisodeTextEnricher(TextAnalyticsClient textAnalyticsClient, TranslatorClient translatorClient, IStorage storage, ILogger<SrEpisodeTextEnricher> logger)
        {
            _textAnalyticsClient = textAnalyticsClient;
            _translatorClient = translatorClient;
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

            var storedEnrichedEpisode = new SrStoredEnrichedEpisode
            {
                OriginalLocale = storedEpisode.AudioLocale
            };

            var title = storedEpisode.Episode.Title;
            var description = storedEpisode.Episode.Description;
            var transcription = storedEpisodeTranscription.CombinedDisplayResult;
            var locale = storedEpisode.AudioLocale;

            _logger.LogInformation($"Analyzing episode {episodeId} texts for sv-SE...");
            var swedishTexts = await AnalyzeTexts(episodeId, title, description, transcription, locale, "sv-SE");
            storedEnrichedEpisode.Title_SV = swedishTexts.Title;
            storedEnrichedEpisode.Description_SV = swedishTexts.Description;
            storedEnrichedEpisode.Transcription_SV = swedishTexts.Transcription;

            _logger.LogInformation($"Analyzing episode {episodeId} texts for en-US...");
            var englishTexts = await AnalyzeTexts(episodeId, title, description, transcription, locale, "en-US");
            storedEnrichedEpisode.Title_EN = englishTexts.Title;
            storedEnrichedEpisode.Description_EN = englishTexts.Description;
            storedEnrichedEpisode.Transcription_EN = englishTexts.Transcription;

            if (locale == "sv-SE")
            {
                storedEnrichedEpisode.Title_Original = storedEnrichedEpisode.Title_SV;
                storedEnrichedEpisode.Description_Original = storedEnrichedEpisode.Description_SV;
                storedEnrichedEpisode.Transcription_Original = storedEnrichedEpisode.Transcription_SV;
            }
            else if (locale == "en-US")
            {
                storedEnrichedEpisode.Title_Original = storedEnrichedEpisode.Title_EN;
                storedEnrichedEpisode.Description_Original = storedEnrichedEpisode.Description_EN;
                storedEnrichedEpisode.Transcription_Original = storedEnrichedEpisode.Transcription_EN;
            }
            else
            {
                var customTexts = await AnalyzeTexts(episodeId, title, description, transcription, locale, locale);
                storedEnrichedEpisode.Title_Original = customTexts.Title;
                storedEnrichedEpisode.Description_Original = customTexts.Description;
                storedEnrichedEpisode.Transcription_Original = customTexts.Transcription;
            }

            await _storage.StoreEnrichedEpisode(episodeId, storedEnrichedEpisode);

            _logger.LogInformation($"Enriched episode {episodeId}...");
        }

        private async Task<EpisodeTexts> AnalyzeTexts(int episodeId, string title, string description, string transcription, string textLocale, string targetLocale)
        {
            if (textLocale == targetLocale)
            {
                return new EpisodeTexts
                {
                    Title = await Analyze(title),
                    Description = await Analyze(description),
                    Transcription = await Analyze(transcription)
                };
            }

            _logger.LogInformation($"Translating episode {episodeId} from {textLocale} to {targetLocale}...");

            var translations = await _translatorClient.Translate(
                new List<string> {
                    GetMaxLengthTextForTranslation(title),
                    GetMaxLengthTextForTranslation(description),
                    GetMaxLengthTextForTranslation(transcription)
                },
                new List<string> { targetLocale },
                textLocale
            );

            return new EpisodeTexts
            {
                Title = await Analyze(GetTranslation(0, translations)),
                Description = await Analyze(GetTranslation(1, translations)),
                Transcription = await Analyze(GetTranslation(2, translations))
            };

            string GetTranslation(int index, List<TranslationResult> t)
            {
                return t[index].Translations?.FirstOrDefault()?.Text ?? string.Empty;
            }
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

        private static string GetMaxLengthTextForTranslation(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 4500)
            {
                return text;
            }

            return text.Substring(0, 4500);
        }

        private class EpisodeTexts
        {
            public EnrichedText? Title { get; set; }
            public EnrichedText? Description { get; set; }
            public EnrichedText? Transcription { get; set; }
        }
    }
}
