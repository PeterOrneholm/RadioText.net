using System;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public interface IStorage
    {
        Task<SrStoredEpisode?> GetEpisode(int episodeId);
        Task StoreEpisode(int episodeId, SrStoredEpisode episode);

        Task<SrStoredEpisodeTranscription?> GetEpisodeTranscription(int episodeId);
        Task StoreTranscription(int episodeId, SrStoredEpisodeTranscription episode);
    }
}
