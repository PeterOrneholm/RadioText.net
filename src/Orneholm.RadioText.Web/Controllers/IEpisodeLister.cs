using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Web.Controllers
{
    public interface IEpisodeLister
    {
        Task<List<SrStoredMiniSummarizedEpisode>> List(int count, string entityName = null, string entityType = null, string keyphrase = null, string query = null, int? programId = null);
    }
}
