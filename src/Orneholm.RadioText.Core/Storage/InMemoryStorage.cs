using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public class InMemoryStorage : IStorage
    {
        private readonly ConcurrentDictionary<int, SrStoredEpisode> _episodes = new ConcurrentDictionary<int, SrStoredEpisode>();

        public Task<SrStoredEpisode?> GetEpisode(int episodeId)
        {
            if (!_episodes.ContainsKey(episodeId))
            {
                return Task.FromResult((SrStoredEpisode?)null);
            }

            return Task.FromResult((SrStoredEpisode?)_episodes[episodeId]);
        }

        public Task StoreEpisode(int episodeId, SrStoredEpisode episode)
        {
            _episodes[episodeId] = episode;
            return Task.CompletedTask;
        }
    }
}