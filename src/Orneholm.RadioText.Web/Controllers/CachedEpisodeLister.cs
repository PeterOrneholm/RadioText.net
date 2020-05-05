using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Web.Controllers
{
    public class CachedEpisodeLister : IEpisodeLister
    {
        private readonly IEpisodeLister _episodeLister;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(30);

        public CachedEpisodeLister(IEpisodeLister episodeLister, IMemoryCache memoryCache)
        {
            _episodeLister = episodeLister;
            _memoryCache = memoryCache;
        }

        public async Task<List<SrStoredMiniSummarizedEpisode>> List(int count, string entityName = null, string entityType = null, string keyphrase = null, string query = null,  int? programId = null)
        {
            var key = (count, entityName, entityType, keyphrase, query, programId);
            if (_memoryCache.TryGetValue(key, out var value) && value is List<SrStoredMiniSummarizedEpisode> valueList)
            {
                return valueList;
            }

            var episodes = await _episodeLister.List(count, entityName, entityType, keyphrase, query, programId);
            _memoryCache.Set(key, episodes, _cacheTimeout);

            return episodes;
        }
    }
}
