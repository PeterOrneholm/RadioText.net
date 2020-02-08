using System.Collections.Concurrent;
using System.Threading.Tasks;
using Orneholm.SverigesRadio.Api.Models.Response.Episodes;

namespace Orneholm.RadioText.Core.Storage
{
    public interface IStorage
    {
        Task<bool> EpisodeExists(int programId, int episodeId);
        Task StoreEpisode(int programId, int episodeId, SrStoredEpisode episode);
    }

    public class SrStoredEpisode
    {
        public Episode? Episode { get; set; }
        public string OriginalAudioUrl { get; set; } = string.Empty;
        public string AudioBlobIdentifier { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public string AudioExtension { get; set; } = string.Empty;
        public string AudioLocale { get; set; } = string.Empty;
    }

    public class InMemoryStorage : IStorage
    {
        private readonly ConcurrentDictionary<(int, int), SrStoredEpisode> _episodes = new ConcurrentDictionary<(int, int), SrStoredEpisode>();

        public Task<bool> EpisodeExists(int programId, int episodeId)
        {
            return Task.FromResult(_episodes.ContainsKey((programId, episodeId)));
        }

        public Task StoreEpisode(int programId, int episodeId, SrStoredEpisode episode)
        {
            _episodes[(programId, episodeId)] = episode;
            return Task.CompletedTask;
        }
    }
}
