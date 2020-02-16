using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Core
{
    public class SrEpisodeSummarizer
    {
        private readonly IStorage _storage;
        private readonly ILogger<SrEpisodeSummarizer> _logger;

        public SrEpisodeSummarizer(IStorage storage, ILogger<SrEpisodeSummarizer> logger)
        {
            _storage = storage;
            _logger = logger;
        }


        public async Task Summarize(int episodeId)
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
            if (enrichedEpisode == null)
            {
                _logger.LogInformation($"Episode {episodeId} isn't enriched...");
                return;
            }

            var speechEpisode = await _storage.GetEpisodeSpeech(episodeId);
            if (speechEpisode == null)
            {
                _logger.LogInformation($"Episode {episodeId} isn't speeched...");
                return;
            }

            var summarizedEpisode = await _storage.GetSummarizedEpisode(episodeId);
            if (summarizedEpisode != null)
            {
                _logger.LogInformation($"Episode {episodeId} already summarized...");
                return;
            }

            await Summarize(episodeId, storedEpisode, storedEpisodeTranscription, enrichedEpisode, speechEpisode);
        }

        private async Task Summarize(int episodeId, SrStoredEpisode storedEpisode, SrStoredEpisodeTranscription storedEpisodeTranscription, SrStoredEnrichedEpisode storedEnrichedEpisode, SrStoredEpisodeSpeech episodeSpeech)
        {
            _logger.LogInformation($"Summarizing episode {episodeId}...");

            var summarizedEpisode = new SrStoredSummarizedEpisode
            {
                EpisodeId = episodeId,

                OriginalAudioUrl = storedEpisode.OriginalAudioUrl,

                AudioUrl = storedEpisode.AudioUrl,
                AudioLocale = storedEpisode.AudioLocale,

                Title = storedEpisode.Episode.Title,
                Description = storedEpisode.Episode.Description,
                Url = storedEpisode.Episode.Url,
                PublishDateUtc = storedEpisode.Episode.PublishDateUtc,
                ImageUrl = storedEpisode.Episode.ImageUrl,
                ProgramId = storedEpisode.Episode.Program.Id,
                ProgramName = storedEpisode.Episode.Program.Name,

                Transcription = storedEpisodeTranscription.CombinedDisplayResult,

                Title_Original = storedEnrichedEpisode.Title_Original,
                Description_Original = storedEnrichedEpisode.Description_Original,
                Transcription_Original = storedEnrichedEpisode.Transcription_Original,

                Title_EN = storedEnrichedEpisode.Title_EN,
                Description_EN = storedEnrichedEpisode.Description_EN,
                Transcription_EN = storedEnrichedEpisode.Transcription_EN,
                SpeechUrl_EN = episodeSpeech.SpeechUrl_EN,

                Title_SV = storedEnrichedEpisode.Title_SV,
                Description_SV = storedEnrichedEpisode.Description_SV,
                Transcription_SV = storedEnrichedEpisode.Transcription_SV,
                SpeechUrl_SV = episodeSpeech.SpeechUrl_SV
            };

            await _storage.StoreSummarizedEpisode(episodeId, summarizedEpisode);

            _logger.LogInformation($"Summarized episode {episodeId}...");
        }
    }
}
