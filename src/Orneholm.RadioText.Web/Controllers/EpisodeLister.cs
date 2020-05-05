using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orneholm.RadioText.Core.Storage;

namespace Orneholm.RadioText.Web.Controllers
{
    public class EpisodeLister : IEpisodeLister
    {
        private readonly ISummaryStorage _summaryStorage;

        public EpisodeLister(ISummaryStorage summaryStorage)
        {
            _summaryStorage = summaryStorage;
        }

        public async Task<List<SrStoredMiniSummarizedEpisode>> List(int count, string entityName = null, string entityType = null, string keyphrase = null, string query = null, int? programId = null)
        {
            var episodes = await _summaryStorage.ListMiniSummarizedEpisode(count);

            var filteredEpisodes = FilterEpisodes(entityName, entityType, keyphrase, episodes, programId);
            var searchedEpisodes = SearchEpisodes(query, filteredEpisodes);
            var orderedEpisodes = OrderEpisodes(searchedEpisodes);

            return orderedEpisodes.ToList();
        }

        private static IOrderedEnumerable<SrStoredMiniSummarizedEpisode> OrderEpisodes(List<SrStoredMiniSummarizedEpisode> filteredEpisodes)
        {
            return filteredEpisodes.OrderByDescending(x => x.PublishDateUtc);
        }

        private static List<SrStoredMiniSummarizedEpisode> SearchEpisodes(string query, List<SrStoredMiniSummarizedEpisode> episodes)
        {
            var filteredEpisodes = episodes;

            if (!string.IsNullOrWhiteSpace(query))
            {
                filteredEpisodes = filteredEpisodes
                    .Where(x => x.Transcription_Original.Text.Contains(query) || x.Transcription_English.Text.Contains(query))
                    .ToList();
            }

            return filteredEpisodes;
        }

        private static List<SrStoredMiniSummarizedEpisode> FilterEpisodes(string entityName, string entityType, string keyphrase, List<SrStoredMiniSummarizedEpisode> episodes, int? programId)
        {
            var filteredEpisodes = episodes;

            if (programId.HasValue)
            {
                filteredEpisodes = filteredEpisodes
                    .Where(x => x.ProgramId == programId)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                filteredEpisodes = filteredEpisodes
                    .Where(x => x.Transcription_Original.Entities.Any(y => y.Name == entityName && (string.IsNullOrWhiteSpace(entityType) || y.Type == entityType))
                                || x.Transcription_English.Entities.Any(y => y.Name == entityName && (string.IsNullOrWhiteSpace(entityType) || y.Type == entityType)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(keyphrase))
            {
                filteredEpisodes = filteredEpisodes
                    .Where(x => x.Transcription_Original.KeyPhrases.Contains(keyphrase)
                                || x.Transcription_English.KeyPhrases.Contains(keyphrase))
                    .ToList();
            }

            return filteredEpisodes;
        }
    }
}
