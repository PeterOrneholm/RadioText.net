using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public interface IStorage
    {
        Task<SrStoredEpisodeStatus> GetEpisodeStatus(int episodeId);
        Task StoreEpisodeStatus(int episodeId, SrStoredEpisodeStatus episode);

        Task<SrStoredEpisode?> GetEpisode(int episodeId);
        Task StoreEpisode(int episodeId, SrStoredEpisode episode);

        Task<SrStoredEpisodeTranscription?> GetEpisodeTranscription(int episodeId);
        Task StoreTranscription(int episodeId, SrStoredEpisodeTranscription episode);

        Task<SrStoredEnrichedEpisode?> GetEnrichedEpisode(int episodeId);
        Task StoreEnrichedEpisode(int episodeId, SrStoredEnrichedEpisode episode);

        Task<SrStoredEpisodeSpeech?> GetEpisodeSpeech(int episodeId);
        Task StoreEpisodeSpeech(int episodeId, SrStoredEpisodeSpeech episode);
        Task<List<SrStoredEpisodeStatus>> GetEpisodesWithStatus(string? phase = null, string? state = null);
        Task DeleteEpisodeStatus(int episodeId);
        Task<List<SrStoredEpisode>> GetStoredEpisodes();
        Task DeleteStoredEpisode(int episodeId);
    }
}
