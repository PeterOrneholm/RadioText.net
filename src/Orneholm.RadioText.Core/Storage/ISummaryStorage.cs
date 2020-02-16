using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orneholm.RadioText.Core.Storage
{
    public interface ISummaryStorage
    {
        Task<List<SrStoredSummarizedEpisode>> ListSummarizedEpisode(int count = 100);
        Task<SrStoredSummarizedEpisode?> GetSummarizedEpisode(int episodeId);
        Task StoreSummarizedEpisode(int episodeId, SrStoredSummarizedEpisode episode);
    }
}
